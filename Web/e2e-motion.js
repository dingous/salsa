import * as THREE from './vendor/three/package/build/three.module.js';
import {GLTFLoader} from './vendor/three/package/examples/jsm/loaders/GLTFLoader.js';
import {VRMLoaderPlugin} from './vendor/three-vrm/package/lib/three-vrm.module.js';
import {AvatarRig} from './rig.js';
import {runMotionChecks,poseFrame} from './motion-cases.js';
const status=document.getElementById('status');
try {
  const loader=new GLTFLoader();loader.register(p=>new VRMLoaderPlugin(p));
  const gltf=await loader.loadAsync('assets/Seed-san.vrm'),vrm=gltf.userData.vrm;
  vrm.scene.traverse(node=>{const a=gltf.parser.associations.get(node);if(gltf.parser.json.nodes?.[a?.nodes]?.name==='robo_arm')node.visible=false;});
  const rig=new AvatarRig(vrm),results=runMotionChecks(rig,THREE);
  for(const result of results){const li=document.createElement('li');li.textContent='PASS · '+result.name;document.getElementById('results').append(li);}
  status.textContent=`PASS · ${results.length} verificações no esqueleto real`;
  rig.setMirror(true);for(let i=0;i<150;i++)rig.update(poseFrame('left-arm'),1/30);
  const scene=new THREE.Scene();scene.background=new THREE.Color('#292646');scene.add(vrm.scene,new THREE.HemisphereLight('#ffffff','#8899aa',3));
  const camera=new THREE.PerspectiveCamera(35,1.5,.01,20);camera.position.set(0,.9,3);camera.lookAt(0,.9,0);
  const renderer=new THREE.WebGLRenderer({canvas:document.getElementById('preview'),antialias:true});renderer.setSize(960,640);renderer.render(scene,camera);
}catch(error){status.textContent='FAIL · '+error.message;console.error(error);}
