export class BrowserTracker {
  constructor(onFrame,onError,onStatus=()=>{}){this.onFrame=onFrame;this.onError=onError;this.onStatus=onStatus;this.generation=0;this.timer=null;}
  async start(index=0,poseQuality='full') {
    this.stop();const generation=this.generation;
    if(!navigator.mediaDevices?.getUserMedia)throw new Error('Use Chrome ou Edge em HTTPS para acessar a webcam.');
    this.onStatus('Permita o acesso à webcam no navegador.');
    let deviceTimeout;
    const devices=(await Promise.race([navigator.mediaDevices.enumerateDevices(),new Promise(resolve=>{deviceTimeout=setTimeout(()=>resolve([]),3000);})]).finally(()=>clearTimeout(deviceTimeout))).filter(d=>d.kind==='videoinput');
    const request=navigator.mediaDevices.getUserMedia({audio:false,video:{width:{ideal:640},height:{ideal:480},...(devices[index]?.deviceId?{deviceId:{exact:devices[index].deviceId}}:{facingMode:'user'})}});
    request.then(stream=>{if(generation!==this.generation)stream.getTracks().forEach(t=>t.stop());}).catch(()=>{});
    let permissionTimeout;
    const stream=await Promise.race([request,new Promise((_,reject)=>{permissionTimeout=setTimeout(()=>{this.stop();reject(new Error('A webcam não respondeu. Confira a permissão de câmera e tente novamente.'));},30000);})]).finally(()=>clearTimeout(permissionTimeout));
    if(generation!==this.generation){stream.getTracks().forEach(t=>t.stop());return;}
    this.onStatus('Webcam aberta. Carregando rosto, corpo e mãos…');
    this.stream=stream;this.video=document.createElement('video');this.video.muted=true;this.video.playsInline=true;this.video.srcObject=stream;await this.video.play();
    this.worker=new Worker(new URL('./browser-tracking-worker.js',import.meta.url));
    await new Promise((resolve,reject)=>{
      const timeout=setTimeout(()=>reject(new Error('O rastreador demorou para carregar. Tente novamente.')),60000);
      this.worker.onerror=event=>{clearTimeout(timeout);reject(new Error(event.message||'Falha no rastreador.'));};
      this.worker.onmessage=({data})=>{if(data.type==='ready'){clearTimeout(timeout);resolve();}else if(data.type==='error'){clearTimeout(timeout);reject(new Error(data.error));}};
      this.worker.postMessage({type:'init',poseQuality});
    });
    if(generation!==this.generation)return;
    this.worker.onmessage=({data})=>{
      if(generation!==this.generation)return;
      if(data.type==='result'){this.onFrame(data.frame);this.timer=setTimeout(()=>this.capture(generation),35);}
      else if(data.type==='error'){this.stop();this.onError(data.error);}
    };
    this.worker.onerror=event=>{this.stop();this.onError(event.message||'Falha no rastreador.');};
    stream.getVideoTracks()[0].onended=()=>{if(generation===this.generation){this.stop();this.onError('A webcam foi desconectada.');}};
    await this.capture(generation);
  }
  async capture(generation){
    try{const image=await createImageBitmap(this.video);if(generation!==this.generation){image.close();return;}this.worker.postMessage({type:'frame',image,time:performance.now()},[image]);}
    catch(error){if(generation===this.generation){this.stop();this.onError(error.message);}}
  }
  stop(){this.generation++;clearTimeout(this.timer);this.worker?.terminate();this.worker=null;this.stream?.getTracks().forEach(t=>t.stop());this.stream=null;if(this.video){this.video.srcObject=null;this.video=null;}this.onFrame({face:false,pose:false,points:[],shapes:[]});}
}
