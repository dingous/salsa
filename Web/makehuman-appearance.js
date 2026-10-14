import * as THREE from './vendor/three/package/build/three.module.js';
export function sanitizeHuman(value={}){value=value&&typeof value==='object'?value:{};return {style:value.style==='stylized'?'stylized':'realistic',hairStyle:[0,1,2,3].includes(value.hairStyle)?value.hairStyle:0,beard:value.beard===true,glasses:value.glasses===true};}
export class HumanAppearance {
  constructor(vrm){this.meshes=[];this.original=new Map();this.toon=new Map();this.available=false;vrm.scene.traverse(mesh=>{if(!mesh.isMesh)return;this.meshes.push(mesh);if(mesh.morphTargetDictionary?.identity_faceWidth!==undefined)this.available=true;this.original.set(mesh,mesh.material);});}
  apply(value){this.value=sanitizeHuman(value);if(!this.available)return;
    for(const mesh of this.meshes){const name=mesh.name.toLowerCase();if(name.includes('short02'))mesh.visible=this.value.hairStyle===0;else if(name.includes('short04'))mesh.visible=this.value.hairStyle===1;else if(name.includes('long01'))mesh.visible=this.value.hairStyle===2;else if(name.includes('beard'))mesh.visible=this.value.beard;else if(name.includes('glasses'))mesh.visible=this.value.glasses;
      const original=this.original.get(mesh),materials=Array.isArray(original)?original:[original];
      const converted=materials.map(material=>{if(!this.toon.has(material)){const toon=new THREE.MeshToonMaterial({map:material.map,color:material.userData.avatarOriginalColor?.clone()||material.color.clone(),alphaTest:material.alphaTest,transparent:material.transparent,opacity:material.opacity,side:material.side});toon.name=material.name;this.toon.set(material,toon);}return this.toon.get(material);});
      mesh.material=this.value.style==='stylized'?(Array.isArray(original)?converted:converted[0]):original;
    }this.morphs();
  }
  morphs(){if(!this.available)return;for(const mesh of this.meshes){const index=mesh.morphTargetDictionary?.identity_stylized;if(index!==undefined)mesh.morphTargetInfluences[index]=this.value?.style==='stylized'?1:0;}}
  dispose(){for(const material of this.toon.values())material.dispose();}
}
