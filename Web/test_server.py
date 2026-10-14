import importlib.util
import json
import socket
import threading
import time
import unittest
from concurrent.futures import ThreadPoolExecutor
from unittest.mock import patch
import urllib.error
import urllib.request
from http.server import ThreadingHTTPServer
from pathlib import Path

spec = importlib.util.spec_from_file_location('studio_server', Path(__file__).with_name('server.py'))
server = importlib.util.module_from_spec(spec)
spec.loader.exec_module(server)


class FakeCamera:
    device = 'Test camera'
    def __enter__(self): return self
    def __exit__(self, *_): pass
    def send(self, image): self.last = image.copy()
    def sleep_until_next_frame(self): time.sleep(.01)


class FakeTracker:
    def __init__(self): self.alive = True
    def poll(self): return None if self.alive else 0
    def terminate(self): self.alive = False
    def kill(self): self.alive = False
    def wait(self, *_): return 0


class BridgeTests(unittest.TestCase):
    def setUp(self):
        self.fake = FakeCamera()
        self.bridge = server.Bridge(port=0, camera_factory=lambda: self.fake)
        self.http = ThreadingHTTPServer(('127.0.0.1', 0), server.handler_for(self.bridge, 0))
        self.port = self.http.server_port
        self.http.RequestHandlerClass = server.handler_for(self.bridge, self.port)
        threading.Thread(target=self.http.serve_forever, daemon=True).start()

    def tearDown(self):
        self.http.shutdown()
        self.http.server_close()
        self.bridge.close()

    def request(self, path, data=None, headers=None):
        headers = headers or {}
        req = urllib.request.Request(f'http://127.0.0.1:{self.port}{path}', data=data, headers=headers)
        return urllib.request.urlopen(req, timeout=3)

    def auth(self, client='tab-a'):
        return {'X-Avatar-Token': server.TOKEN, 'X-Avatar-Client': client}

    def test_origin_and_token_required(self):
        for headers in ({}, {'X-Avatar-Token': server.TOKEN, 'Origin': 'https://example.com'}, {'X-Avatar-Token': server.TOKEN, 'Host': 'evil.test'}):
            with self.assertRaises(urllib.error.HTTPError) as caught:
                self.request('/api/camera/start', b'{}', headers)
            self.assertEqual(caught.exception.code, 403)
        self.assertIsNone(self.bridge.camera)

    def test_files_do_not_escape_web_root(self):
        for path in ('/../README.md', '/%2e%2e/README.md', '/server.py'):
            with self.assertRaises(urllib.error.HTTPError) as caught: self.request(path)
            self.assertEqual(caught.exception.code, 404)
        self.assertEqual(self.request('/').status, 200)

    def test_tracking_timeout_returns_neutral(self):
        self.bridge.tracker = FakeTracker()
        packet = {'face': True, 'shapes': [{'name': 'jawOpen', 'value': 1}]}
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as udp:
            udp.sendto(json.dumps(packet).encode(), self.bridge.udp.getsockname())
        for _ in range(30):
            if self.bridge.tracking_frame().get('face'): break
            time.sleep(.01)
        self.assertTrue(self.bridge.tracking_frame()['face'])
        self.bridge.received -= 1
        self.assertFalse(self.bridge.tracking_frame()['face'])

    def test_invalid_pose_quality_is_rejected(self):
        with self.assertRaises(urllib.error.HTTPError) as caught:
            self.request('/api/tracking/start',b'{"camera":0,"poseQuality":"unknown"}',self.auth())
        self.assertEqual(caught.exception.code,400)
        self.assertIsNone(self.bridge.tracker)

    def test_virtual_frame_color_dimensions_and_stop(self):
        import cv2
        import numpy as np
        self.bridge.start_virtual()
        image = np.zeros((720, 1280, 3), dtype=np.uint8)
        image[:] = [70, 38, 41]
        ok, jpeg = cv2.imencode('.jpg', image)
        self.assertTrue(ok)
        self.bridge.accept_frame(jpeg.tobytes())
        for _ in range(30):
            if self.fake.last[0, 0, 0] > 30: break
            time.sleep(.01)
        self.assertEqual(self.fake.last.shape, image.shape)
        self.assertLess(np.max(np.abs(self.fake.last.astype(int)-image.astype(int))), 4)
        with self.assertRaises(ValueError): self.bridge.accept_frame(b'not a jpeg')
        self.bridge.stop_virtual()
        self.assertIsNone(self.bridge.camera)

    def test_http_complete_camera_cycle(self):
        import cv2
        import numpy as np
        headers = self.auth()
        started = json.load(self.request('/api/camera/start', b'{}', headers))
        self.assertTrue(started['virtualCamera'])
        _, jpeg = cv2.imencode('.jpg', np.full((720, 1280, 3), (40, 120, 200), np.uint8))
        headers['X-Camera-Session'] = started['cameraSession']
        sent = json.load(self.request('/api/frame', jpeg.tobytes(), headers))
        self.assertEqual(sent['framesReceived'], 1)
        self.assertTrue(sent['streaming'])
        stopped = json.load(self.request('/api/camera/stop', b'{}', headers))
        self.assertFalse(stopped['virtualCamera'])
        restarted = json.load(self.request('/api/camera/start', b'{}', headers))
        self.assertNotEqual(started['cameraSession'], restarted['cameraSession'])
        with self.assertRaises(urllib.error.HTTPError) as caught:
            self.request('/api/frame', jpeg.tobytes(), headers)
        self.assertEqual(caught.exception.code, 409)
        self.assertEqual(self.bridge.status()['framesReceived'], 0)

    def test_other_tab_cannot_send_stop_or_start_owned_output(self):
        session = self.bridge.start_virtual('tab-a')
        for path in ('/api/camera/start', '/api/camera/stop'):
            with self.assertRaises(urllib.error.HTTPError) as caught:
                self.request(path, b'{}', self.auth('tab-b'))
            self.assertEqual(caught.exception.code, 409)
        self.assertTrue(self.bridge.status()['virtualCamera'])
        self.assertEqual(self.bridge.status()['cameraSession'], session)

    def test_parallel_start_creates_one_camera(self):
        calls = []
        def factory(): calls.append(1); return self.fake
        self.bridge.camera_factory = factory
        with ThreadPoolExecutor(max_workers=8) as pool:
            sessions = list(pool.map(lambda _: self.bridge.start_virtual('tab-a'), range(8)))
        self.assertEqual(len(calls), 1)
        self.assertEqual(len(set(sessions)), 1)

    def test_startup_failure_can_retry(self):
        def fail(): raise RuntimeError('Driver unavailable')
        self.bridge.camera_factory = fail
        with self.assertRaises(RuntimeError): self.bridge.start_virtual()
        self.assertFalse(self.bridge.status()['virtualCamera'])
        self.bridge.camera_factory = lambda: self.fake
        self.bridge.start_virtual()
        self.assertTrue(self.bridge.status()['virtualCamera'])
        self.assertFalse(self.bridge.status()['cameraError'])

    def test_cancel_inflight_start_never_publishes_camera(self):
        entered, release = threading.Event(), threading.Event()
        class Delayed(FakeCamera):
            def __enter__(self): entered.set(); release.wait(2); return self
        self.bridge.camera_factory = Delayed
        with ThreadPoolExecutor(max_workers=2) as pool:
            started = pool.submit(self.bridge.start_virtual)
            self.assertTrue(entered.wait(1))
            stopped = pool.submit(self.bridge.stop_virtual)
            for _ in range(50):
                if self.bridge.run.stop.is_set(): break
                time.sleep(.005)
            release.set()
            stopped.result(2)
            with self.assertRaises(server.StateConflict): started.result(2)
        self.assertFalse(self.bridge.status()['virtualCamera'])
        self.assertEqual(self.bridge.status()['framesSent'], 0)

    def test_decode_race_cannot_insert_old_frame_after_restart(self):
        import numpy as np
        old = self.bridge.start_virtual('tab-a')
        entered, release = threading.Event(), threading.Event()
        def decode(*_): entered.set(); release.wait(2); return np.ones((720, 1280, 3), np.uint8)
        with patch('cv2.imdecode', decode), ThreadPoolExecutor(max_workers=1) as pool:
            result = pool.submit(self.bridge.accept_frame, b'jpeg', 'tab-a', old)
            self.assertTrue(entered.wait(1))
            self.bridge.stop_virtual('tab-a')
            self.bridge.start_virtual('tab-a')
            release.set()
            with self.assertRaises(server.StateConflict): result.result(2)
        self.assertEqual(self.bridge.status()['framesReceived'], 0)

    def test_stale_video_goes_black(self):
        import cv2
        import numpy as np
        self.bridge.start_virtual()
        _, jpeg = cv2.imencode('.jpg', np.full((720, 1280, 3), 160, np.uint8))
        self.bridge.accept_frame(jpeg.tobytes())
        with self.bridge.lock: self.bridge.run.frame_time -= 2
        for _ in range(50):
            if not self.fake.last.any(): break
            time.sleep(.01)
        self.assertFalse(self.fake.last.any())
        self.assertFalse(self.bridge.status()['streaming'])

    def test_invalid_json_and_frame_return_structured_errors(self):
        for data in (b'[]', b'{', b'null'):
            with self.assertRaises(urllib.error.HTTPError) as caught: self.request('/api/tracking/start', data, self.auth())
            self.assertEqual(caught.exception.code, 400)
            self.assertIn('error', json.load(caught.exception))
        with self.assertRaises(urllib.error.HTTPError) as caught: self.request('/api/camera/start', b'{}', {'X-Avatar-Token': server.TOKEN})
        self.assertEqual(caught.exception.code, 403)

    def test_invalid_packets_are_rejected(self):
        self.assertTrue(server.valid_packet({'face': True, 'pose': False, 'leftHand': [], 'rightHand': []}))
        for packet in ({'face':'yes'}, {'shapes':[None]}, {'rotation':[0]*8}, {'leftHand':[{}]}, {'shapes':[{'name':'jawOpen','value':float('nan')}]}):
            self.assertFalse(server.valid_packet(packet))

    def test_idle_owner_releases_camera(self):
        self.bridge.start_virtual('tab-a')
        self.bridge.camera_activity -= 20
        for _ in range(150):
            if not self.bridge.status()['virtualCamera']: break
            time.sleep(.01)
        self.assertFalse(self.bridge.status()['virtualCamera'])

if __name__ == '__main__': unittest.main()
