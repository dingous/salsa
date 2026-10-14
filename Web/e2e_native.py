"""Real Windows receiver checks. Browser actions are performed separately through the UI."""
import argparse
import datetime
import json
import time
import urllib.error
import urllib.request
import uuid
from pathlib import Path

import cv2
import numpy as np

RESULTS = Path(__file__).resolve().parents[1] / 'Validation/web-e2e-results.json'


class API:
    def __init__(self, base):
        self.base = base
        self.client = 'e2e-' + uuid.uuid4().hex
        self.token = json.load(urllib.request.urlopen(base+'/api/session', timeout=3))['token']

    def call(self, path, body=None, session=None):
        headers = {'X-Avatar-Token': self.token, 'X-Avatar-Client': self.client}
        if isinstance(body, bytes):
            headers.update({'Content-Type':'image/jpeg', 'X-Camera-Session':session})
        elif body is not None:
            body = json.dumps(body).encode()
            headers['Content-Type'] = 'application/json'
        req = urllib.request.Request(self.base+'/api/'+path, data=body, headers=headers)
        with urllib.request.urlopen(req, timeout=12) as response:
            return json.load(response)


def open_receiver(index):
    camera = cv2.VideoCapture(index, cv2.CAP_DSHOW)
    camera.set(cv2.CAP_PROP_FRAME_WIDTH, 1280)
    camera.set(cv2.CAP_PROP_FRAME_HEIGHT, 720)
    if not camera.isOpened():
        raise AssertionError('DirectShow receiver could not open the virtual camera.')
    return camera


def observe(api, args):
    before = api.call('status')
    assert before['virtualCamera'], 'Start the virtual camera in the web UI first.'
    camera = open_receiver(args.device)
    started = time.monotonic()
    count = 0
    motion = []
    previous = None
    corners = []
    try:
        while time.monotonic()-started < args.seconds:
            ok, image = camera.read()
            assert ok, 'Virtual camera did not return a frame.'
            assert image.shape == (720, 1280, 3), str(image.shape)
            count += 1
            corners.append(image[10, 10].astype(float))
            small = cv2.resize(image, (160, 90)).astype(float)
            if previous is not None: motion.append(float(np.abs(small-previous).mean()))
            previous = small
        expected = {'studio':[70,38,41], 'green':[0,255,0], 'dark':[31,19,16]}[args.background]
        color = np.median(corners, axis=0)
        assert np.max(np.abs(color-expected)) < 12, f'Unexpected background: {color.tolist()}'
        if args.motion: assert max(motion, default=0) > .05, 'Avatar video remained static.'
        if args.save:
            Path(args.save).parent.mkdir(parents=True, exist_ok=True)
            cv2.imwrite(args.save, image)
    finally:
        camera.release()
    after = api.call('status')
    elapsed = time.monotonic()-started
    assert after['streaming'] and after['frameAgeMs'] < 1200, 'Browser stopped sending current frames.'
    input_fps = (after['framesReceived']-before['framesReceived'])/elapsed
    assert input_fps >= 4, f'Browser input rate too low: {input_fps:.1f}'
    return dict(receiverFrames=count, resolution=[1280,720], background=color.tolist(),
                browserInputFps=round(input_fps,2), receiverFps=round(count/elapsed,2),
                frameAgeMs=after['frameAgeMs'], maxMotion=round(max(motion,default=0),3))


def pattern_cycle(api, args):
    state = api.call('camera/start', {})
    session = state['cameraSession']
    camera = None
    try:
        # Asymmetric colors verify channels, horizontal orientation and vertical orientation.
        image = np.zeros((720,1280,3), np.uint8)
        image[:360,:640] = [10,20,230]
        image[:360,640:] = [230,20,10]
        image[360:,:] = [20,220,30]
        _, jpeg = cv2.imencode('.jpg', image)
        api.call('frame', jpeg.tobytes(), session)
        camera = open_receiver(args.device)
        for _ in range(12):
            api.call('frame', jpeg.tobytes(), session)
            ok, received = camera.read()
            assert ok
        assert received.shape == image.shape
        for y,x in [(100,100),(100,1100),(600,100),(600,1100)]:
            assert np.max(np.abs(received[y,x].astype(int)-image[y,x].astype(int))) < 12, 'Mirrored, flipped or incorrect color channels.'
        # No new browser frames: the real receiver must eventually get black.
        deadline = time.monotonic()+4
        black = False
        while time.monotonic()<deadline:
            ok, received = camera.read()
            if ok and received.mean()<2: black=True;break
        assert black, 'Stale output did not turn black.'
        camera.release();camera=None
        stopped=api.call('camera/stop',{})
        assert not stopped['virtualCamera']
        restarted=api.call('camera/start',{})
        assert restarted['cameraSession']!=session
        try:
            api.call('frame', jpeg.tobytes(), session)
            raise AssertionError('An old frame was accepted after restart.')
        except urllib.error.HTTPError as error:
            assert error.code==409
        api.call('frame', jpeg.tobytes(), restarted['cameraSession'])
        assert api.call('status')['framesReceived']==1
        return dict(resolution=[1280,720],colorOrientation=True,staleVideoBlack=True,stopRestart=True,oldSessionRejected=True)
    finally:
        if camera: camera.release()
        api.call('camera/stop',{})


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('mode',choices=['observe','pattern'])
    parser.add_argument('--url',default='http://127.0.0.1:8765')
    parser.add_argument('--device',type=int,default=1,help='DirectShow index of OBS Virtual Camera, not the physical webcam')
    parser.add_argument('--seconds',type=float,default=3)
    parser.add_argument('--background',choices=['studio','green','dark'],default='studio')
    parser.add_argument('--motion',action='store_true')
    parser.add_argument('--save')
    parser.add_argument('--case',default='native-receiver')
    args=parser.parse_args()
    result={'case':args.case,'time':datetime.datetime.now(datetime.timezone(datetime.timedelta(hours=-3))).isoformat()}
    try:
        api=API(args.url)
        result.update(observe(api,args) if args.mode=='observe' else pattern_cycle(api,args))
        result['passed']=True
    except Exception as error:
        result.update(passed=False,error=str(error))
        raise
    finally:
        RESULTS.parent.mkdir(parents=True,exist_ok=True)
        prior=json.loads(RESULTS.read_text()) if RESULTS.exists() else []
        prior.append(result)
        RESULTS.write_text(json.dumps(prior,indent=2),encoding='utf-8')
        print(json.dumps(result),flush=True)


if __name__=='__main__': main()
