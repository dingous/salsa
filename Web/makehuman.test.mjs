// Model/rig tests in Node. Texture decoding is stubbed; this does not validate browser rendering.
import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
globalThis.self=globalThis;
globalThis.createImageBitmap=async()=>({width:1024,height:1024,close(){}});
const THREE=await import('./vendor/three/package/build/three.module.js');
const {GLTFLoader}=await import('./vendor/three/package/examples/jsm/loaders/GLTFLoader.js');
const {VRMLoaderPlugin}=await import('./vendor/three-vrm/package/lib/three-vrm.module.js');
const {AvatarRig}=await import('./rig.js');
const {HumanAppearance,sanitizeHuman}=await import('./makehuman-appearance.js');
const {runMotionChecks}=await import('./motion-cases.js');
const bytes=await fs.readFile(new URL('assets/CorporateHuman.vrm',import.meta.url));
const loader=new GLTFLoader();loader.register(p=>new VRMLoaderPlugin(p));
const gltf=await loader.parseAsync(bytes.buffer.slice(bytes.byteOffset,bytes.byteOffset+bytes.byteLength),'');
const vrm=gltf.userData.vrm,human=new HumanAppearance(vrm);
assert.equal(human.available,true);
for(const hairStyle of [0,1,2,3]){human.apply({hairStyle,beard:true,glasses:true});for(const [index,name]of ['short02','short04','long01'].entries()){const meshes=human.meshes.filter(m=>m.name.toLowerCase().includes(name));assert.ok(meshes.length);assert.ok(meshes.every(m=>m.visible===(index===hairStyle)));}}
human.apply({style:'stylized',hairStyle:0,beard:true,glasses:true});assert.ok(human.meshes.every(m=>(Array.isArray(m.material)?m.material:[m.material]).every(x=>x.isMeshToonMaterial)));
human.apply({style:'realistic',hairStyle:0});assert.ok(human.meshes.filter(m=>/beard|glasses/.test(m.name.toLowerCase())).every(m=>!m.visible));
assert.equal(vrm.meta.name,'Corporate Human');assert.equal(vrm.expressionManager.expressions.length,67);
const results=runMotionChecks(new AvatarRig(vrm),THREE);
assert.equal(sanitizeHuman({style:'bad',hairStyle:9}).hairStyle,0);
console.log(JSON.stringify({passed:true,model:vrm.meta.name,customExpressions:67,motionChecks:results.length,hairStyles:4,styles:2,accessories:true,textureDecoding:'stubbed; browser render requires verification'}));
human.dispose();
