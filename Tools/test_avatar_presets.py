import json,struct,unittest,io
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
def glb(path):
 raw=path.read_bytes();n=struct.unpack_from('<I',raw,12)[0]
 return json.loads(raw[20:20+n]),raw[28+n:]
class PresetsTests(unittest.TestCase):
 def test_catalog_and_platform_copy(self):
  raw=(ROOT/'CorporateUnity/Assets/Resources/AvatarPresets.json').read_bytes();data=json.loads(raw)['presets']
  self.assertEqual(raw,(ROOT/'Web/assets/avatar-presets.json').read_bytes())
  self.assertEqual(len({p['id'] for p in data}),12)
  for category in ('masculino','feminino'):self.assertEqual(sum(p['category']==category for p in data),6)
  for p in data:self.assertIn(p['model'],('CorporateHuman.vrm','CorporateWoman.vrm'))
 def test_bases_and_web_geometry(self):
  for model in ('CorporateHuman','CorporateWoman'):
   original,ob=glb(ROOT/f'CorporateUnity/Assets/StreamingAssets/Avatars/{model}.vrm');web,wb=glb(ROOT/f'Web/assets/{model}.vrm')
   self.assertEqual(original['extensions']['VRMC_vrm'],web['extensions']['VRMC_vrm'])
   self.assertEqual(original['accessors'],web['accessors']);self.assertEqual(original['meshes'],web['meshes'])
   vrm=web['extensions']['VRMC_vrm'];self.assertGreaterEqual(len(vrm['humanoid']['humanBones']),52)
   self.assertEqual(vrm['meta']['name'],'Corporate Woman' if model=='CorporateWoman' else 'Corporate Human')
   for key in ('jawOpen','eyeBlinkLeft','eyeBlinkRight','viseme_aa'):self.assertIn(key,vrm['expressions']['custom'])
   images={v['bufferView'] for v in original.get('images',[])}
   for i,(a,b) in enumerate(zip(original['bufferViews'],web['bufferViews'])):
    oa=a.get('byteOffset',0);wa=b.get('byteOffset',0);payload=wb[wa:wa+b['byteLength']]
    if i in images:self.assertLessEqual(max(Image.open(io.BytesIO(payload)).size),1024)
    else:self.assertEqual(ob[oa:oa+a['byteLength']],payload)
if __name__=='__main__':unittest.main()
