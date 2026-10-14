// One Euro in meters: same tuning and short-occlusion policy as Windows.
export class PoseFilter {
 constructor(cutoff=1.2,beta=.65){this.cutoff=cutoff;this.beta=beta;this.reset();}
 reset(){this.points=[];this.previous=null;this.clock=0;}
 update(points,dt){
  if(points===this.previous)return this.points;
  if(dt>.4)this.points=[];
  this.previous=points;dt=Math.max(1/120,Math.min(dt,.15));this.clock+=dt;
  const alpha=cutoff=>1/(1+1/(2*Math.PI*cutoff*dt));
  this.points=(points||[]).map((p,i)=>{
   if(!p||![p.x,p.y,p.z,p.confidence].every(Number.isFinite))p={x:0,y:0,z:0,confidence:0};
   const old=this.points[i];
   if(p.confidence<.55){if(old&&old.confidence>=.55&&this.clock-old.seen<.15)return {...old,confidence:.55};return {...p};}
   if(!old||old.confidence<.55)return {...p,velocity:[0,0,0],raw:{x:p.x,y:p.y,z:p.z},seen:this.clock};
   const distance=Math.hypot(p.x-old.x,p.y-old.y,p.z-old.z),limit=3*dt+.025,gain=distance>0?Math.min(1,limit/distance):1;
   const raw={x:old.x+(p.x-old.x)*gain,y:old.y+(p.y-old.y)*gain,z:old.z+(p.z-old.z)*gain};
   const velocity=['x','y','z'].map((key,j)=>(old.velocity?.[j]||0)+(((raw[key]-(old.raw?.[key]??old[key]))/dt)-(old.velocity?.[j]||0))*alpha(1));
   const smoothing=alpha(this.cutoff+Math.hypot(...velocity)*this.beta);
   return {x:old.x+(raw.x-old.x)*smoothing,y:old.y+(raw.y-old.y)*smoothing,z:old.z+(raw.z-old.z)*smoothing,confidence:p.confidence,velocity,raw,seen:this.clock};
  });return this.points;
 }
}
