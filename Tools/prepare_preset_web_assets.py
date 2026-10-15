"""Pack web avatars with 1024px textures; preserve all rigs and expression bindings."""
import hashlib,json,struct,io
from PIL import Image
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def compact(source,destination):
 raw=source.read_bytes();size=struct.unpack_from('<I',raw,12)[0];doc=json.loads(raw[20:20+size]);binary=raw[28+size:]
 packed=bytearray();seen={};images={image['bufferView']:image for image in doc.get('images',[]) if 'bufferView' in image}
 for index,view in enumerate(doc.get('bufferViews',[])):
  start=view.get('byteOffset',0);data=binary[start:start+view['byteLength']];key=hashlib.sha256(data).digest()
  if index in images:
   image=Image.open(io.BytesIO(data))
   if max(image.size)>1024:
    image.thumbnail((1024,1024),Image.Resampling.LANCZOS);output=io.BytesIO();image.save(output,format='PNG' if images[index].get('mimeType')=='image/png' else 'JPEG',optimize=True);data=output.getvalue();view['byteLength']=len(data);key=hashlib.sha256(data).digest()
  if key not in seen:
   packed.extend(b'\0'*(-len(packed)%4));seen[key]=len(packed);packed.extend(data)
  view['byteOffset']=seen[key]
 doc['buffers'][0]['byteLength']=len(packed);packed.extend(b'\0'*(-len(packed)%4))
 payload=json.dumps(doc,separators=(',',':')).encode();payload+=b' '*(-len(payload)%4)
 result=struct.pack('<4sII',b'glTF',2,28+len(payload)+len(packed))+struct.pack('<I4s',len(payload),b'JSON')+payload+struct.pack('<I4s',len(packed),b'BIN\0')+packed
 destination.write_bytes(result);print(destination.name,len(raw),'->',len(result),flush=True)
def main():
 assets=ROOT/'Web/assets';assets.mkdir(exist_ok=True)
 for model in ('CorporateHuman','CorporateWoman'):compact(ROOT/f'CorporateUnity/Assets/StreamingAssets/Avatars/{model}.vrm',assets/f'{model}.vrm')
 (assets/'avatar-presets.json').write_bytes((ROOT/'CorporateUnity/Assets/Resources/AvatarPresets.json').read_bytes())
if __name__=='__main__':main()
