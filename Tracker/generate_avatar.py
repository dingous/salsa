"""Create an editable cartoon profile from one frontal photo, entirely locally."""
import argparse
import json
import math
from pathlib import Path
import sys

import cv2
import mediapipe as mp
import numpy as np
from PIL import Image, ImageOps, UnidentifiedImageError
from tracker import model


def color_hex(rgb):
    return '#%02X%02X%02X' % tuple(int(x) for x in np.clip(rgb, 0, 255))


def generate(source, destination, name):
    source = Path(source)
    if not source.is_file():
        raise ValueError('A foto selecionada não existe mais. Selecione novamente.')
    if source.stat().st_size > 25 * 1024 * 1024:
        raise ValueError('Escolha uma foto de até 25 MB.')
    try:
        with Image.open(source) as original:
            if original.width * original.height > 40_000_000:
                raise ValueError('A foto é grande demais. Reduza para até 40 megapixels.')
            image = ImageOps.exif_transpose(original).convert('RGB')
            image.thumbnail((1280, 1280), Image.Resampling.LANCZOS)
    except (UnidentifiedImageError, OSError) as exc:
        raise ValueError('Não foi possível abrir a foto. Use PNG ou JPEG.') from exc
    rgb = np.array(image)
    h, w = rgb.shape[:2]
    vision = mp.tasks.vision
    options = vision.FaceLandmarkerOptions(
        base_options=mp.tasks.BaseOptions(model_asset_buffer=Path(model('face')).read_bytes()),
        num_faces=2, output_face_blendshapes=True,
        output_facial_transformation_matrixes=True)
    with vision.FaceLandmarker.create_from_options(options) as face:
        result = face.detect(mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb))
    if len(result.face_landmarks) != 1:
        raise ValueError('Use uma foto nítida com exatamente um rosto, de frente e sem obstruções.')
    landmarks = result.face_landmarks[0]
    points = np.array([(v.x * w, v.y * h, v.z * w) for v in landmarks])
    distance = lambda a, b: float(np.linalg.norm(points[a, :2] - points[b, :2]))
    width, height = distance(234, 454), distance(10, 152)
    if width < 70 or height < 90:
        raise ValueError('O rosto está pequeno na foto. Use um retrato mais próximo.')
    eyes = distance(33, 263)
    nose_ratio = abs(points[1, 0] - (points[33, 0] + points[263, 0]) / 2) / max(eyes, 1)
    roll = abs(math.degrees(math.atan2(points[263, 1] - points[33, 1], points[263, 0] - points[33, 0])))
    if nose_ratio > .19 or roll > 22:
        raise ValueError('Escolha um retrato frontal, com a cabeça alinhada e boa iluminação.')

    def sample(index, radius=.035):
        x, y = points[index, :2].astype(int)
        r = max(2, int(width * radius))
        patch = rgb[max(0, y-r):min(h, y+r+1), max(0, x-r):min(w, x+r+1)]
        return np.median(patch.reshape(-1, 3), axis=0) if patch.size else np.array([160, 110, 80])

    skin = np.median([sample(50), sample(280), sample(6, .025)], axis=0)
    # Segment the subject before sampling hair to avoid using a dark background.
    with mp.solutions.selfie_segmentation.SelfieSegmentation(model_selection=0) as segmenter:
        mask = segmenter.process(rgb).segmentation_mask
    brow_y = int(min(points[70, 1], points[300, 1]))
    left = max(0, int(points[234, 0] - width * .12))
    right = min(w, int(points[454, 0] + width * .12))
    top = max(0, int(points[10, 1] - height * .55))
    hair_region = rgb[top:brow_y, left:right]
    hair_mask = mask[top:brow_y, left:right] > .7
    skin_difference = np.linalg.norm(hair_region.astype(float) - skin, axis=2)
    candidates = hair_region[hair_mask & (skin_difference > 35)]
    hair = np.median(candidates, axis=0) if len(candidates) > 40 else np.array([55, 39, 32])
    # Iris landmarks are available in Face Landmarker (indices 468..477).
    eye = sample(468, .015)
    eye = np.clip(eye * .75, 18, 125)
    profile = {
        'schemaVersion': 1, 'name': name.strip()[:48] or 'Meu avatar',
        'skin': color_hex(skin), 'hair': color_hex(hair), 'eyes': color_hex(eye),
        'outfit': '#7279EC', 'hairStyle': 0, 'glasses': False,
        'faceWidth': float(np.clip(width / height / .76, .83, 1.18)),
        'eyeSpacing': float(np.clip(eyes / width / .68, .85, 1.15)),
        'noseSize': float(np.clip(distance(98, 327) / width / .22, .8, 1.22)),
        'mouthWidth': float(np.clip(distance(61, 291) / width / .40, .8, 1.2)),
        'jawWidth': float(np.clip(distance(172, 397) / width / .70, .84, 1.16)),
        'eyeSize': float(np.clip((distance(33, 133)+distance(362, 263)) / width / .42, .85, 1.12)),
        'lipFullness': float(np.clip(distance(0, 17) / height / .075, .75, 1.25)),
        'cheekVolume': 1.0, 'hairVolume': 1.0,
        'source': 'photo',
    }
    destination = Path(destination)
    destination.mkdir(parents=True, exist_ok=True)
    # Export a small reference only; keep the original photo at its original location.
    thumb = image.copy()
    thumb.thumbnail((480, 480), Image.Resampling.LANCZOS)
    thumb.save(destination / 'reference.png')
    temp = destination / 'avatar.tmp'
    temp.write_text(json.dumps(profile, ensure_ascii=False, indent=2, allow_nan=False), encoding='utf-8')
    temp.replace(destination / 'avatar.json')
    print(json.dumps({'ok': True, 'profile': str(destination / 'avatar.json')}, ensure_ascii=False), flush=True)
    return profile


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--input', required=True)
    parser.add_argument('--output', required=True)
    parser.add_argument('--name', default='Meu avatar')
    args = parser.parse_args()
    try:
        generate(args.input, args.output, args.name)
    except Exception as exc:
        print(str(exc), file=sys.stderr, flush=True)
        sys.exit(1)
