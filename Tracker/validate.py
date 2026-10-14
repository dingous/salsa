"""Smoke test models and optionally one webcam frame; does not save images."""
import argparse
import cv2
import mediapipe as mp
import numpy as np
from pathlib import Path
from tracker import model

parser = argparse.ArgumentParser()
parser.add_argument('--camera', type=int)
args = parser.parse_args()
image = np.zeros((480, 640, 3), dtype=np.uint8)
if args.camera is not None:
    capture = cv2.VideoCapture(args.camera)
    try:
        ok, image = capture.read()
        if not ok:
            raise RuntimeError('Webcam indisponível')
        image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
        print('Webcam: leitura OK')
    finally:
        capture.release()
vision = mp.tasks.vision
with vision.FaceLandmarker.create_from_options(vision.FaceLandmarkerOptions(
    base_options=mp.tasks.BaseOptions(model_asset_buffer=Path(model('face')).read_bytes()),
    output_face_blendshapes=True, output_facial_transformation_matrixes=True)) as face:
    result = face.detect(mp.Image(image_format=mp.ImageFormat.SRGB, data=image))
    print('Face: inferência OK; rostos:', len(result.face_landmarks))
with vision.PoseLandmarker.create_from_options(vision.PoseLandmarkerOptions(
    base_options=mp.tasks.BaseOptions(model_asset_buffer=Path(model('pose')).read_bytes()))) as pose:
    result = pose.detect(mp.Image(image_format=mp.ImageFormat.SRGB, data=image))
    print('Pose: inferência OK; corpos:', len(result.pose_landmarks))
