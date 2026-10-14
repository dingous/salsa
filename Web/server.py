"""Local web studio bridge: tracking and rendered avatar -> OBS virtual camera."""
import argparse
import base64
import hashlib
import json
import math
import mimetypes
import secrets
import socket
import subprocess
import sys
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import unquote, urlsplit

ROOT = Path(__file__).resolve().parent
PROJECT = ROOT.parent
sys.path.insert(0,str(PROJECT / 'Tracker'))
import dingous_lab
import dingous_partner
from local_ai import ensure_local_ai
from voice_service import VoiceService
VERSION = 2
REVISION = hashlib.sha256(Path(__file__).read_bytes()).hexdigest()
TOKEN = secrets.token_urlsafe(32)
IMPORT_MAP = b'{"imports":{"three":"./vendor/three/package/build/three.module.js","three/addons/":"./vendor/three/package/examples/jsm/","@pixiv/three-vrm":"./vendor/three-vrm/package/lib/three-vrm.module.js"}}'
IMPORT_HASH = base64.b64encode(hashlib.sha256(IMPORT_MAP).digest()).decode()


class StateConflict(RuntimeError):
    pass


class CameraRun:
    def __init__(self, owner):
        self.owner = owner
        self.session = secrets.token_urlsafe(20)
        self.stop = threading.Event()
        self.ready = threading.Event()
        self.thread = None
        self.camera = None
        self.error = ''
        self.phase = 'starting'
        self.frame = None
        self.frame_time = 0
        self.sent = self.received = self.blank = 0


def valid_packet(packet):
    if not isinstance(packet, dict) or not isinstance(packet.get('face', False), bool) or not isinstance(packet.get('pose', False), bool):
        return False
    def number(value):
        return isinstance(value, (int, float)) and not isinstance(value, bool) and math.isfinite(value)
    shapes = packet.get('shapes', [])
    if not isinstance(shapes, list) or len(shapes) > 100:
        return False
    if any(not isinstance(s, dict) or not isinstance(s.get('name'), str) or len(s['name']) > 80 or not number(s.get('value')) for s in shapes):
        return False
    rotation = packet.get('rotation', [])
    if not isinstance(rotation, list) or len(rotation) not in (0, 9) or any(not number(v) for v in rotation):
        return False
    for key, count in [('points', 33), ('imagePose', 33), ('leftHand', 21), ('rightHand', 21)]:
        points = packet.get(key, [])
        if not isinstance(points, list) or len(points) not in (0, count):
            return False
        if any(not isinstance(p, dict) or any(not number(p.get(k)) for k in ('x', 'y', 'z', 'confidence')) for p in points):
            return False
    return True


