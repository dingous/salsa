const worker=new Worker('./browser-tracking-worker.js'),status=document.getElementById('status');
const timer=setTimeout(()=>{status.textContent='FAIL · timeout';worker.terminate();},60000);
worker.onerror=event=>{clearTimeout(timer);status.textContent='FAIL · '+event.message;};
worker.onmessage=async({data})=>{
  if(data.type==='ready'){
    const canvas=document.createElement('canvas');canvas.width=640;canvas.height=480;
    const ctx=canvas.getContext('2d');ctx.fillStyle='#eee';ctx.fillRect(0,0,640,480);
    const image=await createImageBitmap(canvas);worker.postMessage({type:'frame',image,time:performance.now()},[image]);
  }else if(data.type==='result'){
    clearTimeout(timer);status.textContent=(!data.frame.face&&!data.frame.pose&&!data.frame.leftHand.length&&!data.frame.rightHand.length)?'PASS · rosto, corpo e mãos inicializados; imagem vazia sem falsas detecções':'FAIL · detecção inesperada';
    document.getElementById('result').textContent=JSON.stringify(data.frame,null,2);worker.terminate();
  }else if(data.type==='error'){clearTimeout(timer);status.textContent='FAIL · '+data.error;worker.terminate();}
};
worker.postMessage({type:'init'});
