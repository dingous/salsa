"""Local webcam -> MediaPipe Tasks -> Unity UDP. No images leave this machine."""
import argparse
import json
import socket
import time
import urllib.request
import uuid
from contextlib import ExitStack
from pathlib import Path
import cv2
import mediapipe as mp
import numpy as np
from hand_assignment import anatomical_hand

MODELS = {
    'face': 'https://storage.googleapis.com/mediapipe-models/face_landmarker/face_landmarker/float16/1/face_landmarker.task',
    'pose': 'https://storage.googleapis.com/mediapipe-models/pose_landmarker/pose_landmarker_lite/float16/1/pose_landmarker_lite.task',
    'pose_full': 'https://storage.googleapis.com/mediapipe-models/pose_landmarker/pose_landmarker_full/float16/1/pose_landmarker_full.task',
    'pose_heavy': 'https://storage.googleapis.com/mediapipe-models/pose_landmarker/pose_landmarker_heavy/float16/1/pose_landmarker_heavy.task',
    'hand': 'https://storage.googleapis.com/mediapipe-models/hand_landmarker/hand_landmarker/float16/1/hand_landmarker.task',
}

def model(name):
    path = Path(__file__).parent / 'models' / (name + '.task')
    path.parent.mkdir(exist_ok=True)
    if not path.exists():
        print('Baixando modelo:', name, flush=True)
        temp = path.with_suffix('.download.' + uuid.uuid4().hex)
        try:
            with urllib.request.urlopen(MODELS[name], timeout=30) as response, temp.open('wb') as stream:
                while chunk := response.read(1024 * 1024):
                    stream.write(chunk)
            temp.replace(path)
        finally:
            temp.unlink(missing_ok=True)
    return path.resolve().as_posix()

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--camera', type=int, default=0)
    parser.add_argument('--port', type=int, default=5055)
    parser.add_argument('--download-only', action='store_true')
    parser.add_argument('--no-preview', action='store_true')
    parser.add_argument('--frames', type=int, default=0)
    parser.add_argument('--hands', action='store_true')
    parser.add_argument('--hand-stride', type=int, choices=(1, 2, 3), default=1)
    parser.add_argument('--pose-quality',choices=('full','heavy'),default='full')
    args = parser.parse_args()
    face_path, pose_path = model('face'), model('pose_'+args.pose_quality)
    hand_path = model('hand') if args.hands or args.download_only else None
    if args.download_only:
        return
    vision = mp.tasks.vision
    face_options = vision.FaceLandmarkerOptions(
        base_options=mp.tasks.BaseOptions(model_asset_buffer=Path(face_path).read_bytes()),
        running_mode=vision.RunningMode.VIDEO, num_faces=1,
        output_face_blendshapes=True, output_facial_transformation_matrixes=True)
    pose_options = vision.PoseLandmarkerOptions(
        base_options=mp.tasks.BaseOptions(model_asset_buffer=Path(pose_path).read_bytes()),
        running_mode=vision.RunningMode.VIDEO, num_poses=1,
        min_pose_detection_confidence=.6,min_pose_presence_confidence=.6,min_tracking_confidence=.6)
    camera = cv2.VideoCapture(args.camera)
    camera.set(cv2.CAP_PROP_FRAME_WIDTH, 640)
    camera.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)
    udp = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        if not camera.isOpened():
            raise RuntimeError('Webcam indisponível. Feche outros aplicativos ou use --camera 1.')
        with ExitStack() as stack:
            face = stack.enter_context(vision.FaceLandmarker.create_from_options(face_options))
            pose = stack.enter_context(vision.PoseLandmarker.create_from_options(pose_options))
            hands = None
            if args.hands:
                hands = stack.enter_context(vision.HandLandmarker.create_from_options(vision.HandLandmarkerOptions(
                    base_options=mp.tasks.BaseOptions(model_asset_buffer=Path(hand_path).read_bytes()),
                    running_mode=vision.RunningMode.VIDEO, num_hands=2)))
            last_timestamp = -1
            count = 0
            hand_result = None
            while True:
                ok, frame = camera.read()
                if not ok:
                    raise RuntimeError('Não foi possível ler a webcam.')
                timestamp = max(last_timestamp + 1, time.monotonic_ns() // 1000000)
                last_timestamp = timestamp
                image = mp.Image(image_format=mp.ImageFormat.SRGB, data=cv2.cvtColor(frame, cv2.COLOR_BGR2RGB))
                f, p = face.detect_for_video(image, timestamp), pose.detect_for_video(image, timestamp)
                # By default all three tasks use the same webcam frame and timestamp.
                if hands is not None and count % args.hand_stride == 0:
                    hand_result = hands.detect_for_video(image, timestamp)
                packet = dict(face=bool(f.face_landmarks), pose=bool(p.pose_world_landmarks), shapes=[], points=[], rotation=[], leftHand=[], rightHand=[], imagePose=[])
                if f.face_blendshapes:
                    packet['shapes'] = [dict(name=s.category_name, value=float(s.score)) for s in f.face_blendshapes[0]]
                if f.facial_transformation_matrixes:
                    packet['rotation'] = np.asarray(f.facial_transformation_matrixes[0])[:3, :3].flatten().tolist()
                if p.pose_world_landmarks:
                    normalized_pose = p.pose_landmarks[0]
                    packet['points'] = [dict(x=float(v.x), y=float(v.y), z=float(v.z),
                                             confidence=float(v.visibility) if 0 <= normalized_pose[j].x <= 1 and 0 <= normalized_pose[j].y <= 1 else 0.0)
                                        for j, v in enumerate(p.pose_world_landmarks[0])]
                    packet['imagePose'] = [dict(x=float(v.x), y=float(v.y), z=float(v.z), confidence=float(v.visibility) if 0 <= v.x <= 1 and 0 <= v.y <= 1 else 0.0) for v in p.pose_landmarks[0]]
                if hand_result:
                    for i, points in enumerate(hand_result.hand_world_landmarks):
                        category = hand_result.handedness[i][0]
                        side = anatomical_hand(category.category_name, hand_result.hand_landmarks[i][0], p.pose_landmarks[0] if p.pose_landmarks else None)
                        normalized = hand_result.hand_landmarks[i]
                        packet[side] = [dict(x=float(v.x), y=float(v.y), z=float(v.z),
                                             confidence=float(category.score) if 0 <= normalized[j].x <= 1 and 0 <= normalized[j].y <= 1 else 0.0)
                                        for j, v in enumerate(points)]
                udp.sendto(json.dumps(packet, separators=(',', ':'), allow_nan=False).encode(), ('127.0.0.1', args.port))
                count += 1
                if args.frames and count >= args.frames:
                    break
                if not args.no_preview:
                    preview = cv2.flip(frame, 1)
                    cv2.putText(preview, 'ROSTO: %s  POSE: %s | Q: sair' % (packet['face'], packet['pose']), (10, 25), cv2.FONT_HERSHEY_SIMPLEX, .55, (80, 255, 100), 1)
                    cv2.imshow('Webcam Avatar - MediaPipe', preview)
                    if cv2.waitKey(1) & 0xff in (ord('q'), 27):
                        break
    finally:
        camera.release()
        udp.close()
        cv2.destroyAllWindows()

if __name__ == '__main__':
    main()
