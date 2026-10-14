import * as THREE from './vendor/three/package/build/three.module.js';

export class AvatarRig {
  constructor(vrm) {
    this.vrm=vrm;this.bones=new Map();this.rest=new Map();this.world=new Map();this.directions=new Map();this.palms=new Map();this.origin=vrm.scene.position.clone();this.neutral=new THREE.Quaternion();this.headRaw=new THREE.Quaternion();this.center=null;
    this.filtered=new Map();this.lastSeen=new Map();this.clock=0;this.dt=1/30;
    vrm.scene.updateMatrixWorld(true);
    for(const key of Object.keys(vrm.humanoid.humanBones)) {
      const bone=vrm.humanoid.getNormalizedBoneNode(key);if(!bone)continue;
      this.bones.set(key,bone);this.rest.set(key,bone.quaternion.clone());this.world.set(key,bone.getWorldQuaternion(new THREE.Quaternion()));
      const position=bone.getWorldPosition(new THREE.Vector3());
      const next={UpperArm:'LowerArm',LowerArm:'Hand',UpperLeg:'LowerLeg',LowerLeg:'Foot',Foot:'Toes',Shoulder:'UpperArm'};
      const match=key.match(/^(left|right)(UpperArm|LowerArm|UpperLeg|LowerLeg|Foot|Shoulder)$/);
      let child=match?vrm.humanoid.getNormalizedBoneNode(match[1]+next[match[2]]):bone.children.find(c=>c.isBone)||bone.children[0];
      this.directions.set(key,child?child.getWorldPosition(new THREE.Vector3()).sub(position):position.sub(bone.parent.getWorldPosition(new THREE.Vector3())));
    }
    for(const side of ['left','right']) {
      const keys=['Hand','IndexProximal','MiddleProximal','LittleProximal'].map(k=>side+k);
      if(keys.every(k=>this.bones.has(k))) {
        const p=keys.map(k=>this.bones.get(k).getWorldPosition(new THREE.Vector3()));this.palms.set(side+'Hand',this.basis(p[2].clone().sub(p[0]),p[3].clone().sub(p[1]),side));
      }
    }
  }
  point(p){return new THREE.Vector3(-p.x,-p.y,-p.z);}
  valid(p,...indices){return p&&indices.every(i=>p[i]&&p[i].confidence>=.55);}
  basis(up,across,side) {
    if(up.lengthSq()<1e-8 || across.lengthSq()<1e-8)return new THREE.Quaternion();
    up.normalize();const normal=across.clone().cross(up).normalize();if(side==='right')normal.negate();
    const x=up.clone().cross(normal).normalize();return new THREE.Quaternion().setFromRotationMatrix(new THREE.Matrix4().makeBasis(x,up,normal));
  }
  rotate(key,target,t,world=false) {
    const bone=this.bones.get(key);if(!bone)return;
    if(world){const parent=bone.parent.getWorldQuaternion(new THREE.Quaternion()).invert();target=parent.multiply(target);}
    // Bound angular velocity so dropped frames cannot snap a joint across the body.
    const angle=bone.quaternion.angleTo(target),speed=key.includes('Arm')?5.5:key==='head'?3.5:7;
    bone.quaternion.slerp(target,angle>1e-6?Math.min(t,speed*this.dt/angle):t);bone.updateWorldMatrix(false,true);
  }
  aim(key,direction,t) {
    const rest=this.directions.get(key);if(!rest||rest.lengthSq()<1e-8||direction.lengthSq()<1e-8)return;
    const q=new THREE.Quaternion().setFromUnitVectors(rest.clone().normalize(),direction.clone().normalize()).multiply(this.world.get(key));this.rotate(key,q,t,true);
  }
  segment(key,points,a,b,t,fallback){
    const tracked=this.valid(points,a,b),previous=this.filtered.get(key);
    let direction=tracked?this.point(points[b]).sub(this.point(points[a])):fallback.clone();
    if(tracked&&direction.lengthSq()>1e-8)this.lastSeen.set(key,this.clock);
    else if(previous&&this.clock-(this.lastSeen.get(key)??-Infinity)<.3)direction=previous.clone();
    if(direction.lengthSq()<1e-8)return;
    direction.normalize();
    const filtered=previous||direction.clone();
    filtered.lerp(direction,1-Math.exp(-(tracked?9:3)*this.dt)).normalize();
    this.filtered.set(key,filtered);this.aim(key,filtered,t);
  }
  calibrate(frame){this.neutral.copy(this.headRaw);const p=frame?.imagePose;if(this.valid(p,23,24))this.center={x:(p[23].x+p[24].x)/2,y:(p[23].y+p[24].y)/2};}
  update(frame,dt,demo=false,time=0) {
    this.dt=Math.min(Math.max(dt,0),.1);this.clock+=this.dt;
    const t=1-Math.exp(-10*this.dt),p=demo?null:frame?.pose?frame.points:null;
    const weights={};for(const s of frame?.face?frame.shapes||[]:[])weights[s.name]=s.value;
    const E=name=>demo?(name.startsWith('eyeBlink')?(time%4.4>4.1?Math.sin((time%4.4-4.1)/.3*Math.PI):0):name.startsWith('mouthSmile')?.55:name==='jawOpen'?Math.max(0,Math.sin(time*3))*.25:0):weights[name]||0;
    const mapping={happy:(E('mouthSmileLeft')+E('mouthSmileRight'))/2,angry:(E('browDownLeft')+E('browDownRight'))/2,sad:(E('mouthFrownLeft')+E('mouthFrownRight'))/2,surprised:E('browInnerUp')*E('jawOpen'),aa:E('jawOpen')*(1-E('mouthClose'))*(1-E('mouthPucker')),ou:E('mouthPucker'),oh:E('mouthFunnel'),ee:(E('mouthStretchLeft')+E('mouthStretchRight'))/2,ih:(E('mouthPressLeft')+E('mouthPressRight'))/4,blinkLeft:E('eyeBlinkLeft'),blinkRight:E('eyeBlinkRight')};
    const expressions=this.vrm.expressionManager;
    if(expressions) {
      if(!expressions.getExpression('blinkLeft'))mapping.blink=(E('eyeBlinkLeft')+E('eyeBlinkRight'))/2;
      for(const expression of expressions.expressions) {
        const raw=mapping[expression.expressionName]??E(expression.expressionName);
        const value=raw*(/^(happy|angry|sad|surprised)$/.test(expression.expressionName)?this.expressionScale??1:1);
        const current=expressions.getValue(expression.expressionName)||0;expressions.setValue(expression.expressionName,THREE.MathUtils.lerp(current,value,t));
      }
    }
    if(this.vrm.lookAt){this.vrm.lookAt.yaw=THREE.MathUtils.lerp(this.vrm.lookAt.yaw,(E('eyeLookOutLeft')+E('eyeLookInRight')-E('eyeLookInLeft')-E('eyeLookOutRight'))*12,t);this.vrm.lookAt.pitch=THREE.MathUtils.lerp(this.vrm.lookAt.pitch,(E('eyeLookUpLeft')+E('eyeLookUpRight')-E('eyeLookDownLeft')-E('eyeLookDownRight'))*8,t);}
    if(this.valid(p,11,12,23,24)) {
      const up=this.point(p[11]).add(this.point(p[12])).sub(this.point(p[23])).sub(this.point(p[24]));
      const right=this.point(p[11]).sub(this.point(p[12])).normalize();const forward=right.clone().cross(up).normalize();
      if(forward.lengthSq()>1e-8){const y=up.normalize(),x=y.clone().cross(forward).normalize();this.rotate('spine',new THREE.Quaternion().setFromRotationMatrix(new THREE.Matrix4().makeBasis(x,y,forward)),t,true);}
    }else if(this.rest.has('spine'))this.rotate('spine',this.rest.get('spine'),t);
    for(let side=0;side<2;side++) {
      const prefix=side===0?'left':'right',s=side===0?11:12,e=side===0?13:14,w=side===0?15:16,h=side===0?23:24,k=side===0?25:26,a=side===0?27:28,toe=side===0?31:32;
      // Clavicles stay attached to the chest; arm targets rotate joints only.
      if(this.rest.has(prefix+'Shoulder'))this.rotate(prefix+'Shoulder',this.rest.get(prefix+'Shoulder'),t);
      this.segment(prefix+'UpperArm',p,s,e,t,new THREE.Vector3(side===0?.18:-.18,-1,0));this.segment(prefix+'LowerArm',p,e,w,t,new THREE.Vector3(side===0?.08:-.08,-1,.15));
      this.segment(prefix+'UpperLeg',p,h,k,t,new THREE.Vector3(0,-1,0));this.segment(prefix+'LowerLeg',p,k,a,t,new THREE.Vector3(0,-1,0));this.segment(prefix+'Foot',p,a,toe,t,new THREE.Vector3(0,-.1,1));
      const hand=demo?null:frame?.[prefix+'Hand'],wrist=prefix+'Hand';
      if(this.valid(hand,0,5,9,17)&&this.palms.has(wrist)) {
        const basis=this.basis(this.point(hand[9]).sub(this.point(hand[0])),this.point(hand[17]).sub(this.point(hand[5])),prefix);
        this.rotate(wrist,basis.multiply(this.palms.get(wrist).clone().invert()).multiply(this.world.get(wrist)),t,true);
      }else if(this.rest.has(wrist))this.rotate(wrist,this.rest.get(wrist),t);
      for(const [fingerIndex,finger] of ['Thumb','Index','Middle','Ring','Little'].entries())for(const [j,joint] of ['Proximal','Intermediate','Distal'].entries()) {
        const key=prefix+finger+joint,i=1+fingerIndex*4+j;
        if(this.valid(hand,i,i+1))this.aim(key,this.point(hand[i+1]).sub(this.point(hand[i])),t);else if(this.rest.has(key))this.rotate(key,this.rest.get(key),t);
      }
    }
    if(frame?.face&&frame.rotation?.length===9) {
      const r=frame.rotation;const matrix=new THREE.Matrix4().set(r[0],-r[1],r[2],0,-r[3],r[4],-r[5],0,r[6],-r[7],r[8],0,0,0,0,1);this.headRaw.setFromRotationMatrix(matrix);
    }else this.headRaw.identity();
    const head=demo?new THREE.Quaternion().setFromEuler(new THREE.Euler(Math.sin(time*.6)*.06,Math.sin(time*.5)*.15,0)):this.neutral.clone().invert().multiply(this.headRaw);
    const headAngles=new THREE.Euler().setFromQuaternion(head,'YXZ');
    headAngles.x=THREE.MathUtils.clamp(headAngles.x,-.65,.65);headAngles.y=THREE.MathUtils.clamp(headAngles.y,-1.1,1.1);headAngles.z=THREE.MathUtils.clamp(headAngles.z,-.45,.45);
    head.setFromEuler(headAngles);
    if(this.world.has('head'))this.rotate('head',head.multiply(this.world.get('head')),t,true);
    let offset=new THREE.Vector3();const image=frame?.imagePose;
    if(!demo&&this.valid(image,23,24)) {const x=(image[23].x+image[24].x)/2,y=(image[23].y+image[24].y)/2;if(!this.center)this.center={x,y};offset.set(THREE.MathUtils.clamp(-(x-this.center.x),-.4,.4)*1.5,THREE.MathUtils.clamp(-(y-this.center.y),-.4,.4)*1.2,0);}
    this.vrm.scene.position.lerp(this.origin.clone().add(offset),t);
    this.vrm.update(dt);
  }
}
