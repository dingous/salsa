"""Wrap the generated, skinned glTF in VRM 1.0 with explicit humanoid/expression bindings."""
import json
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def convert(source, destination):
    data = source.read_bytes()
    length = struct.unpack_from('<I', data, 12)[0]
    doc = json.loads(data[20:20 + length])
    binary_chunks = data[20 + length:]
    for material in doc.get('materials', []):
        name = material.get('name', '').lower()
        if name.startswith(('skin_', 'eye_', 'outfit_')) or any(n in name for n in ('teeth', 'tongue', 'shoes')):
            material['alphaMode'] = 'OPAQUE'
        else:
            material['alphaMode'] = 'MASK'
            material['alphaCutoff'] = .35
    lookup = {node.get('name', '').split(':')[-1]: i for i, node in enumerate(doc['nodes'])}
    names = {'hips':'Hips', 'spine':'Spine', 'chest':'Spine1', 'upperChest':'Spine2', 'neck':'Neck', 'head':'Head'}
    for side in ('left', 'right'):
        prefix = side.title()
        for bone, suffix in [('Shoulder','Shoulder'), ('UpperArm','Arm'), ('LowerArm','ForeArm'), ('Hand','Hand'), ('UpperLeg','UpLeg'), ('LowerLeg','Leg'), ('Foot','Foot'), ('Toes','ToeBase')]:
            names[side + bone] = prefix + suffix
        for finger in ('Thumb', 'Index', 'Middle', 'Ring', 'Little'):
            source_finger = 'Pinky' if finger == 'Little' else finger
            joints = ('Metacarpal', 'Proximal', 'Distal') if finger == 'Thumb' else ('Proximal', 'Intermediate', 'Distal')
            for i, joint in enumerate(joints, 1):
                names[side + finger + joint] = prefix + 'Hand' + source_finger + str(i)
    missing = [name for name, source_name in names.items() if source_name not in lookup]
    if missing:
        raise RuntimeError(f'Missing humanoid bones: {missing}; exported nodes: {list(lookup)}')
    custom = {}
    for i, node in enumerate(doc['nodes']):
        if 'mesh' not in node:
            continue
        mesh = doc['meshes'][node['mesh']]
        for index, name in enumerate(mesh.get('extras', {}).get('targetNames', [])):
            if name.startswith('identity_'):
                continue
            custom.setdefault(name, {'morphTargetBinds': [], 'isBinary': False})['morphTargetBinds'].append({'node': i, 'index': index, 'weight': 1})
    if not all(key in custom for key in ('jawOpen', 'eyeBlinkLeft', 'mouthSmileRight', 'viseme_aa')):
        raise RuntimeError('Required facial/viseme targets missing')
    doc.setdefault('extensions', {})['VRMC_vrm'] = {
        'specVersion': '1.0',
        'meta': {'name':'Corporate Human', 'version':'1.0', 'authors':['MakeHuman Community', 'Salsa'],
                 'copyrightInformation':'Derived from MakeHuman CC0 core assets and face units.',
                 'licenseUrl':'https://creativecommons.org/publicdomain/zero/1.0/',
                 'avatarPermission':'everyone', 'allowExcessivelyViolentUsage':False,
                 'allowExcessivelySexualUsage':False, 'commercialUsage':'corporation',
                 'allowPoliticalOrReligiousUsage':True, 'allowAntisocialOrHateUsage':False,
                 'creditNotation':'unnecessary', 'allowRedistribution':True, 'modification':'allowModificationRedistribution'},
        'humanoid': {'humanBones': {name: {'node': lookup[source_name]} for name, source_name in names.items()}},
        'expressions': {'preset': {}, 'custom': custom},
        'lookAt': {'offsetFromHeadBone':[0,0,0], 'type':'expression',
                   **{key:{'inputMaxValue':90, 'outputScale':1} for key in ('rangeMapHorizontalInner','rangeMapHorizontalOuter','rangeMapVerticalDown','rangeMapVerticalUp')}},
        'firstPerson': {'meshAnnotations':[{'node':i,'type':'both'} for i,n in enumerate(doc['nodes']) if 'mesh' in n]},
    }
    doc.setdefault('extensionsUsed', []).append('VRMC_vrm')
    payload = json.dumps(doc, separators=(',', ':')).encode('utf-8')
    payload += b' ' * (-len(payload) % 4)
    result = struct.pack('<4sII', b'glTF', 2, 20 + len(payload) + len(binary_chunks)) + struct.pack('<I4s', len(payload), b'JSON') + payload + binary_chunks
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_bytes(result)
    print(f'{destination}: {len(result)} bytes, {len(names)} humanoid bones, {len(custom)} expressions')


if __name__ == '__main__':
    convert(ROOT / 'Downloads/CorporateHuman.glb', ROOT / 'CorporateUnity/Assets/StreamingAssets/Avatars/CorporateHuman.vrm')
