import fs from 'node:fs';
import assert from 'node:assert/strict';
import {prepareMetaHumanVrm} from '../Web/metahuman-vrm.js';
function unpack(input){const b=Buffer.from(input);return {doc:JSON.parse(b.subarray(20,20+b.readUInt32LE(12))),tail:b.subarray(20+b.readUInt32LE(12))};}
function pack(doc,tail){const json=Buffer.from(JSON.stringify(doc)),pad=(json.length+3)&~3,b=Buffer.alloc(20+pad+tail.length,32);b.write('glTF');b.writeUInt32LE(2,4);b.writeUInt32LE(b.length,8);b.writeUInt32LE(pad,12);b.write('JSON',16);json.copy(b,20);tail.copy(b,20+pad);return b;}
// Existing MakeHuman is solely a compatibility fixture, not a MetaHuman asset.
const source=fs.readFileSync('Web/assets/CorporateHuman.vrm'),original=unpack(source);
const result=prepareMetaHumanVrm(source),converted=unpack(result.buffer);
assert.equal(result.facialBindings,true);assert.equal(converted.doc.asset.extras.dingousFramework,'metahuman');
assert.deepEqual(converted.doc.extensions,original.doc.extensions);assert.deepEqual(converted.doc.nodes,original.doc.nodes);assert.deepEqual(converted.doc.meshes,original.doc.meshes);assert.deepEqual(converted.tail,original.tail);
assert.equal(converted.doc.extensions.VRMC_vrm.meta.name,'Corporate Human'); // No brand/model-name substitution.
const missing=structuredClone(original.doc);delete missing.extensions.VRMC_vrm.humanoid.humanBones.leftIndexDistal;
assert.throws(()=>prepareMetaHumanVrm(pack(missing,original.tail)),/leftIndexDistal/);
const ambiguous=structuredClone(original.doc);ambiguous.extensions.VRMC_vrm.humanoid.humanBones.leftIndexDistal=ambiguous.extensions.VRMC_vrm.humanoid.humanBones.leftIndexProximal;
assert.throws(()=>prepareMetaHumanVrm(pack(ambiguous,original.tail)),/ambíguo/);
const raw=structuredClone(original.doc);delete raw.extensions.VRMC_vrm;assert.throws(()=>prepareMetaHumanVrm(pack(raw,original.tail)),/VRM 1.0/);
const external=structuredClone(original.doc);external.images=[{uri:'https://example.com/private.png'}];assert.throws(()=>prepareMetaHumanVrm(pack(external,original.tail)),/texturas/);
const staticFace=structuredClone(original.doc);staticFace.extensions.VRMC_vrm.expressions={};assert.equal(prepareMetaHumanVrm(pack(staticFace,original.tail)).facialBindings,false);
assert.throws(()=>prepareMetaHumanVrm(Buffer.from('Unreal DNA')),/VRM/);
console.log('METAHUMAN_PREPARED_VRM_TESTS=PASS (MakeHuman fixture; actual MetaHuman asset not tested)');
