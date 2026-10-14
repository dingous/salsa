"""Unity camera JPEG frames via localhost TCP -> installed virtual camera driver."""
import argparse
import socket
import struct
import sys
import cv2
import numpy as np


def read_exact(connection, length):
    chunks = bytearray()
    while len(chunks) < length:
        part = connection.recv(length - len(chunks))
        if not part:
            raise EOFError('Unity desconectado')
        chunks.extend(part)
    return bytes(chunks)


def run(port=5056, width=1280, height=720, fps=30, check=False):
    import pyvirtualcam
    with pyvirtualcam.Camera(width=width, height=height, fps=fps,
                             fmt=pyvirtualcam.PixelFormat.BGR, backend='obs') as camera:
        print('READY|' + camera.device, flush=True)
        if check:
            return
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server:
            server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            server.bind(('127.0.0.1', port))
            server.listen(1)
            server.settimeout(20)
            connection, _ = server.accept()
            with connection:
                connection.settimeout(10)
                while True:
                    size, = struct.unpack('!I', read_exact(connection, 4))
                    if not 0 < size <= 4 * 1024 * 1024:
                        raise ValueError('Frame inválido')
                    image = cv2.imdecode(np.frombuffer(read_exact(connection, size), dtype=np.uint8), cv2.IMREAD_COLOR)
                    if image is None or image.shape[:2] != (height, width):
                        raise ValueError('Resolução do frame inválida')
                    camera.send(image)
                    camera.sleep_until_next_frame()


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--port', type=int, default=5056)
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    try:
        run(port=args.port, check=args.check)
    except EOFError:
        pass
    except Exception as exc:
        print('DETAIL|' + str(exc), flush=True)
        print('Câmera virtual indisponível. Instale o OBS Studio com a câmera virtual e mantenha a saída virtual do OBS desligada.', file=sys.stderr, flush=True)
        sys.exit(1)
