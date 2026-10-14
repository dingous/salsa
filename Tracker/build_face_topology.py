"""Generate original facial topology with eye and mouth openings for the Unity rig."""
import json
from pathlib import Path
import numpy as np
from scipy.spatial import Delaunay


def inside_shape(x, y):
    width = np.sqrt(max(0, 1 - y * y)) * (1 - .13 * max(0, -y))
    return abs(x) < width


def make():
    holes = [(-.43, .20, .265, .125), (.43, .20, .265, .125), (0, -.43, .285, .022)]
    positions = []
    for y in np.linspace(-.98, .98, 72):
        for x in np.linspace(-.98, .98, 66):
            if inside_shape(x, y) and all(((x-cx)/rx)**2 + ((y-cy)/ry)**2 > 1.12 for cx, cy, rx, ry in holes):
                positions.append((float(x), float(y)))
    contour = []
    for a in np.linspace(0, 2*np.pi, 128, endpoint=False):
        y = np.sin(a)
        x = np.cos(a) * (1 - .13 * max(0, -y))
        contour.append(len(positions))
        positions.append((float(x), float(y)))
    for cx, cy, rx, ry in holes:
        for expansion in (1, 1.13, 1.32, 1.6):
            for a in np.linspace(0, 2*np.pi, 64, endpoint=False):
                positions.append((float(cx + rx * np.cos(a) * expansion), float(cy + ry * np.sin(a) * expansion)))
    unique = list(dict.fromkeys((round(x, 7), round(y, 7)) for x, y in positions))
    remap = {pair:i for i, pair in enumerate(unique)}
    contour = [remap[(round(positions[i][0], 7), round(positions[i][1], 7))] for i in contour]
    array = np.array(unique)
    triangles = []
    for tri in Delaunay(array).simplices:
        center = array[tri].mean(axis=0)
        if not inside_shape(*center):
            continue
        if any(((center[0]-cx)/rx)**2 + ((center[1]-cy)/ry)**2 < .99 for cx, cy, rx, ry in holes):
            continue
        # Source points are in XY. Reverse winding to face the camera along -Z.
        a, b, c = (int(i) for i in tri)
        triangles.extend([a, c, b])
    names = ['browDownLeft','browDownRight','browInnerUp','browOuterUpLeft','browOuterUpRight',
             'cheekPuff','cheekSquintLeft','cheekSquintRight','eyeBlinkLeft','eyeBlinkRight',
             'eyeLookDownLeft','eyeLookDownRight','eyeLookInLeft','eyeLookInRight','eyeLookOutLeft','eyeLookOutRight',
             'eyeLookUpLeft','eyeLookUpRight','eyeSquintLeft','eyeSquintRight','eyeWideLeft','eyeWideRight',
             'jawForward','jawLeft','jawOpen','jawRight','mouthClose','mouthDimpleLeft','mouthDimpleRight',
             'mouthFrownLeft','mouthFrownRight','mouthFunnel','mouthLeft','mouthLowerDownLeft','mouthLowerDownRight',
             'mouthPressLeft','mouthPressRight','mouthPucker','mouthRight','mouthRollLower','mouthRollUpper',
             'mouthShrugLower','mouthShrugUpper','mouthSmileLeft','mouthSmileRight','mouthStretchLeft','mouthStretchRight',
             'mouthUpperUpLeft','mouthUpperUpRight','noseSneerLeft','noseSneerRight','tongueOut']
    payload = dict(points=[dict(x=x, y=y) for x,y in unique], triangles=triangles, contour=contour, shapes=names)
    target = Path(__file__).resolve().parent.parent / 'Assets/WebcamAvatar/Resources/AdvancedFaceTopology.json'
    target.write_text(json.dumps(payload, separators=(',', ':')), encoding='utf-8')
    print('Original topology:', len(unique), 'vertices,', len(triangles)//3, 'triangles,', len(names), 'shapes')


if __name__ == '__main__':
    make()
