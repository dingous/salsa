import importlib
import json
from pathlib import Path
import socket
import struct
import sys
import tempfile
import threading
import time
import types
import unittest

import cv2
import numpy as np
from PIL import Image

from generate_avatar import generate
from virtual_camera import read_exact, run


class PhotoGenerationTests(unittest.TestCase):
    def test_frontal_photo_generates_editable_profile(self):
        fixture = Path(__file__).resolve().parent.parent / 'Validation' / 'reference.png'
        if not fixture.exists():
            self.skipTest('Reference fixture is not present')
        with tempfile.TemporaryDirectory() as folder:
            profile = generate(fixture, folder, 'Avatar teste')
            self.assertEqual(profile['source'], 'photo')
            self.assertEqual(profile['name'], 'Avatar teste')
            self.assertEqual(profile['schemaVersion'], 1)
            for color in ('skin', 'hair', 'eyes', 'outfit'):
                self.assertRegex(profile[color], r'^#[0-9A-F]{6}$')
            for field in ('faceWidth', 'eyeSpacing', 'noseSize', 'mouthWidth', 'jawWidth'):
                self.assertTrue(.7 <= profile[field] <= 1.3)
            stored = json.loads((Path(folder) / 'avatar.json').read_text(encoding='utf-8'))
            self.assertEqual(stored, profile)
            with Image.open(Path(folder) / 'reference.png') as thumbnail:
                self.assertLessEqual(max(thumbnail.size), 480)

    def test_no_face_has_actionable_error_and_no_profile(self):
        with tempfile.TemporaryDirectory() as folder:
            source = Path(folder) / 'blank.png'
            Image.new('RGB', (400, 400), 'white').save(source)
            output = Path(folder) / 'output'
            with self.assertRaisesRegex(ValueError, 'exatamente um rosto'):
                generate(source, output, 'Teste')
            self.assertFalse((output / 'avatar.json').exists())

    def test_two_faces_are_rejected(self):
        fixture = Path(__file__).resolve().parent.parent / 'Validation' / 'reference.png'
        if not fixture.exists():
            self.skipTest('Reference fixture is not present')
        with tempfile.TemporaryDirectory() as folder:
            with Image.open(fixture) as image:
                combined = Image.new('RGB', (image.width * 2, image.height))
                combined.paste(image, (0, 0))
                combined.paste(image, (image.width, 0))
                path = Path(folder) / 'two.png'
                combined.save(path)
            with self.assertRaisesRegex(ValueError, 'exatamente um rosto'):
                generate(path, Path(folder) / 'output', 'Teste')

    def test_invalid_photo_has_actionable_error(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'bad.jpg'
            path.write_bytes(b'not a photo')
            with self.assertRaisesRegex(ValueError, 'PNG ou JPEG'):
                generate(path, Path(folder) / 'output', 'Teste')


class TransportTests(unittest.TestCase):
    def test_partial_tcp_reads(self):
        a, b = socket.socketpair()
        try:
            a.sendall(b'ab')
            a.sendall(b'cdef')
            self.assertEqual(read_exact(b, 6), b'abcdef')
            a.close()
            with self.assertRaises(EOFError):
                read_exact(b, 1)
        finally:
            a.close()
            b.close()

    def test_jpeg_reaches_virtual_backend_with_correct_orientation(self):
        frames, errors = [], []
        class Camera:
            device = 'Test virtual camera'
            def __init__(self, **kwargs):
                self.options = kwargs
            def __enter__(self):
                return self
            def __exit__(self, *args):
                pass
            def send(self, frame):
                frames.append(frame.copy())
            def sleep_until_next_frame(self):
                pass
        fake = types.SimpleNamespace(Camera=Camera, PixelFormat=types.SimpleNamespace(BGR='BGR'))
        original = sys.modules.get('pyvirtualcam')
        sys.modules['pyvirtualcam'] = fake
        probe = socket.socket()
        probe.bind(('127.0.0.1', 0))
        port = probe.getsockname()[1]
        probe.close()
        def server():
            try:
                run(port=port, width=1280, height=720)
            except EOFError:
                pass
            except Exception as exc:
                errors.append(exc)
        thread = threading.Thread(target=server, daemon=True)
        thread.start()
        try:
            client = None
            for _ in range(50):
                try:
                    client = socket.create_connection(('127.0.0.1', port), timeout=.5)
                    break
                except OSError:
                    time.sleep(.02)
            self.assertIsNotNone(client)
            frame = np.zeros((720, 1280, 3), np.uint8)
            frame[:360] = [0, 0, 240]
            frame[360:] = [240, 0, 0]
            ok, encoded = cv2.imencode('.jpg', frame)
            self.assertTrue(ok)
            payload = encoded.tobytes()
            with client:
                client.sendall(struct.pack('!I', len(payload)) + payload)
            thread.join(3)
            self.assertFalse(thread.is_alive())
            self.assertFalse(errors)
            self.assertEqual(len(frames), 1)
            self.assertGreater(frames[0][20, 20, 2], 230)
            self.assertGreater(frames[0][700, 20, 0], 230)
        finally:
            if original is None:
                sys.modules.pop('pyvirtualcam', None)
            else:
                sys.modules['pyvirtualcam'] = original


if __name__ == '__main__':
    unittest.main(verbosity=2)
