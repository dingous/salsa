"""Run with Blender --background --python. Uses official CC0 MakeHuman assets."""
import sys
from pathlib import Path
import zipfile
import json

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / 'Downloads/mpfb-user/data'
DATA.mkdir(parents=True, exist_ok=True)
for pack in ('makehuman_system_assets', 'faceunits01', 'visemes02', 'bodyparts05', 'glasses01'):
    with zipfile.ZipFile(ROOT / f'Downloads/{pack}.zip') as archive:
        archive.extractall(DATA)

import bpy
sys.path.insert(0, str(ROOT / 'Downloads/mpfb2/src'))
# This is an isolated headless build, not an installation into the user's preferences.
original_extension_path = bpy.utils.extension_path_user
bpy.utils.extension_path_user = lambda package, **kw: str(ROOT / 'Downloads/mpfb-user') if package == 'mpfb' else original_extension_path(package, **kw)
import mpfb
bpy.context.preferences.addons.new().module = 'mpfb'
mpfb.register()
from mpfb.services.humanservice import HumanService
from mpfb.services.targetservice import TargetService
from mpfb.services.assetservice import AssetService
from mpfb.services.faceservice import FaceService
from mpfb.services.exportservice import ExportService
from mpfb.services.objectservice import ObjectService

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
macro = TargetService.get_default_macro_info_dict()
macro.update(gender=1.0, age=.42, muscle=.45, weight=.46)
macro['race'] = {'caucasian': .7, 'african': .15, 'asian': .15}
human = HumanService.create_human(macro_detail_dict=macro)
skin = AssetService.find_asset_absolute_path('young_caucasian_male.mhmat', asset_subdir='skins')
if not skin:
    raise RuntimeError('Required official skin missing')
HumanService.set_character_skin(skin, human, skin_type='GAMEENGINE')
HumanService.add_builtin_rig(human, 'mixamo_unity')
for subdir, name, kind in [
    ('eyes', 'low-poly.mhclo', 'Eyes'),
    ('eyebrows', 'eyebrow001.mhclo', 'Eyebrows'),
    ('eyelashes', 'eyelashes01.mhclo', 'Eyelashes'),
    ('tongue', 'tongue01.mhclo', 'Tongue'),
    ('teeth', 'teeth_base.mhclo', 'Teeth'),
    ('hair', 'short02.mhclo', 'Hair'),
    ('hair', 'short04.mhclo', 'Hair'),
    ('hair', 'long01.mhclo', 'Hair'),
    ('clothes', 'male_elegantsuit01.mhclo', 'Clothes'),
    ('clothes', 'shoes01.mhclo', 'Clothes'),
    ('clothes', 'wdg_scruffy_beard.mhclo', 'Clothes'),
    ('clothes', 'spamrakuen_tbm_glasses_frames_01.mhclo', 'Clothes'),
]:
    path = AssetService.find_asset_absolute_path(name, asset_subdir=subdir)
    if not path:
        raise RuntimeError(f'Required official asset missing: {name}')
    HumanService.add_mhclo_asset(path, human, asset_type=kind, material_type='GAMEENGINE')
TargetService.bake_targets(human)
FaceService.load_targets(human, load_microsoft_visemes=False, load_meta_visemes=True, load_arkit_faceunits=True)
FaceService.interpolate_targets(human)
# Optional accessories must not cut permanent holes in the head when switched off.
for modifier in list(human.modifiers):
    if modifier.type == 'MASK' and modifier.vertex_group != 'body' and not any(n in modifier.vertex_group.lower() for n in ('male_elegantsuit', 'shoes01')):
        human.modifiers.remove(modifier)
ExportService.bake_modifiers_remove_helpers(human, bake_masks=True, bake_subdiv=False)
rig = human.parent
if rig is None or rig.type != 'ARMATURE':
    raise RuntimeError('Missing human rig')

# Store material roles for runtime palette customization; textures remain embedded.
for obj in bpy.data.objects:
    if obj.type != 'MESH':
        continue
    for material in obj.data.materials:
        if not material:
            continue
        role = 'skin' if obj == human else 'hair' if any(n in obj.name.lower() for n in ('short02', 'short04', 'long01', 'eyebrow', 'beard')) else 'eye' if 'low-poly' in obj.name.lower() else 'outfit' if 'suit' in obj.name.lower() else 'detail'
        material.name = role + '_' + material.name
        if material.use_nodes:
            for node in material.node_tree.nodes:
                if node.type == 'BSDF_PRINCIPLED':
                    node.inputs['Roughness'].default_value = .68
                    node.inputs['Metallic'].default_value = 0

# Conservative identity morphs shared by the face and fitted accessories.
# All expressions are additive to these shapes; no runtime remeshing is necessary.
for obj in bpy.data.objects:
    if obj.type != 'MESH':
        continue
    if not obj.data.shape_keys:
        obj.shape_key_add(name='Basis')
    basis = obj.data.shape_keys.key_blocks[0]
    head_z = rig.data.bones['mixamorig:Head'].head_local.z
    for name in ('identity_faceWidth', 'identity_jawWidth', 'identity_stylized'):
        shape = obj.shape_key_add(name=name)
        for src, dst in zip(basis.data, shape.data):
            co = src.co.copy()
            weight = max(0, min(1, (co.z - head_z + .10) / .12))
            if name == 'identity_jawWidth':
                weight *= max(0, 1 - abs(co.z - head_z) / .13)
            co.x *= 1 + weight * (.12 if name != 'identity_stylized' else .08)
            if name == 'identity_stylized':
                co.z = head_z + (co.z - head_z) * (1 + weight * .09)
            dst.co = co

out = ROOT / 'Downloads/CorporateHuman.glb'
bpy.ops.object.select_all(action='SELECT')
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.gltf(filepath=str(out), export_format='GLB', use_selection=True,
                         export_animations=False, export_morph=True, export_morph_normal=False,
                         export_morph_tangent=False, export_skins=True, export_yup=True)
print('CORPORATE_HUMAN_EXPORTED', out, flush=True)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / 'Downloads/CorporateHuman.blend'))
