// One Euro filtering: suppress resting jitter while responding to intentional motion.
export class PoseFilter {
  constructor(){this.reset();}
  reset(){this.points=[];this.previous=null;}
  update(points,dt){
    if(points===this.previous)return this.points;
    this.previous=points;dt=Math.max(.008,Math.min(dt,.2));
    this.points=(points||[]).map((p,i)=>{
      if(!p||![p.x,p.y,p.z,p.confidence].every(Number.isFinite))return {x:0,y:0,z:0,confidence:0};
      const old=this.points[i];if(!old||old.confidence<.55||p.confidence<.55)return {...p,velocity:0};
      const distance=Math.hypot(p.x-old.x,p.y-old.y,p.z-old.z),raw=old.raw||old;
      const speed=Math.hypot(p.x-raw.x,p.y-raw.y,p.z-raw.z)/dt;
      const velocity=old.velocity+(speed-old.velocity)*(1-Math.exp(-2*Math.PI*dt));
      const cutoff=1.3+.18*velocity,alpha=1-Math.exp(-2*Math.PI*cutoff*dt);
      // Reject impossible single-frame teleportation without changing limb lengths.
      const gain=Math.min(alpha,distance>0?3*dt/distance:1);
      return {x:old.x+(p.x-old.x)*gain,y:old.y+(p.y-old.y)*gain,z:old.z+(p.z-old.z)*gain,confidence:p.confidence,velocity,raw:{x:p.x,y:p.y,z:p.z}};
    });return this.points;
  }
}
