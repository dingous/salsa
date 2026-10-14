"""Safely normalize a user-selected photo for the Unity preview."""
import argparse
from pathlib import Path
import sys
from PIL import Image, ImageOps

parser = argparse.ArgumentParser()
parser.add_argument('--input', required=True)
parser.add_argument('--output', required=True)
args = parser.parse_args()
try:
    path = Path(args.input)
    if path.stat().st_size > 25 * 1024 * 1024:
        raise ValueError('Escolha uma foto de até 25 MB.')
    with Image.open(path) as image:
        if image.format not in ('PNG', 'JPEG'):
            raise ValueError('Use uma foto PNG ou JPEG.')
        if image.width * image.height > 40_000_000:
            raise ValueError('Reduza a foto para até 40 megapixels.')
        preview = ImageOps.exif_transpose(image).convert('RGB')
        preview.thumbnail((640, 640), Image.Resampling.LANCZOS)
        preview.save(args.output)
except Exception as exc:
    print(str(exc), file=sys.stderr, flush=True)
    sys.exit(1)
