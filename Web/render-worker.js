import * as THREE from './vendor/three/package/build/three.module.js';
import {GLTFLoader} from './vendor/three/package/examples/jsm/loaders/GLTFLoader.js';
import {VRMLoaderPlugin,VRMUtils} from './vendor/three-vrm/package/lib/three-vrm.module.js';
import {AvatarRig} from './rig.js';
import {countHands} from './tracking-utils.js';
import {HumanAppearance} from './makehuman-appearance.js';

let renderer,scene,camera,loader,vrm,rig,token='',client='',cameraSession=null,frame=null,demo=false,virtual=false,sending=false,framingMode='bust',elapsed=0,previous=performance.now(),version=0,renderFailed=false;
let failedSince=0,nextSend=0;
let presentationMode='professional',keyLight,fillLight;
let mirrored=true,hosted=false;
let appearanceColors={},originalMaterials=new Map();
let humanAppearance,humanSettings={};
function applyAppearance(){
  vrm?.scene.traverse(mesh=>{if(!mesh.isMesh)return;for(const material of Array.isArray(mesh.material)?mesh.material:[mesh.material]){if(!material.color)continue;material.userData.avatarOriginalColor??=material.color.clone();const name=material.name.toLowerCase();const key=name.includes('hair')?'hair':name.includes('eye')&&!name.includes('trans')?'eyes':/huku|cloth|outfit/.test(name)?'outfit':/body_bake|skin/.test(name)?'skin':null;material.color.copy(material.userData.avatarOriginalColor);if(key&&/^#[0-9a-f]{6}$/i.test(appearanceColors[key]||''))material.color.set(appearanceColors[key]);}});
}
function backdrop(value) {
  if(scene.background?.isTexture)scene.background.dispose();
  if(value!=='corporate'){scene.background=new THREE.Color({studio:'#292646',green:'#00ff00',dark:'#10131f'}[value]||'#292646');return;}
  const canvas=new OffscreenCanvas(1280,720),ctx=canvas.getContext('2d');
  const gradient=ctx.createLinearGradient(0,0,1280,720);gradient.addColorStop(0,'#edf2f5');gradient.addColorStop(1,'#abbac6');ctx.fillStyle=gradient;ctx.fillRect(0,0,1280,720);
  ctx.fillStyle='#ffffff35';ctx.fillRect(80,0,230,720);ctx.fillRect(950,0,210,720);
  ctx.fillStyle='#70839125';ctx.fillRect(0,560,1280,160);
  const texture=new THREE.CanvasTexture(canvas);texture.colorSpace=THREE.SRGBColorSpace;scene.background=texture;
}
const message=(type,data)=>postMessage({type,...data});
async function api(path,body,session) {
  const response=await fetch('/api/'+path,{method:body?'POST':'GET',signal:AbortSignal.timeout(4000),headers:{'X-Avatar-Token':token,'X-Avatar-Client':client,...(body?{'Content-Type':'image/jpeg','X-Camera-Session':session}:{})},body});
  let result;try{result=await response.json();}catch{const error=new Error('A ponte local está indisponível.');error.code=response.status;throw error;}if(response.status===403)message('authExpired',{});
  if(!response.ok){const error=new Error(result.error||'Ponte local desconectada.');error.code=response.status;throw error;}return result;
}
function framing() {
  if(!vrm)return;const box=new THREE.Box3().setFromObject(vrm.scene),center=box.getCenter(new THREE.Vector3()),head=vrm.humanoid.getRawBoneNode('head')?.getWorldPosition(new THREE.Vector3());
  const full=framingMode==='full',y=full?center.y:(head?.y??center.y)-.18;camera.position.set(0,y,full?4.2:2.5);camera.lookAt(0,y,0);
}
async function load(source) {
  const current=++version;let candidate;
  try {
    const gltf=typeof source==='string'?await loader.loadAsync(source):await new Promise((resolve,reject)=>loader.parse(source,'',resolve,reject));candidate=gltf.userData.vrm;
    if(!candidate)throw new Error('O arquivo precisa conter um avatar VRM 0.x ou 1.0.');
    if(current!==version){VRMUtils.deepDispose(candidate.scene);return;}
    VRMUtils.rotateVRM0(candidate);
    // Seed-san includes a separate decorative robot arm, unrelated to the human rig.
    if(typeof source==='string'&&source.endsWith('/Seed-san.vrm'))candidate.scene.traverse(node=>{
      const association=gltf.parser.associations.get(node),original=gltf.parser.json.nodes?.[association?.nodes];
      if(original?.name==='robo_arm'||/^robo_arm(?:_\d+)?$/.test(node.name))node.visible=false;
    });
    candidate.scene.updateMatrixWorld(true);
    const bounds=new THREE.Box3().setFromObject(candidate.scene),height=bounds.getSize(new THREE.Vector3()).y;if(height<.1)throw new Error('O modelo não contém uma malha visível.');
    candidate.scene.scale.multiplyScalar(2.1/height);candidate.scene.position.y=-bounds.min.y*2.1/height;candidate.scene.updateMatrixWorld(true);
    if(vrm){humanAppearance?.dispose();scene.remove(vrm.scene);VRMUtils.deepDispose(vrm.scene);}vrm=candidate;scene.add(vrm.scene);
    humanAppearance=new HumanAppearance(vrm);humanAppearance.apply(humanSettings);
    originalMaterials.clear();vrm.scene.traverse(node=>{if(node.isMesh)for(const material of Array.isArray(node.material)?node.material:[node.material])if(material?.color&&!originalMaterials.has(material))originalMaterials.set(material,material.color.clone());});applyAppearance();
    rig=new AvatarRig(vrm);rig.setMirror(mirrored);rig.expressionScale=presentationMode==='corporate'?.75:1;framing();
    message('loaded',{makehuman:humanAppearance.available,name:vrm.meta?.name||vrm.meta?.title||'Meu avatar VRM',authors:vrm.meta?.authors?.join(', ')||vrm.meta?.author||'Autor informado no arquivo',expressions:vrm.expressionManager?.expressions.length||0});
  }catch(error){if(candidate&&candidate!==vrm)VRMUtils.deepDispose(candidate.scene);message('error',{error:error.message});}
}
async function poll() {
  let retry=false;try{frame=await api('tracking');}catch{frame=null;retry=true;}
  message('tracking',{face:!!frame?.face,pose:!!frame?.pose,hands:countHands(frame)});
  setTimeout(poll,retry?1000:65);
}
async function tick() {
  if(renderFailed)return;
  const now=performance.now(),dt=Math.min((now-previous)/1000,.1);previous=now;elapsed+=dt;
  try{rig?.update(frame,dt,demo,elapsed);humanAppearance?.morphs();renderer.render(scene,camera);}catch(error){renderFailed=true;virtual=false;message('renderError',{error:error.message});return;}
  if(virtual&&!sending&&now>=nextSend) {
    sending=true;
    const session=cameraSession;
    try {
      const blob=await renderer.domElement.convertToBlob({type:'image/jpeg',quality:.88});
      if(virtual&&session===cameraSession){await api('frame',blob,session);if(failedSince)message('cameraRecovered',{});failedSince=0;nextSend=0;}
    }catch(error) {
      if(virtual&&session===cameraSession&&error.code!==409&&error.code!==403) {
        if(!failedSince){failedSince=performance.now();message('cameraRetrying',{});}
        nextSend=performance.now()+250;
        if(error.code===400||performance.now()-failedSince>8000){virtual=false;message('cameraError',{error:error.message});}
      }
    }finally{sending=false;}
  }
}
self.onmessage=async event=>{
  const data=event.data;
  if(data.type==='init') {
    try {
      token=data.token;client=data.client;hosted=!!data.hosted;scene=new THREE.Scene();scene.background=new THREE.Color('#292646');camera=new THREE.PerspectiveCamera(30,1280/720,.1,20);
      renderer=new THREE.WebGLRenderer({canvas:data.canvas,antialias:true,preserveDrawingBuffer:true});renderer.setSize(1280,720,false);renderer.setPixelRatio(1);renderer.outputColorSpace=THREE.SRGBColorSpace;renderer.toneMapping=THREE.ACESFilmicToneMapping;
      fillLight=new THREE.HemisphereLight('#fff1df','#8995c0',2);scene.add(fillLight);keyLight=new THREE.DirectionalLight('#fff5e8',2);keyLight.position.set(-2,3,3);scene.add(keyLight);
      humanSettings=data.humanSettings||{};loader=new GLTFLoader();loader.register(parser=>new VRMLoaderPlugin(parser));await load(data.source||new URL('assets/CorporateHuman.vrm',self.location.href).href);if(!hosted)poll();setInterval(tick,1000/30);
    }catch(error){message('error',{error:error.message});}
  }else if(data.type==='load')load(data.source);
  else if(data.type==='demo')demo=data.value;
  else if(data.type==='appearance'){appearanceColors=data.value||{};applyAppearance();}
  else if(data.type==='humanAppearance'){humanSettings=data.value||{};humanAppearance?.apply(humanSettings);applyAppearance();}
  else if(data.type==='mirror'){mirrored=!!data.value;rig?.setMirror(mirrored);}
  else if(data.type==='trackingFrame'){frame=data.frame;message('tracking',{face:!!frame?.face,pose:!!frame?.pose,hands:countHands(frame)});}
  else if(data.type==='virtual'){if(cameraSession!==(data.session||null)){failedSince=0;nextSend=0;}virtual=data.value;cameraSession=data.session||null;}
  else if(data.type==='token'){token=data.token;virtual=false;cameraSession=null;}
  else if(data.type==='framing'){framingMode=data.value;framing();}
  else if(data.type==='background')backdrop(data.value);
  else if(data.type==='presentation'){
    presentationMode=data.value==='corporate'?'corporate':'professional';
    const corporate=presentationMode==='corporate';if(rig)rig.expressionScale=corporate?.75:1;
    keyLight?.color.set(corporate?'#ffffff':'#fff5e8');if(keyLight)keyLight.intensity=corporate?1.7:2;
    fillLight?.color.set(corporate?'#f1f6ff':'#fff1df');
  }
  else if(data.type==='calibrate')rig?.calibrate(frame);
};