class Bridge:
    def __init__(self, port=5057, camera_factory=None, idle_timeout=15):
        self.camera_factory = camera_factory
        self.idle_timeout = idle_timeout
        self.lock = threading.RLock()
        self.tracking_control = threading.RLock()
        self.tracker = None
        self.tracker_owner = None
        self.tracker_error = ''
        self.packet = None
        self.received = 0
        self.tracker_activity = 0
        self.camera_activity = 0
        self.run = None
        self.closing = threading.Event()
        self.udp = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.udp.bind(('127.0.0.1', port))
        self.port = self.udp.getsockname()[1]
        self.udp.settimeout(.3)
        self.receiver = threading.Thread(target=self.receive, daemon=True)
        self.receiver.start()
        threading.Thread(target=self.watchdog, daemon=True).start()

    @property
    def camera(self):
        return self.run.camera if self.run and self.run.phase == 'running' else None

    def touch(self, client):
        with self.lock:
            if self.tracker_owner == client:
                self.tracker_activity = time.monotonic()
            if self.run and self.run.owner == client:
                self.camera_activity = time.monotonic()

    def watchdog(self):
        while not self.closing.wait(.5):
            now = time.monotonic()
            with self.lock:
                tracker_owner = self.tracker_owner if now-self.tracker_activity > self.idle_timeout else None
                camera_run = self.run if self.run and now-self.camera_activity > self.idle_timeout else None
            if tracker_owner:
                self.stop_tracking(tracker_owner)
            if camera_run:
                self.stop_virtual(camera_run.owner, expected=camera_run)

    def receive(self):
        while not self.closing.is_set():
            try:
                data, _ = self.udp.recvfrom(65535)
                packet = json.loads(data)
                if not valid_packet(packet):
                    continue
                with self.lock:
                    if self.tracker and self.tracker.poll() is None:
                        self.packet, self.received = packet, time.monotonic()
            except (socket.timeout, ValueError):
                pass
            except OSError:
                break

    def start_tracking(self, index, client='internal', pose_quality='full'):
        if pose_quality not in ('full','heavy'):raise ValueError('Escolha a precisão Full ou Heavy.')
        if not isinstance(index, int) or isinstance(index, bool) or not 0 <= index <= 9:
            raise ValueError('Escolha uma webcam de 0 a 9.')
        with self.tracking_control, self.lock:
            if self.tracker and self.tracker.poll() is None:
                if self.tracker_owner != client:
                    raise StateConflict('A webcam está em uso por outra aba do Studio.')
                return
            self.tracker_error = ''
            self.packet = None
            self.received = 0
            args = [sys.executable, '-u', str(PROJECT / 'Tracker/tracker.py'), '--port', str(self.port),
                    '--camera', str(index), '--no-preview', '--hands', '--pose-quality',pose_quality]
            process = subprocess.Popen(args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                                       text=True, encoding='utf-8', errors='replace',
                                       creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
            self.tracker = process
            self.tracker_owner = client
            self.tracker_activity = time.monotonic()
        def drain():
            try:
                for _ in process.stdout:
                    pass
                process.wait()
                with self.lock:
                    if self.tracker is process and process.returncode:
                        self.packet = None
                        self.tracker_error = 'Não foi possível abrir a webcam. Feche outros aplicativos que usam a câmera e tente outra entrada.'
            finally:
                process.stdout.close()
        threading.Thread(target=drain, daemon=True).start()

    def stop_tracking(self, client=None):
        with self.tracking_control:
            with self.lock:
                if client and self.tracker_owner and self.tracker_owner != client:
                    raise StateConflict('Esta webcam pertence a outra aba do Studio.')
                process, self.tracker = self.tracker, None
                self.tracker_owner = None
                self.tracker_error = ''
                self.packet = None
                self.received = 0
            if process and process.poll() is None:
                process.terminate()
                try:
                    process.wait(3)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait(3)

    def start_virtual(self, client='internal'):
        with self.lock:
            if self.run and self.run.thread.is_alive():
                if self.run.owner != client:
                    raise StateConflict('A câmera virtual está em uso por outra aba do Studio.')
                if self.run.stop.is_set():
                    raise StateConflict('A câmera virtual está encerrando. Aguarde e tente novamente.')
                run = self.run
            else:
                run = CameraRun(client)
                self.run = run
                run.thread = threading.Thread(target=self.send_virtual, args=(run,), daemon=True)
                run.thread.start()
            self.camera_activity = time.monotonic()
        if not run.ready.wait(8):
            self.stop_virtual(client, expected=run)
            raise RuntimeError('A câmera virtual demorou a responder.')
        if run.stop.is_set():
            raise StateConflict('Ativação da câmera virtual cancelada.')
        if run.error:
            raise RuntimeError(run.error)
        return run.session

    def send_virtual(self, run):
        import numpy as np
        try:
            if self.camera_factory is None:
                import pyvirtualcam
                factory = lambda: pyvirtualcam.Camera(width=1280, height=720, fps=30,
                                                      fmt=pyvirtualcam.PixelFormat.BGR, backend='obs')
            else:
                factory = self.camera_factory
            with factory() as camera:
                with self.lock:
                    if run.stop.is_set() or self.run is not run:
                        return
                    run.camera = camera
                    run.phase = 'running'
                    run.ready.set()
                blank = np.zeros((720, 1280, 3), dtype=np.uint8)
                while not run.stop.is_set():
                    with self.lock:
                        fresh = run.frame is not None and time.monotonic()-run.frame_time < 1.2
                        image = run.frame if fresh else blank
                    camera.send(image)
                    with self.lock:
                        run.sent += 1
                        run.blank += int(not fresh)
                    camera.sleep_until_next_frame()
        except Exception as exc:
            with self.lock:
                if not run.stop.is_set():
                    run.error = 'Câmera virtual indisponível. Instale o OBS Studio e deixe a câmera virtual do próprio OBS desligada antes de ativar aqui.'
            if not run.stop.is_set():
                print('Virtual camera:', str(exc), flush=True)
        finally:
            with self.lock:
                run.camera = None
                run.phase = 'error' if run.error else 'stopped'
                run.frame = None
                run.ready.set()

    def accept_frame(self, data, client='internal', session=None):
        import cv2
        import numpy as np
        with self.lock:
            run = self.run
            if not run or run.phase != 'running' or run.stop.is_set():
                raise StateConflict('A saída da câmera foi encerrada.')
            if run.owner != client or (session is not None and run.session != session):
                raise StateConflict('Este frame pertence a outra sessão de transmissão.')
        try:
            image = cv2.imdecode(np.frombuffer(data, np.uint8), cv2.IMREAD_COLOR)
        except cv2.error as exc:
            raise ValueError('Frame JPEG inválido.') from exc
        if image is None or image.shape != (720, 1280, 3):
            raise ValueError('A saída do avatar deve ter resolução 1280 × 720.')
        with self.lock:
            if self.run is not run or run.stop.is_set():
                raise StateConflict('A sessão da câmera mudou durante o envio.')
            run.frame, run.frame_time = image, time.monotonic()
            run.received += 1
            self.camera_activity = time.monotonic()

    def stop_virtual(self, client=None, expected=None):
        with self.lock:
            run = self.run
            if not run or (expected is not None and run is not expected):
                return
            if client and run.owner != client and run.phase not in ('stopped', 'error'):
                raise StateConflict('Esta câmera virtual pertence a outra aba do Studio.')
            run.stop.set()
            run.frame = None
            run.error = ''
            run.phase = 'stopping' if run.thread.is_alive() else 'stopped'
        run.thread.join(3)

    def status(self):
        with self.lock:
            tracking = bool(self.tracker and self.tracker.poll() is None)
            run = self.run
            active = bool(run and run.phase == 'running' and not run.stop.is_set())
            age = round((time.monotonic()-run.frame_time)*1000) if run and run.frame is not None else None
            return dict(version=VERSION, tracking=tracking, connected=tracking and time.monotonic()-self.received < .8,
                        trackerOwner=self.tracker_owner if tracking else None, trackerError=self.tracker_error,
                        virtualCamera=active, cameraState=run.phase if run else 'stopped',
                        cameraOwner=run.owner if run and run.phase in ('starting', 'running', 'stopping') else None,
                        cameraSession=run.session if active else None, cameraError=run.error if run else '',
                        device=getattr(run.camera, 'device', '') if active else '', framesSent=run.sent if run else 0,
                        framesReceived=run.received if run else 0, blankFrames=run.blank if run else 0,
                        frameAgeMs=age, streaming=active and age is not None and age < 1200)

    def tracking_frame(self):
        with self.lock:
            live = self.tracker and self.tracker.poll() is None and time.monotonic()-self.received < .6
            return self.packet if live and self.packet else dict(face=False, pose=False, shapes=[], points=[])

    def close(self):
        if hasattr(self,"voice"):self.voice.stop()
        self.closing.set()
        self.stop_tracking()
        self.stop_virtual()
        self.udp.close()
        self.receiver.join(1)


def handler_for(bridge, port):
    if not hasattr(bridge,"voice"):bridge.voice=VoiceService()
    class Handler(BaseHTTPRequestHandler):
        def setup(self):
            super().setup()
            self.connection.settimeout(12)

        def log_message(self, *_):
            pass

        def reply(self, code, body, content_type='application/json'):
            if isinstance(body, (dict, list)):
                body = json.dumps(body, ensure_ascii=False, allow_nan=False).encode()
            self.send_response(code)
            self.send_header('Content-Type', content_type)
            self.send_header('Content-Length', str(len(body)))
            self.send_header('Cache-Control', 'no-store')
            self.send_header('X-Content-Type-Options', 'nosniff')
            self.send_header('Referrer-Policy', 'no-referrer')
            self.send_header('Content-Security-Policy', f"default-src 'self'; script-src 'self' 'wasm-unsafe-eval' 'sha256-{IMPORT_HASH}'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; connect-src 'self' blob:; frame-ancestors 'none'")
            self.end_headers()
            try:
                self.wfile.write(body)
            except (BrokenPipeError, ConnectionResetError):
                pass

        def authorized(self, token=False):
            host = self.headers.get('Host', '')
            if host not in (f'127.0.0.1:{port}', f'localhost:{port}'):
                self.reply(403, {'error': 'Host não autorizado.'})
                return False
            origin = self.headers.get('Origin')
            if origin and origin != 'http://' + host:
                self.reply(403, {'error': 'Origem não autorizada.'})
                return False
            if token and not secrets.compare_digest(self.headers.get('X-Avatar-Token', ''), TOKEN):
                self.reply(403, {'error': 'Sessão inválida. Reconecte a ponte local.'})
                return False
            if token:
                client = self.headers.get('X-Avatar-Client', '')
                if not client or len(client) > 80 or not all(c.isalnum() or c in '-_' for c in client):
                    self.reply(403, {'error': 'Cliente inválido. Recarregue a página.'})
                    return False
                bridge.touch(client)
            return True

        def do_GET(self):
            if not self.authorized():
                return
            path = unquote(urlsplit(self.path).path)
            if path == '/api/session':
                self.reply(200, {'app': 'AvatarStudioWeb', 'version': VERSION, 'revision': REVISION, 'token': TOKEN})
            elif path in ('/api/status', '/api/tracking'):
                if self.authorized(True):
                    self.reply(200, bridge.status() if path.endswith('status') else bridge.tracking_frame())
            else:
                file = (ROOT / ('index.html' if path == '/' else path.lstrip('/'))).resolve()
                if not file.is_relative_to(ROOT) or not file.is_file() or file.suffix not in ('.html', '.js', '.mjs', '.wasm', '.task', '.css', '.json', '.vrm', '.txt', '.md', '.png', '.jpg', '.svg'):
                    self.reply(404, {'error': 'Arquivo não encontrado.'})
                    return
                mime = 'text/javascript' if file.suffix in ('.js','.mjs') else 'application/wasm' if file.suffix=='.wasm' else mimetypes.guess_type(file.name)[0] or 'application/octet-stream'
                self.reply(200, file.read_bytes(), mime)

        def do_POST(self):
            try:
                length = int(self.headers.get('Content-Length', '0'))
                if not 0 <= length <= 4*1024*1024:
                    self.reply(413, {'error': 'Requisição muito grande.'})
                    return
                data = self.rfile.read(length)
                if len(data) != length:
                    raise ValueError('Requisição incompleta.')
                if not self.authorized(True):
                    return
                path = urlsplit(self.path).path
                client = self.headers['X-Avatar-Client']
                if path == '/api/frame':
                    session = self.headers.get('X-Camera-Session')
                    if not session:
                        raise StateConflict('Sessão de transmissão ausente.')
                    bridge.accept_frame(data, client, session)
                else:
                    body = json.loads(data)
                    if not isinstance(body, dict):
                        raise ValueError('A operação precisa de um objeto JSON.')
                    if path in ('/api/voice/start','/api/voice/stop','/api/voice/state'):
                        if path.endswith('/start'):bridge.voice.start(client)
                        elif path.endswith('/stop'):bridge.voice.stop(client)
                        self.reply(200,bridge.voice.status(client));return
                    elif path == '/api/partner/ask':
                        settings=dingous_partner.load_settings()
                        if settings['backend']=='ollama':ensure_local_ai()
                        self.reply(200,dingous_partner.answer(body.get('question')));return
                    elif path == '/api/partner/remember':
                        self.reply(200,dict(saved=dingous_partner.remember(body.get('text'))));return
                    elif path == '/api/partner/approve':
                        self.reply(200,dict(dataset=dingous_partner.approve(body)));return
                    elif path == '/api/laboratory/generate':
                        if body.get('demo') is not True:ensure_local_ai()
                        plan=dingous_lab.validate(dingous_lab.DEMO) if body.get('demo') is True else dingous_lab.generate(body.get('prompt',''))
                        self.reply(200,dict(plan=plan,frames=list(dingous_lab.frames(plan)),source='programmed-demo' if body.get('demo') is True else 'ollama-local'))
                        return
                    elif path == '/api/tracking/start':
                        bridge.start_tracking(body.get('camera', 0), client, body.get('poseQuality','full'))
                    elif path == '/api/tracking/stop':
                        bridge.stop_tracking(client)
                    elif path == '/api/camera/start':
                        bridge.start_virtual(client)
                    elif path == '/api/camera/stop':
                        bridge.stop_virtual(client)
                    else:
                        self.reply(404, {'error': 'Operação desconhecida.'})
                        return
                self.reply(200, bridge.status())
            except StateConflict as exc:
                self.reply(409, {'error': str(exc)})
            except (ValueError, RuntimeError, OSError) as exc:
                self.reply(400, {'error': str(exc)})
    return Handler


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--port', type=int, default=8765)
    parser.add_argument('--tracking-port', type=int, default=5057)
    args = parser.parse_args()
    bridge = Bridge(port=args.tracking_port)
    server = ThreadingHTTPServer(('127.0.0.1', args.port), handler_for(bridge, args.port))
    print(f'Avatar Studio Web v{VERSION}: http://127.0.0.1:{args.port}', flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()
        bridge.close()
