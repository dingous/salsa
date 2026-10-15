"""Real Unity -> JPEG bridge -> OBS DirectShow receiver; saves avatar output only."""
import json
import re
import subprocess
import time
from pathlib import Path
import cv2
import numpy as np
import sys
import argparse

ROOT = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--executable', type=Path, default=ROOT / 'publish/Windows/AvatarStudio.exe')
parser.add_argument('--virtual-device', type=int)
for flag in ('cartoon', 'gestures', 'preview', 'face-gestures'):
    parser.add_argument('--'+flag, action='store_true')
options = parser.parse_args()
if not options.executable.is_file():
    parser.error('Executável Windows ausente: '+str(options.executable))
from pygrabber.dshow_graph import FilterGraph
devices = FilterGraph().get_input_devices()
if options.virtual_device is None:
    candidates = [i for i, name in enumerate(devices) if name == 'OBS Virtual Camera']
    if len(candidates) != 1:
        parser.error('Selecione um dispositivo OBS Virtual Camera inequívoco com --virtual-device.')
    options.virtual_device = candidates[0]
if not 0 <= options.virtual_device < len(devices) or devices[options.virtual_device] != 'OBS Virtual Camera':
    parser.error('O receptor deve ser OBS Virtual Camera; captura física não será salva como evidência.')
avatar_model = 'cartoon' if '--cartoon' in sys.argv else 'corporate'
log = ROOT / f'Validation/{avatar_model}-live-player.log'
log.parent.mkdir(exist_ok=True)
log.write_text('', encoding='utf-8')
info = subprocess.STARTUPINFO()
info.dwFlags |= subprocess.STARTF_USESHOWWINDOW
info.wShowWindow = 0
gestures = '--gestures' in sys.argv
process = subprocess.Popen([str(options.executable.resolve()), '--corporate-live-e2e', '--corporate-smoke', *(['--gestures'] if gestures else []), *(['--cartoon'] if '--cartoon' in sys.argv else []), *(['--preview'] if '--preview' in sys.argv else []), *(['--face-gestures'] if '--face-gestures' in sys.argv else []), '-logFile', str(log)], startupinfo=info)
receiver = None
try:
    deadline = time.monotonic() + 35
    while time.monotonic() < deadline:
        text = log.read_text(encoding='utf-8', errors='replace')
        if ('CORPORATE_GESTURES_READY' if gestures else 'CORPORATE_LIVE_CAMERA_READY') in text:
            break
        if process.poll() is not None:
            raise AssertionError('Unity exited before virtual output became ready')
        time.sleep(.25)
    else:
        raise AssertionError('Virtual camera startup timeout; see corporate-live-player.log')
    receiver = cv2.VideoCapture(options.virtual_device, cv2.CAP_DSHOW)
    receiver.set(cv2.CAP_PROP_FRAME_WIDTH, 1280)
    receiver.set(cv2.CAP_PROP_FRAME_HEIGHT, 720)
    assert receiver.isOpened(), f'OBS DirectShow receiver unavailable at device {options.virtual_device}'
    start = time.monotonic()
    count = 0
    variation = []
    previous = None
    while time.monotonic() - start < (60 if gestures else 18):
        ok, frame = receiver.read()
        assert ok and frame.shape == (720, 1280, 3), 'Invalid virtual video frame'
        small = cv2.resize(frame, (160, 90)).astype(float)
        if previous is not None:
            variation.append(float(np.abs(small - previous).mean()))
        previous = small
        count += 1
        if count == 60:
            cv2.imwrite(str(ROOT / f'Validation/{avatar_model}-virtual-output.png'), frame)
            assert frame[130:680, 360:920].std() > 15, 'Avatar not visible in virtual output'
    elapsed = time.monotonic() - start
    fps = count / elapsed
    assert fps >= 15, f'Virtual receiver too slow: {fps:.1f} FPS'
    assert max(variation, default=0) > .03, 'No avatar movement reached the receiver'
    receiver.release()
    receiver = None
    code = process.wait(timeout=20)
    text = log.read_text(encoding='utf-8', errors='replace')
    passed = code == 0 and 'CORPORATE_LIVE_RESULT=True' in text
    result = {'result':'PASS' if passed else 'FAIL', 'exitCode':code, 'unityChecksPassed':'CORPORATE_LIVE_RESULT=True' in text, 'avatar':'cartoon' if '--cartoon' in sys.argv else 'corporate', 'frames':count, 'seconds':round(elapsed,2), 'receiverFps':round(fps,2),
              'validationScope':'unilateral winks and reopening' if '--face-gestures' in sys.argv else 'arms head reach and fingers' if gestures else 'virtual transmission',
              'resolution':[1280,720], 'physicalTracking':True, 'virtualDriver':'OBS/DirectShow',
              'virtualDeviceIndex':options.virtual_device, 'virtualDeviceName':devices[options.virtual_device],
              'cleanScreenIncluded':not gestures, 'cleanScreenMotionSource':'real webcam' if gestures else 'animated avatar preview',
              'savedRawWebcamImages':False,
              'faceSeen':'face=True' in text, 'poseSeen':'pose=True' in text, 'handsSeen':'hands=True' in text}
    if '--face-gestures' in sys.argv:
        wink = re.search(r'CORPORATE_WINK_SAMPLES left=(\d+) right=(\d+) reopened=(\d+)', text)
        result['winkRenderSamples'] = dict(zip(('left','right','reopened'), map(int,wink.groups()))) if wink else {}
    elif gestures:
        result['gestureAudit'] = json.loads((options.executable.resolve().parent.parent / 'Validation/corporate-gesture-audit.json').read_text(encoding='utf-8'))
    (ROOT / f'Validation/{avatar_model}-camera-result.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps(result))
    assert passed, f'Unity hardware/gesture checks failed; see {avatar_model}-camera-result.json'
except Exception as error:
    failure = locals().get('result', {'avatar': avatar_model, 'savedRawWebcamImages': False})
    failure.update(result='FAIL', error=type(error).__name__+': '+str(error), log=str(log))
    (ROOT / f'Validation/{avatar_model}-camera-result.json').write_text(json.dumps(failure, indent=2), encoding='utf-8')
    print(json.dumps(failure), flush=True)
    raise
finally:
    if receiver is not None:
        receiver.release()
    if process.poll() is None:
        try:
            # Let the instrumented application close its webcam and OBS bridge normally.
            process.wait(timeout=40)
        except subprocess.TimeoutExpired:
            subprocess.run(['taskkill.exe', '/PID', str(process.pid), '/T', '/F'], startupinfo=info, capture_output=True, timeout=10)
            process.wait(timeout=10)
