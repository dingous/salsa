import assert from 'node:assert/strict';
import * as THREE from './vendor/three/package/build/three.module.js';
import {AvatarRig} from './rig.js';

const scene=new THREE.Scene(),shoulder=new THREE.Bone(),upper=new THREE.Bone(),helper=new THREE.Bone(),lower=new THREE.Bone(),hand=new THREE.Bone();
scene.add(shoulder);shoulder.add(upper);upper.add(helper,lower);lower.add(hand);
upper.position.set(.2,1,0);helper.position.set(0,.01,0);lower.position.set(.4,0,0);hand.position.set(.3,0,0);
const bones={leftShoulder:shoulder,leftUpperArm:upper,leftLowerArm:lower,leftHand:hand};
const vrm={scene,humanoid:{humanBones:bones,getNormalizedBoneNode:k=>bones[k]},update:()=>{}};
const rig=new AvatarRig(vrm);rig.setMirror(false);const positions=Object.values(bones).map(b=>b.position.clone());
assert.ok(rig.directions.get('leftUpperArm').angleTo(new THREE.Vector3(1,0,0))<1e-6,'Use elbow, not a helper child, as the arm direction');
for(let i=0;i<90;i++)rig.update(null,1/30);
const before=upper.quaternion.clone(),points=Array.from({length:33},()=>({x:0,y:0,z:0,confidence:0}));
points[11]={x:0,y:0,z:0,confidence:1};points[13]={x:-1,y:0,z:0,confidence:1};
rig.update({pose:true,points},1/30);
assert.ok(before.angleTo(upper.quaternion)<=5.5/30+1e-6,'An abrupt observation must respect angular speed');
const tracked=rig.filtered.get('leftUpperArm').clone();
rig.update(null,1/30);
assert.ok(tracked.angleTo(rig.filtered.get('leftUpperArm'))<1e-6,'Brief tracking loss must hold the last arm direction');
for(let i=0;i<180;i++)rig.update(null,1/30);
assert.ok(rig.filtered.get('leftUpperArm').y<-.95,'Long tracking loss must ease into a relaxed pose');
assert.ok(rig.filtered.get('leftLowerArm').z>.9,'Out-of-frame wrist must relax into a forward desk pose');
for(const mirror of [false,true]){
  rig.setMirror(mirror);
  const source=mirror?12:11,elbow=mirror?14:13,wrist=mirror?16:15;
  const seated=Array.from({length:33},()=>({x:0,y:0,z:0,confidence:0}));
  seated[source]={x:0,y:0,z:0,confidence:1};
  seated[elbow]={x:mirror?-.2:.2,y:.4,z:0,confidence:1};
  for(let i=0;i<180;i++)rig.update({pose:true,points:seated},1/30);
  const u=rig.filtered.get('leftUpperArm'),l=rig.filtered.get('leftLowerArm');
  assert.ok(l.z>.85,'Missing wrist remains bent forward with a visible elbow in either mirror mode');
  assert.ok(u.angleTo(l)<2.62,'Seated fallback must respect the elbow limit');
  for(let i=0;i<120;i++){
    const next=seated.map(p=>({...p}));
    next[wrist]={x:-next[elbow].x,y:-next[elbow].y,z:.01,confidence:1};
    rig.update({pose:true,points:next},1/30);
    assert.ok(rig.filtered.get('leftUpperArm').angleTo(rig.filtered.get('leftLowerArm'))<=2.62+1e-6,'Filtered reacquisition must not overfold the elbow');
  }
}
Object.values(bones).forEach((bone,i)=>assert.ok(bone.position.equals(positions[i]),'Shoulder and limb attachments must not translate'));
console.log('Arm attachment, anatomical axis, motion speed, seated wrist loss, both mirrors and elbow reacquisition checks passed');
