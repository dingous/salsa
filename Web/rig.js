import * as THREE from './vendor/three/package/build/three.module.js';
import {PoseFilter} from './pose-filter.js';
import {solveArm} from './arm-ik.js';

export class AvatarRig {
  constructor(vrm) {
    this.vrm=vrm;this.bones=new Map();this.rest=new Map();this.world=new Map();this.directions=new Map();this.palms=new Map();this.origin=vrm.scene.position.clone();this.neutral=new THREE.Quaternion();this.headRaw=new THREE.Quaternion();this.center=null;
    this.wristTargets=new Map();this.ends=new Map();this.elbowPoles=new Map();this.heldHands=new Map();this.handSeen=new Map();
    this.filtered=new Map();this.lastSeen=new Map();this.clock=0;this.dt=1/30;
    for(const expression of vrm.expressionManager?.expressions||[]){expression.overrideBlink="none";expression.isBinary=false;}
    this.mirror=true;
    this.poseFilter=new PoseFilter();this.poseFrame=null;this.poseElapsed=0;
    this.handFilters={left:new PoseFilter(1.5,3),right:new PoseFilter(1.5,3)};this.handFrames={};this.handElapsed={left:0,right:0};
    vrm.scene.updateMatrixWorld(true);
    for(const key of Object.keys(vrm.humanoid.humanBones)) {
      const bone=vrm.humanoid.getNormalizedBoneNode(key);if(!bone)continue;
      this.bones.set(key,bone);this.rest.set(key,bone.quaternion.clone());this.world.set(key,bone.getWorldQuaternion(new THREE.Quaternion()));
      const position=bone.getWorldPosition(new THREE.Vector3());
      const next={UpperArm:'LowerArm',LowerArm:'Hand',UpperLeg:'LowerLeg',LowerLeg:'Foot',Foot:'Toes',Shoulder:'UpperArm'};
      const match=key.match(/^(left|right)(UpperArm|LowerArm|UpperLeg|LowerLeg|Foot|Shoulder)$/);
      let child=match?vrm.humanoid.getNormalizedBoneNode(match[1]+next[match[2]]):bone.children.find(c=>c.isBone)||bone.children[0];
      if(match&&child)this.ends.set(key,child);
      this.directions.set(key,child?child.getWorldPosition(new THREE.Vector3()).sub(position):position.sub(bone.parent.getWorldPosition(new THREE.Vector3())));
    }
    for(const side of ['left','right']) {
      const keys=['Hand','IndexProximal','MiddleProximal','LittleProximal'].map(k=>side+k);
      if(keys.every(k=>this.bones.has(k))) {
        const p=keys.map(k=>this.bones.get(k).getWorldPosition(new THREE.Vector3()));this.palms.set(side+'Hand',this.basis(p[2].clone().sub(p[0]),p[3].clone().sub(p[1]),side));
      }
    }
  }
  point(p){return new THREE.Vector3(this.mirror?-p.x:p.x,-p.y,-p.z);}
  setMirror(value){this.mirror=!!value;this.filtered.clear();this.lastSeen.clear();this.elbowPoles.clear();this.wristTargets.clear();this.heldHands.clear();this.handSeen.clear();this.neutral.identity();this.center=null;this.poseFilter.reset();this.poseFrame=null;for(const side of ['left','right']){this.handFilters[side].reset();this.handFrames[side]=null;this.handElapsed[side]=0;}}
  valid(p,...indices){return p&&indices.every(i=>p[i]&&p[i].confidence>=.55&&[p[i].x,p[i].y,p[i].z].every(Number.isFinite));}
  basis(up,across,side) {
    if(up.lengthSq()<1e-8 || across.lengthSq()<1e-8)return new THREE.Quaternion();
    up.normalize();const normal=across.clone().cross(up).normalize();if(side==='right')normal.negate();
    const x=up.clone().cross(normal).normalize();return new THREE.Quaternion().setFromRotationMatrix(new THREE.Matrix4().makeBasis(x,up,normal));
  }
  rotate(key,target,t,world=false) {
    const bone=this.bones.get(key);if(!bone)return;
    if(world){const parent=bone.parent.getWorldQuaternion(new THREE.Quaternion()).invert();target=parent.multiply(target);}
    // Bound angular velocity so dropped frames cannot snap a joint across the body.
    const angle=bone.quaternion.angleTo(target),speed=key.includes('LowerArm')?Math.PI*4:key.includes('UpperArm')?Math.PI*2:key==='head'?3.5:Math.PI*8/3;
    bone.quaternion.slerp(target,angle>1e-6?Math.min(t,speed*this.dt/angle):t);bone.updateWorldMatrix(false,true);
  }
  aim(key,direction,t) {
    const end=this.ends.get(key),bone=this.bones.get(key);
    if(end&&bone&&direction.lengthSq()>1e-8){
      const actual=end.getWorldPosition(new THREE.Vector3()).sub(bone.getWorldPosition(new THREE.Vector3())).normalize();
      const rotation=new THREE.Quaternion().setFromUnitVectors(actual,direction.clone().normalize()).multiply(bone.getWorldQuaternion(new THREE.Quaternion()));
      this.rotate(key,rotation,1,true);return;
    }
    const rest=this.directions.get(key);if(!rest||rest.lengthSq()<1e-8||direction.lengthSq()<1e-8)return;
    const q=new THREE.Quaternion().setFromUnitVectors(rest.clone().normalize(),direction.clone().normalize()).multiply(this.world.get(key));this.rotate(key,q,t,true);
  }
  aimFinger(key,direction,wrist,t){
    const rest=this.directions.get(key),hand=this.bones.get(wrist);
    if(!rest||!hand||direction.lengthSq()<1e-8||rest.lengthSq()<1e-8)return;
    // Carry the finger's rest twist with the palm. A world-space rest rotation
    // discards wrist roll and makes fingers twist when the palm turns.
    const palmDelta=hand.getWorldQuaternion(new THREE.Quaternion()).multiply(this.world.get(wrist).clone().invert());
    const axis=rest.clone().applyQuaternion(palmDelta).normalize();
    const target=new THREE.Quaternion().setFromUnitVectors(axis,direction.clone().normalize()).multiply(palmDelta).multiply(this.world.get(key));
    this.rotate(key,target,t,true);
  }
  segment(key,points,a,b,t,fallback,upperKey=null){
    const tracked=this.valid(points,a,b),previous=this.filtered.get(key);
    let direction=tracked?this.point(points[b]).sub(this.point(points[a])):fallback.clone();
    if(tracked&&direction.lengthSq()>1e-8)this.lastSeen.set(key,this.clock);
    else if(previous&&this.clock-(this.lastSeen.get(key)??-Infinity)<.2)direction=previous.clone();
    if(direction.lengthSq()<1e-8)return;
    direction.normalize();
    const filtered=previous||direction.clone();
    filtered.lerp(direction,1-Math.exp(-(tracked?18:4)*this.dt)).normalize();
    // Apply the elbow limit after smoothing too: independently filtered joints
    // can otherwise fold through one another during reacquisition.
    const upper=upperKey&&this.filtered.get(upperKey);
    if(upper&&upper.angleTo(filtered)>2.62){
      let axis=upper.clone().cross(filtered);
      if(axis.lengthSq()<1e-8)axis=upper.clone().cross(new THREE.Vector3(0,0,1));
      if(axis.lengthSq()<1e-8)axis.set(1,0,0);
      filtered.copy(upper).applyAxisAngle(axis.normalize(),2.62);
    }
    this.filtered.set(key,filtered);this.aim(key,filtered,t);
  }
  seatedArm(prefix,p,s,e,w){
    const upper=this.bones.get(prefix+'UpperArm'),lower=this.bones.get(prefix+'LowerArm'),wrist=this.bones.get(prefix+'Hand'),other=this.bones.get((prefix==='left'?'right':'left')+'UpperArm');
    if(!this.valid(p,11,12,s,w)||!upper||!lower||!wrist||!other)return false;
    const position=bone=>bone.getWorldPosition(new THREE.Vector3()),shoulder=position(upper);
    const observed=this.point(p[11]).distanceTo(this.point(p[12]));
    const a=shoulder.distanceTo(position(lower)),b=position(lower).distanceTo(position(wrist));if(observed<.12||a<.01||b<.01)return false;
    const scale=THREE.MathUtils.clamp(shoulder.distanceTo(position(other))/observed,.4,3);
    let pole=this.valid(p,s,e)?this.point(p[e]).sub(this.point(p[s])):(this.filtered.get(prefix+'UpperArm')?.clone()||new THREE.Vector3(prefix==='left'?.4:-.4,.2,.15));
    pole.normalize();const old=this.elbowPoles.get(prefix);
    if(old){const delta=new THREE.Quaternion().setFromUnitVectors(old,pole);pole=old.clone().applyQuaternion(new THREE.Quaternion().slerp(delta,1-Math.exp(-10*this.dt)));}this.elbowPoles.set(prefix,pole.clone());
    let target=this.point(p[w]).sub(this.point(p[s])).multiplyScalar(scale);
    if(this.valid(p,0)&&this.bones.has('head')){
      const distance=this.point(p[w]).distanceTo(this.point(p[0]));
      const headTarget=position(this.bones.get('head')).sub(shoulder).add(this.point(p[w]).sub(this.point(p[0])).multiplyScalar(scale));
      target.lerp(headTarget,1-THREE.MathUtils.smoothstep(distance,.23,.30));
    }
    const minimum=Math.sqrt(a*a+b*b+2*a*b*Math.cos(150*Math.PI/180));if(target.length()<minimum)target=pole.clone().multiplyScalar(minimum);
    const solved=solveArm(target,pole,a,b),goal=shoulder.clone().add(solved.target);this.wristTargets.set(prefix,goal.clone());
    this.filtered.set(prefix+'UpperArm',solved.upper);this.lastSeen.set(prefix+'UpperArm',this.clock);this.aim(prefix+'UpperArm',solved.upper,1);
    const actualUpper=position(lower).sub(shoulder).normalize();let actualLower=goal.sub(position(lower)).normalize();
    if(actualUpper.angleTo(actualLower)>150*Math.PI/180){const q=new THREE.Quaternion().setFromUnitVectors(actualUpper,actualLower);actualLower=actualUpper.clone().applyQuaternion(new THREE.Quaternion().slerp(q,(150*Math.PI/180)/actualUpper.angleTo(actualLower)));}
    this.filtered.set(prefix+'LowerArm',actualLower);this.lastSeen.set(prefix+'LowerArm',this.clock);this.aim(prefix+'LowerArm',actualLower,1);return true;
  }
  rememberHand(key){const bone=this.bones.get(key);if(bone){this.heldHands.set(key,bone.quaternion.clone());this.handSeen.set(key,this.clock);}}
  releaseHand(key){if(!this.rest.has(key))return;if(this.clock-(this.handSeen.get(key)??-Infinity)<.18)this.rotate(key,this.heldHands.get(key),1);else this.rotate(key,this.rest.get(key),1-Math.exp(-4*this.dt));}
  calibrate(frame){this.neutral.copy(this.headRaw);const p=frame?.imagePose;if(this.valid(p,23,24))this.center={x:(p[23].x+p[24].x)/2,y:(p[23].y+p[24].y)/2};}
  bend(points,a,b,c,max){
    if(!this.valid(points,a,b,c))return points;
    const upper=this.point(points[b]).sub(this.point(points[a])).normalize(),lower=this.point(points[c]).sub(this.point(points[b]));
    const length=lower.length();if(length<1e-6)return points;lower.normalize();
    const angle=upper.angleTo(lower);if(angle<=max)return points;
    let axis=upper.clone().cross(lower);if(axis.lengthSq()<1e-6)axis=upper.clone().cross(new THREE.Vector3(0,0,1));if(axis.lengthSq()<1e-6)axis.set(1,0,0);
    lower.copy(upper).applyAxisAngle(axis.normalize(),max).multiplyScalar(length).add(this.point(points[b]));
    const corrected=points.slice();corrected[c]={x:(this.mirror?-1:1)*lower.x,y:-lower.y,z:-lower.z,confidence:points[c].confidence};return corrected;
  }
  update(frame,dt,demo=false,time=0) {
    this.dt=Math.min(Math.max(dt,0),.1);this.clock+=this.dt;
    this.poseElapsed+=this.dt;
    const raw=demo?null:frame?.pose?frame.points:null;
    if(raw!==this.poseFrame){this.poseFilter.update(raw,this.poseElapsed);this.poseFrame=raw;this.poseElapsed=0;}
    const t=1-Math.exp(-12*this.dt);let p=raw?this.poseFilter.points:null;
    for(const [a,b,c,max] of [[23,25,27,2.79],[24,26,28,2.79]])p=this.bend(p,a,b,c,max);
    const weights={};for(const s of frame?.face?frame.shapes||[]:[])weights[s.name]=s.value;
    const E=name=>{
      if(demo)return name.startsWith('eyeBlink')?(time%4.4>4.1?Math.sin((time%4.4-4.1)/.3*Math.PI):0):name.startsWith('mouthSmile')?.55:name==='jawOpen'?Math.max(0,Math.sin(time*3))*.25:0;
      if(this.mirror)name=name.endsWith('Left')?name.slice(0,-4)+'Right':name.endsWith('Right')?name.slice(0,-5)+'Left':name;
      const value=weights[name]||0;
      return name.startsWith('eyeBlink')?THREE.MathUtils.smoothstep(value,.15,.65):value;
    };
    const mapping={happy:(E('mouthSmileLeft')+E('mouthSmileRight'))/2,angry:(E('browDownLeft')+E('browDownRight'))/2,sad:(E('mouthFrownLeft')+E('mouthFrownRight'))/2,surprised:E('browInnerUp')*E('jawOpen'),aa:E('jawOpen')*(1-E('mouthClose'))*(1-E('mouthPucker')),ou:E('mouthPucker'),oh:E('mouthFunnel'),ee:(E('mouthStretchLeft')+E('mouthStretchRight'))/2,ih:(E('mouthPressLeft')+E('mouthPressRight'))/4,blinkLeft:E('eyeBlinkLeft'),blinkRight:E('eyeBlinkRight')};
    const expressions=this.vrm.expressionManager;
    if(expressions) {
      if(!expressions.getExpression('blinkLeft'))mapping.blink=(E('eyeBlinkLeft')+E('eyeBlinkRight'))/2;
      for(const expression of expressions.expressions) {
        const raw=mapping[expression.expressionName]??E(expression.expressionName);
        const value=raw*(/^(happy|angry|sad|surprised)$/.test(expression.expressionName)?this.expressionScale??1:1);
        const current=expressions.getValue(expression.expressionName)||0;const blink=/^(blink|blinkLeft|blinkRight|eyeBlinkLeft|eyeBlinkRight)$/.test(expression.expressionName);
        const response=blink?1-Math.exp(-(value>current?80:35)*this.dt):t;
        expressions.setValue(expression.expressionName,THREE.MathUtils.lerp(current,value,response));
      }
    }
    if(this.vrm.lookAt){this.vrm.lookAt.yaw=THREE.MathUtils.lerp(this.vrm.lookAt.yaw,(E('eyeLookOutLeft')+E('eyeLookInRight')-E('eyeLookInLeft')-E('eyeLookOutRight'))*12,t);this.vrm.lookAt.pitch=THREE.MathUtils.lerp(this.vrm.lookAt.pitch,(E('eyeLookUpLeft')+E('eyeLookUpRight')-E('eyeLookDownLeft')-E('eyeLookDownRight'))*8,t);}
    if(this.valid(p,11,12,23,24)) {
      const across=this.point(p[this.mirror?24:23]).sub(this.point(p[this.mirror?23:24]));
      const hipForward=across.clone().cross(new THREE.Vector3(0,1,0));
      if(hipForward.lengthSq()>1e-6)this.rotate('hips',new THREE.Quaternion().setFromRotationMatrix(new THREE.Matrix4().lookAt(hipForward,new THREE.Vector3(),new THREE.Vector3(0,1,0))),t,true);
      const up=this.point(p[11]).add(this.point(p[12])).sub(this.point(p[23])).sub(this.point(p[24]));
      const right=this.point(p[this.mirror?12:11]).sub(this.point(p[this.mirror?11:12])).normalize();const forward=right.clone().cross(up).normalize();
      if(forward.lengthSq()>1e-8){const y=up.normalize(),x=y.clone().cross(forward).normalize();this.rotate('spine',new THREE.Quaternion().setFromRotationMatrix(new THREE.Matrix4().makeBasis(x,y,forward)),t,true);}
    }else {
      // A desk webcam usually cannot see the pelvis. Retain shoulder roll
      // without using inferred hips to tip or spin the seated avatar.
      if(this.valid(p,11,12)&&this.world.has('spine')){
        const across=this.point(p[this.mirror?12:11]).sub(this.point(p[this.mirror?11:12]));
        const roll=THREE.MathUtils.clamp(Math.atan2(across.y,Math.abs(across.x)), -.3,.3);
        this.rotate('spine',new THREE.Quaternion().setFromAxisAngle(new THREE.Vector3(0,0,1),roll).multiply(this.world.get('spine')),t,true);
      }else if(this.rest.has('spine'))this.rotate('spine',this.rest.get('spine'),t);
      if(this.rest.has('hips'))this.rotate('hips',this.rest.get('hips'),t);
    }
    for(let side=0;side<2;side++) {
      const prefix=side===0?'left':'right',source=this.mirror?1-side:side,s=source===0?11:12,e=source===0?13:14,w=source===0?15:16,h=source===0?23:24,k=source===0?25:26,a=source===0?27:28,toe=source===0?31:32;
      // Clavicles stay attached to the chest; arm targets rotate joints only.
      if(this.rest.has(prefix+'Shoulder'))this.rotate(prefix+'Shoulder',this.rest.get(prefix+'Shoulder'),t);
      const torso=this.bones.get('chest')||this.bones.get('spine');
      const torsoRotation=torso?torso.getWorldQuaternion(new THREE.Quaternion()).multiply(this.world.get(torso===this.bones.get('chest')?'chest':'spine').clone().invert()):new THREE.Quaternion();
      const relaxedUpper=new THREE.Vector3(side===0?.12:-.12,-1,.08).normalize().applyQuaternion(torsoRotation);
      const reached=this.seatedArm(prefix,p,s,e,w);
      if(!reached)this.segment(prefix+'UpperArm',p,s,e,t,relaxedUpper);
      // When wrists leave the bottom of the webcam, use a bent desk pose,
      // coupled to the actual upper arm rather than a dangling world-space arm.
      const relaxedLower=new THREE.Vector3(side===0?.06:-.06,-.25,1).normalize().applyQuaternion(torsoRotation);
      const upperDirection=this.filtered.get(prefix+'UpperArm');
      if(upperDirection)relaxedLower.applyQuaternion(new THREE.Quaternion().setFromUnitVectors(relaxedUpper,upperDirection));
      if(!reached)this.segment(prefix+'LowerArm',p,e,w,t,relaxedLower,prefix+'UpperArm');
      this.segment(prefix+'UpperLeg',p,h,k,t,new THREE.Vector3(0,-1,0));this.segment(prefix+'LowerLeg',p,k,a,t,new THREE.Vector3(0,-1,0));this.segment(prefix+'Foot',p,a,toe,t,new THREE.Vector3(0,-.1,1));
      const rawHand=demo?null:frame?.[(source===0?'left':'right')+'Hand'],wrist=prefix+'Hand';
      this.handElapsed[prefix]+=this.dt;
      if(rawHand!==this.handFrames[prefix]){this.handFilters[prefix].update(rawHand,this.handElapsed[prefix]);this.handFrames[prefix]=rawHand;this.handElapsed[prefix]=0;}
      const hand=rawHand?this.handFilters[prefix].points:null;
      const palmValid=this.valid(hand,0,5,9,17)&&this.point(hand[9]).sub(this.point(hand[0])).cross(this.point(hand[17]).sub(this.point(hand[5]))).lengthSq()>1e-10;
      if(palmValid&&this.palms.has(wrist)) {
        const basis=this.basis(this.point(hand[9]).sub(this.point(hand[0])),this.point(hand[17]).sub(this.point(hand[5])),prefix);
        this.rotate(wrist,basis.multiply(this.palms.get(wrist).clone().invert()).multiply(this.world.get(wrist)),t,true);
        this.rememberHand(wrist);
      }else this.releaseHand(wrist);
      for(const [fingerIndex,finger] of ['Thumb','Index','Middle','Ring','Little'].entries())for(const [j,joint] of (finger==='Thumb'&&this.bones.has(prefix+'ThumbMetacarpal')?['Metacarpal','Proximal','Distal']:['Proximal','Intermediate','Distal']).entries()) {
        const key=prefix+finger+joint,i=1+fingerIndex*4+j;
        if(palmValid&&this.valid(hand,i,i+1)){this.aimFinger(key,this.point(hand[i+1]).sub(this.point(hand[i])),wrist,t);this.rememberHand(key);}else this.releaseHand(key);
      }
    }
    if(frame?.face&&frame.rotation?.length===9) {
      const r=frame.rotation,c=[this.mirror?-1:1,-1,-1];
      const matrix=new THREE.Matrix4().set(r[0],r[1]*c[0]*c[1],r[2]*c[0]*c[2],0,r[3]*c[1]*c[0],r[4],r[5]*c[1]*c[2],0,r[6]*c[2]*c[0],r[7]*c[2]*c[1],r[8],0,0,0,0,1);this.headRaw.setFromRotationMatrix(matrix);
    }else this.headRaw.copy(this.neutral);
    const head=demo?new THREE.Quaternion().setFromEuler(new THREE.Euler(Math.sin(time*.6)*.06,Math.sin(time*.5)*.15,0)):this.neutral.clone().invert().multiply(this.headRaw);
    const headAngles=new THREE.Euler().setFromQuaternion(head,'YXZ');
    headAngles.x=THREE.MathUtils.clamp(headAngles.x,-.65,.65);headAngles.y=THREE.MathUtils.clamp(headAngles.y,-1.1,1.1);headAngles.z=THREE.MathUtils.clamp(headAngles.z,-.45,.45);
    head.setFromEuler(headAngles);
    if(this.world.has('head'))this.rotate('head',head.multiply(this.world.get('head')),t,true);
    let offset=new THREE.Vector3();const image=frame?.imagePose;
    if(!demo&&this.valid(image,23,24)) {const x=(image[23].x+image[24].x)/2,y=(image[23].y+image[24].y)/2;if(!this.center)this.center={x,y};offset.set(THREE.MathUtils.clamp((this.mirror?-1:1)*(x-this.center.x),-.4,.4)*1.5,THREE.MathUtils.clamp(-(y-this.center.y),-.4,.4)*1.2,0);}
    this.vrm.scene.position.lerp(this.origin.clone().add(offset),t);
    this.vrm.update(dt);
  }
}
