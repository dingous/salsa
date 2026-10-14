import * as THREE from './vendor/three/package/build/three.module.js';
export function solveArm(target,pole,a,b){
 const min=Math.sqrt(a*a+b*b+2*a*b*Math.cos(150*Math.PI/180));
 const distance=THREE.MathUtils.clamp(target.length(),min,a+b-.0001);
 const axis=target.lengthSq()>1e-6?target.clone().normalize():new THREE.Vector3(0,-1,0);
 let bend=pole.clone().addScaledVector(axis,-pole.dot(axis));
 for(const fallback of [new THREE.Vector3(0,0,1),new THREE.Vector3(1,0,0)])if(bend.lengthSq()<1e-6)bend=fallback.addScaledVector(axis,-fallback.dot(axis));
 const along=(a*a-b*b+distance*distance)/(2*distance);
 const elbow=axis.clone().multiplyScalar(along).addScaledVector(bend.normalize(),Math.sqrt(Math.max(0,a*a-along*along)));
 const goal=axis.clone().multiplyScalar(distance);
 return {upper:elbow.clone().normalize(),lower:goal.clone().sub(elbow).normalize(),target:goal};
}
