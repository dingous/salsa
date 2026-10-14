import assert from 'node:assert/strict';
import * as THREE from './vendor/three/package/build/three.module.js';
import {solveArm} from './arm-ik.js';
import {AvatarRig} from './rig.js';
import {PoseFilter} from './pose-filter.js';
let checks=0;const check=(ok)=>{assert.ok(ok);checks++;};
for(const a of [.26,.32])for(const b of [.23,.28])for(const target of [new THREE.Vector3(.1,.4,-.1),new THREE.Vector3(-.2,.3,.2),new THREE.Vector3(0,-.4,.2),new THREE.Vector3(0,0,0)]){
 const result=solveArm(target,new THREE.Vector3(.3,.1,.1),a,b);
 check(result.upper.clone().multiplyScalar(a).addScaledVector(result.lower,b).distanceTo(result.target)<1e-6);
 check(result.upper.angleTo(result.lower)<=150*Math.PI/180+1e-6);
}
for(const lengths of [[.3,.28],[.25,.24]])for(const mirror of [false,true]){
 const scene=new THREE.Group(),bones=new Map();
 const bone=(name,parent,pos)=>{const b=new THREE.Bone();b.position.set(...pos);parent.add(b);bones.set(name,b);return b;};
 const hips=bone('hips',scene,[0,1,0]),spine=bone('spine',hips,[0,.2,0]),chest=bone('chest',spine,[0,.2,0]);bone('head',chest,[0,.4,0]);
 for(const [prefix,sign] of [['left',1],['right',-1]]){const upper=bone(prefix+'UpperArm',chest,[sign*.2,.05,0]),lower=bone(prefix+'LowerArm',upper,[sign*lengths[0],0,0]);bone(prefix+'Hand',lower,[sign*lengths[1],0,0]);}
 scene.updateMatrixWorld(true);const vrm={scene,humanoid:{humanBones:Object.fromEntries([...bones].map(([name])=>[name,{}])),getNormalizedBoneNode:key=>bones.get(key)},update:()=>{}};
 const attachments=[...bones].map(([key,bone])=>[key,bone.position.clone()]);
 const rig=new AvatarRig(vrm);rig.setMirror(mirror);const points=Array.from({length:33},()=>({x:0,y:0,z:0,confidence:0}));
 points[0]={x:0,y:-.8,z:0,confidence:1};points[11]={x:.2,y:-.5,z:0,confidence:1};points[12]={x:-.2,y:-.5,z:0,confidence:1};
 points[15]={x:.05,y:-.8,z:0,confidence:1};points[16]={x:-.05,y:-.8,z:0,confidence:1};
 for(let i=0;i<120;i++)rig.update({pose:true,face:false,points,shapes:[]},1/60,false,i/60);
 for(const prefix of ['left','right']){check(bones.get(prefix+'Hand').getWorldPosition(new THREE.Vector3()).distanceTo(bones.get('head').getWorldPosition(new THREE.Vector3()))<.3);check(bones.get(prefix+'Hand').getWorldPosition(new THREE.Vector3()).distanceTo(rig.wristTargets.get(prefix))<.04);}
 check(attachments.every(([key,position])=>bones.get(key).position.distanceTo(position)<1e-7));
 check([...bones].every(([,b])=>[b.quaternion.x,b.quaternion.y,b.quaternion.z,b.quaternion.w].every(Number.isFinite)));
}
const filter=new PoseFilter();filter.update([{x:.5,y:0,z:0,confidence:1}],1/30);let held=filter.update([{x:9,y:0,z:0,confidence:0}],.05);check(held[0].x===.5&&held[0].confidence>=.55);held=filter.update([{x:9,y:0,z:0,confidence:0}],.15);check(held[0].confidence<.55);
const resting=new PoseFilter();let rawEnergy=0,filteredEnergy=0;
for(let i=0;i<120;i++){const noise=.005*Math.sin(i*2*Math.PI/3);const p=resting.update([{x:.5+noise,y:0,z:0,confidence:1}],1/30);if(i>30){rawEnergy+=noise*noise;filteredEnergy+=(p[0].x-.5)**2;}}
check(filteredEnergy<rawEnergy*.25);
for(let i=0;i<15;i++)resting.update([{x:.7,y:0,z:0,confidence:1}],1/30);
check(Math.abs(resting.points[0].x-.7)<.02);
console.log(`${checks} rig geometry and continuity checks passed`);
