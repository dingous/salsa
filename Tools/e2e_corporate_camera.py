"""Real Unity -> JPEG bridge -> OBS DirectShow receiver; saves avatar output only."""
import json
import subprocess
import time
from pathlib import Path
import cv2
import numpy as np
import sys

ROOT = Path(__file__).resolve().parents[1]
avatar_model = 'cartoon' if '--cartoon' in sys.argv else 'corporate'
log = ROOT / f'Validation/{avatar_model}-live-player.log'
log.parent.mkdir(exist_ok=True)
log.write_text('', encoding='utf-8')
info = subprocess.STARTUPINFO()
info.dwFlags |= subprocess.STARTF_USESHOWWINDOW
info.wShowWindow = 0
gestures = '--gestures' in sys.argv
process = subprocess.Popen([str(ROOT / 'CorporateUnity/Build/CorporateAvatarStudio.exe'), '--corporate-live-e2e', *(['--gestures'] if gestures else []), *(['--cartoon'] if '--cartoon' in sys.argv else []), '-logFile', str(log)], startupinfo=info)
receiver = None
try:
    deadline = time.monotonic() + 35
    while time.monotonic() < deadline:
        text = log.read_text(encoding='utf-8', errors='replace')
        if 'CORPORATE_LIVE_CAMERA_READY' in text:
            break
        if process.poll() is not None:
            raise AssertionError('Unity exited before virtual output became ready')
        time.sleep(.25)
    else:
        raise AssertionError('Virtual camera startup timeout; see corporate-live-player.log')
    receiver = cv2.VideoCapture(1, cv2.CAP_DSHOW)
    receiver.set(cv2.CAP_PROP_FRAME_WIDTH, 1280)
    receiver.set(cv2.CAP_PROP_FRAME_HEIGHT, 720)
    assert receiver.isOpened(), 'OBS DirectShow receiver unavailable at device 1'
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
    result = {'result':'PASS' if passed else 'FAIL', 'avatar':'cartoon' if '--cartoon' in sys.argv else 'corporate', 'frames':count, 'seconds':round(elapsed,2), 'receiverFps':round(fps,2),
              'resolution':[1280,720], 'physicalTracking':True, 'virtualDriver':'OBS/DirectShow',
              'cleanScreenIncluded':not gestures, 'cleanScreenMotionSource':'real webcam' if gestures else 'animated avatar preview',
              'savedRawWebcamImages':False,
              'faceSeen':'face=True' in text, 'poseSeen':'pose=True' in text, 'handsSeen':'hands=True' in text}
    if gestures:
        result['gestureAudit'] = json.loads((ROOT / 'CorporateUnity/Validation/corporate-gesture-audit.json').read_text(encoding='utf-8'))
    (ROOT / 'Validation/corporate-camera-result.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    (ROOT / f'Validation/{avatar_model}-camera-result.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps(result))
    assert passed, 'Unity hardware/gesture checks failed; see corporate-camera-result.json'
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
