"""Verify HTTPS deployment, code hashes, MIME types and model/WASM availability."""
import hashlib
import json
import time
import urllib.request
from pathlib import Path

BASE='https://dingous.com.br/Games/Avatar/'

def get(path,limit=None):
    request=urllib.request.Request(BASE+path,headers={'Cache-Control':'no-cache'})
    with urllib.request.urlopen(request,timeout=12) as response:
        return response.status,response.headers,response.read() if limit is None else response.read(limit),response.url

started=time.monotonic()
while True:
    try:
        code,headers,body,url=get('deployment.json')
        manifest=json.loads(body)
        assert manifest['release']=='2026-10-14-movement-e2e'
        break
    except Exception as error:
        if time.monotonic()-started>300:raise
        print('Waiting for Avatar deployment:',type(error).__name__,flush=True)
        time.sleep(10)

checks=[]
for name,digest in manifest['files'].items():
    code,headers,body,url=get(name)
    assert code==200
    if name=='index.html':
        assert b'id="avatar"' in body and b'id="mirror"' in body and b'type="importmap"' not in body
    else:assert hashlib.sha256(body).hexdigest()==digest,name+' hash mismatch'
    if name.endswith('.js'):assert 'javascript' in headers['Content-Type'],name
    checks.append(name)
for name,mime in [('vendor/mediapipe/package/vision_bundle.mjs','javascript'),('vendor/mediapipe/package/wasm/vision_wasm_internal.wasm','application/wasm'),('assets/face.task','application/octet-stream'),('assets/hand.task','application/octet-stream'),('assets/pose.task','application/octet-stream'),('assets/Seed-san.vrm','application/octet-stream')]:
    code,headers,body,url=get(name,16)
    assert code==200 and mime in headers['Content-Type'],(name,headers['Content-Type'])
    if name.endswith('.wasm'):assert body[:4]==b'\0asm'
    if name.endswith('.vrm'):assert body[:4]==b'glTF'
    checks.append(name)
code,headers,body,url=get('')
assert "camera=(self)" in headers.get('Permissions-Policy','')
assert "wasm-unsafe-eval" in headers.get('Content-Security-Policy','')
with urllib.request.urlopen(BASE.rstrip('/'),timeout=12) as response:assert response.url.endswith('/Games/Avatar/')
result={'passed':True,'url':BASE,'release':manifest['release'],'checks':checks,'https':True,'redirect':True,'cameraPolicy':True,'wasmPolicy':True}
out=Path(__file__).resolve().parents[1]/'Validation/public-avatar-e2e.json'
out.write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result),flush=True)
