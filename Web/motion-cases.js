// Anatomical landmark fixtures: camera input is not horizontally flipped.
export function poseFrame(action='neutral') {
  const points=Array.from({length:33},()=>({x:0,y:0,z:0,confidence:0}));
  const set=(i,x,y,z=0)=>points[i]={x,y,z,confidence:1};
  set(11,.2,-.5);set(12,-.2,-.5);set(13,.25,-.2);set(14,-.25,-.2);set(15,.28,.05);set(16,-.28,.05);
  set(23,.15,0);set(24,-.15,0);set(25,.15,.4);set(26,-.15,.4);set(27,.15,.8);set(28,-.15,.8);set(31,.15,.82,-.15);set(32,-.15,.82,-.15);
  if(action==='left-arm'){set(13,.5,-.85);set(15,.7,-1.1);}
  if(action==='right-arm'){set(14,-.5,-.85);set(16,-.7,-1.1);}
  if(action==='left-forward'){set(13,.2,-.5,-.3);set(15,.2,-.5,-.65);}
  if(action==='left-leg'){set(25,.15,.15,-.4);set(27,.15,.45,-.55);}
  if(action==='lean'){set(11,.38,-.5);set(12,-.02,-.5);}
  const imagePose=points.map(p=>({...p,x:p.x+.5,y:p.y+.5}));
  return {face:true,pose:true,points,imagePose,rotation:[1,0,0,0,1,0,0,0,1],shapes:[],leftHand:[],rightHand:[]};
}

export function runMotionChecks(rig,THREE) {
  const results=[],check=(name,condition)=>{results.push({name,pass:!!condition});if(!condition)throw new Error(name);};
  const settle=frame=>{for(let i=0;i<150;i++)rig.update(frame?{...frame,points:frame.points.map(p=>({...p}))}:null,1/30);rig.vrm.scene.updateMatrixWorld(true);};
  const direction=(a,b)=>rig.bones.get(b).getWorldPosition(new THREE.Vector3()).sub(rig.bones.get(a).getWorldPosition(new THREE.Vector3())).normalize();
  for(const mirror of [false,true]) {
    rig.setMirror(mirror);settle(poseFrame());
    const label=mirror?'espelho':'anatômico';
    const attachments=[...rig.bones].map(([key,bone])=>[key,bone.position.clone()]);
    for(const source of ['left','right']) {
      const active=mirror?(source==='left'?'right':'left'):source,inactive=active==='left'?'right':'left';
      settle(poseFrame(source+'-arm'));
      const arm=direction(active+'UpperArm',active+'LowerArm'),other=direction(inactive+'UpperArm',inactive+'LowerArm');
      check(label+': '+source+' levanta o braço correto',arm.y>.6&&other.y<-.7);
      check(label+': braço não cruza o tronco',active==='left'?arm.x>.3:arm.x<-.3);
      check(label+': antebraço acompanha o cotovelo',direction(active+'LowerArm',active+'Hand').y>.6);
    }
    settle(poseFrame('left-forward'));
    const active=mirror?'right':'left';
    check(label+': braço avança em direção à câmera',direction(active+'UpperArm',active+'LowerArm').z>.8);
    settle(poseFrame('left-leg'));
    check(label+': perna correta avança',direction(active+'UpperLeg',active+'LowerLeg').z>.8);
    const extreme=poseFrame();extreme.points[15]={x:.25,y:-.9,z:0,confidence:1};extreme.points[27]={x:.15,y:.01,z:0,confidence:1};settle(extreme);
    check(label+': cotovelo respeita flexão máxima',direction(active+'UpperArm',active+'LowerArm').angleTo(direction(active+'LowerArm',active+'Hand'))<2.65);
    check(label+': joelho respeita flexão máxima',direction(active+'UpperLeg',active+'LowerLeg').angleTo(direction(active+'LowerLeg',active+'Foot'))<2.82);
    settle(poseFrame('neutral'));
    const forward=new THREE.Vector3(0,0,1).applyQuaternion(rig.bones.get('spine').getWorldQuaternion(new THREE.Quaternion()));
    check(label+': tronco neutro não gira 180 graus',forward.z>.9);
    settle(poseFrame('lean'));
    const up=new THREE.Vector3(0,1,0).applyQuaternion(rig.bones.get('spine').getWorldQuaternion(new THREE.Quaternion()));
    check(label+': inclinação lateral correta',mirror?up.x<-.15:up.x>.15);
    const face=poseFrame(),angle=.3;face.rotation=[Math.cos(angle),0,Math.sin(angle),0,1,0,-Math.sin(angle),0,Math.cos(angle)];settle(face);
    const yaw=new THREE.Euler().setFromQuaternion(rig.bones.get('head').getWorldQuaternion(new THREE.Quaternion()),'YXZ').y;
    check(label+': cabeça usa a mesma conversão de eixos',mirror?yaw>.2:yaw<-.2);
    const gestures=poseFrame();gestures.shapes=[{name:'eyeBlinkLeft',value:1},{name:'jawOpen',value:1}];
    gestures.leftHand=Array.from({length:21},(_,i)=>({x:i===0?0:((i-1)%4+1)*.025,y:0,z:0,confidence:1}));
    settle(gestures);
    if(rig.vrm.expressionManager){
      check(label+': pisca o olho correspondente',rig.vrm.expressionManager.getValue(mirror?'blinkRight':'blinkLeft')>.9&&rig.vrm.expressionManager.getValue(mirror?'blinkLeft':'blinkRight')<.1);
      check(label+': abertura da boca preservada',rig.vrm.expressionManager.getValue('aa')>.9);
    }
    for(const finger of ['Thumb','Index','Middle','Ring','Little']) {
      const first=finger==='Thumb'?'Metacarpal':'Proximal',second=finger==='Thumb'?'Proximal':'Intermediate';
      const d=direction(active+finger+first,active+finger+second);
      check(label+': mão correta move o dedo '+finger,mirror?d.x<-.9:d.x>.9);
    }
    check(label+': ligações do esqueleto preservadas',attachments.every(([key,p])=>rig.bones.get(key).position.distanceTo(p)<1e-6));
    rig.calibrate(face);settle(null);
    const neutralHead=new THREE.Euler().setFromQuaternion(rig.bones.get('head').getWorldQuaternion(new THREE.Quaternion()),'YXZ');
    check(label+': perda de rosto após calibrar mantém cabeça neutra',Math.abs(neutralHead.y)<.02);
    rig.setMirror(mirror);
    settle(null);check(label+': perda de sinal retorna ao repouso',direction('leftUpperArm','leftLowerArm').y<-.9&&direction('rightUpperArm','rightLowerArm').y<-.9);
  }
  return results;
}
