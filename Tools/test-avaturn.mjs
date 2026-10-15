import assert from 'node:assert/strict';
import fs from 'node:fs';
import {avaturnBones,avaturnProject,convertAvaturnGlb} from '../Web/avaturn-glb.js';
function pack(doc,tail=Buffer.from([4,0,0,0,66,73,78,0,1,2,3,4])){const json=Buffer.from(JSON.stringify(doc));const pad=(json.length+3)&~3;const out=Buffer.alloc(20+pad+tail.length,32);out.write('glTF');out.writeUInt32LE(2,4);out.writeUInt32LE(out.length,8);out.writeUInt32LE(pad,12);out.write('JSON',16);json.copy(out,20);tail.copy(out,20+pad);return out;}
function unpack(buffer){const b=Buffer.from(buffer);return {doc:JSON.parse(b.subarray(20,20+b.readUInt32LE(12))),tail:b.subarray(20+b.readUInt32LE(12))};}
const names=Object.values(avaturnBones()),doc={asset:{version:'2.0',copyright:'Original owner'},nodes:names.map(name=>({name:'mixamorig:'+name})),meshes:[{extras:{targetNames:['JawOpen','EyeBlinkLeft','EyeBlinkRight']}}],extensions:{VENDOR:{preserved:true}}};doc.nodes.push({mesh:0});
const input=pack(doc),result=convertAvaturnGlb(input),converted=unpack(result.buffer);
assert.equal(Object.keys(converted.doc.extensions.VRMC_vrm.humanoid.humanBones).length,52);assert.equal(result.expressions,3);
assert.equal(converted.doc.extensions.VRMC_vrm.expressions.custom.jawOpen.morphTargetBinds[0].node,52);
assert.deepEqual(converted.doc.nodes,doc.nodes);assert.deepEqual(converted.doc.meshes,doc.meshes);assert.deepEqual(converted.doc.asset,doc.asset);assert.deepEqual(converted.doc.extensions.VENDOR,doc.extensions.VENDOR);assert.deepEqual(converted.tail,unpack(input).tail);
assert.equal(converted.doc.extensions.VRMC_vrm.meta.allowRedistribution,false);
assert.throws(()=>convertAvaturnGlb(new Uint8Array(30)),/GLB/);
assert.throws(()=>convertAvaturnGlb(pack({...doc,nodes:[]})),/Osso/);
assert.throws(()=>convertAvaturnGlb(pack({...doc,images:[{uri:'https://example.com/a.png'}]})),/incorporados/);
assert.throws(()=>avaturnProject('https://demo.avaturn.dev.evil.test'),/HTTPS/);
assert.throws(()=>avaturnProject('http://demo.avaturn.dev'),/HTTPS/);
assert.equal(avaturnProject('https://demo.avaturn.dev'),'https://demo.avaturn.dev/');
if(fs.existsSync('Downloads/Avaturn-BaseModel.glb')){const real=convertAvaturnGlb(fs.readFileSync('Downloads/Avaturn-BaseModel.glb'));assert.equal(Object.keys(unpack(real.buffer).doc.extensions.VRMC_vrm.humanoid.humanBones).length,52);assert.equal(real.expressions,0);fs.writeFileSync('Validation/avaturn-base-converted.vrm',Buffer.from(real.buffer));}
console.log('AVATURN_IMPORT_TESTS=PASS');
