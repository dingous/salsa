"""Run with Blender --background --python. Uses official CC0 MakeHuman assets."""
import sys
from pathlib import Path
import zipfile
import json

ROOT = Path(__file__).resolve().parents[1]
FEMALE = '--female' in sys.argv
MODEL = 'CorporateWoman' if FEMALE else 'CorporateHuman'
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
macro.update(gender=0.0 if FEMALE else 1.0, age=.38 if FEMALE else .42, muscle=.35 if FEMALE else .45, weight=.46)
macro['race'] = {'caucasian': .7, 'african': .15, 'asian': .15}
human = HumanService.create_human(macro_detail_dict=macro)
skin = AssetService.find_asset_absolute_path('young_caucasian_female.mhmat' if FEMALE else 'young_caucasian_male.mhmat', asset_subdir='skins')
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
    *[('clothes', name+'.mhclo', 'Clothes') for name in (['female_elegantsuit01','female_casualsuit01','female_casualsuit02','female_sportsuit01','male_casualsuit04','male_casualsuit05','male_casualsuit06'] if FEMALE else ['male_elegantsuit01',*[f'male_casualsuit{i:02}' for i in range(1,7)]])],
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
# Remove only skin covered by every wardrobe choice. Baking the union of
# clothing masks would leave holes in the arms when switching to a T-shirt.
wardrobe_masks = []
shoe_mask = set()
for modifier in human.modifiers:
    if modifier.type != 'MASK' or modifier.vertex_group == 'body':
        continue
    group = human.vertex_groups.get(modifier.vertex_group)
    if not group:
        continue
    hidden = {v.index for v in human.data.vertices if any(g.group == group.index and g.weight > .5 for g in v.groups)}
    if any(n in modifier.vertex_group.lower() for n in ('male_elegantsuit', 'male_casualsuit','female_elegantsuit','female_casualsuit','female_sportsuit')):
        wardrobe_masks.append(hidden)
    elif 'shoes01' in modifier.vertex_group.lower():
        shoe_mask |= hidden
for modifier in list(human.modifiers):
    if modifier.type == 'MASK' and modifier.vertex_group != 'body':
        human.modifiers.remove(modifier)
if len(wardrobe_masks) != 7:
    raise RuntimeError(f'Expected seven garment masks, got {len(wardrobe_masks)}')
hidden = set.intersection(*wardrobe_masks) | shoe_mask
group = human.vertex_groups.new(name='wardrobe_common_hidden')
group.add(sorted(hidden), 1, 'REPLACE')
mask = human.modifiers.new(name='Wardrobe common coverage', type='MASK')
mask.vertex_group = group.name
mask.invert_vertex_group = True
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
    if FEMALE and 'suit' in obj.name.lower():
        # Shared skin coverage must support shirts and skirts. Add garment ease
        # instead of deleting body areas needed by another wardrobe choice.
        offsets=[vertex.normal.copy()*.006 for vertex in obj.data.vertices]
        for vertex,offset in zip(obj.data.vertices,offsets):vertex.co += offset
        if obj.data.shape_keys:
            for key in obj.data.shape_keys.key_blocks:
                for vertex,offset in zip(key.data,offsets):vertex.co += offset
        obj.data.update()
        print('GARMENT_EASE',obj.name,len(offsets),flush=True)
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

out = ROOT / f'Downloads/{MODEL}.glb'
bpy.ops.object.select_all(action='SELECT')
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.gltf(filepath=str(out), export_format='GLB', use_selection=True,
                         export_animations=False, export_morph=True, export_morph_normal=False,
                         export_morph_tangent=False, export_skins=True, export_yup=True)
print('CORPORATE_HUMAN_EXPORTED', out, flush=True)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / f'Downloads/{MODEL}.blend'))
