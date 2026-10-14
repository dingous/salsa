import {BrowserTracker} from './browser-tracker.js';
import {readAppearance,writeAppearance,sanitizeAppearance,loadGuestModel,saveGuestModel,clearGuestModel} from './appearance-storage.js';
const hosted=!['127.0.0.1','localhost'].includes(location.hostname)||new URLSearchParams(location.search).get('mode')==='browser';
const $=id=>document.getElementById(id),status=(text,error=false)=>{$('status').textContent=text;$('status').classList.toggle('error',error);};
const browserTracker=new BrowserTracker(frame=>{worker?.postMessage({type:'trackingFrame',frame});},error=>{tracking=false;$('track').textContent='Iniciar rastreamento';$('camera').disabled=$('poseQuality').disabled=false;status(error,true);},text=>status(text));
const client=crypto.randomUUID();
let token='',tracking=false,virtual=false,cameraSession=null,detected={face:false,pose:false,hands:0},worker,ready=false,epoch=0,pending=false,latest=null,reconnecting=null,bridgeWasOffline=false;
let appearance=readAppearance(),pendingModel=null;
if(!appearance){let previous;try{previous=localStorage.getItem('avatarPresentation');}catch{}appearance=sanitizeAppearance({presentation:previous});}
function savePreferences(){
  appearance=sanitizeAppearance({...appearance,presentation:$('presentation').value,background:$('background').value,framing:$('framing').value,mirror:$('mirror').checked,poseQuality:$('poseQuality').value});
  $('preferencesState').textContent=writeAppearance(appearance)?'Aparência salva neste navegador por 180 dias.':'Cookie bloqueado: preferências válidas somente nesta sessão.';
}
$('virtual').disabled=true;
async function handshake() {
  const response=await fetch('/api/session',{signal:AbortSignal.timeout(5000)});
  if(!response.ok)throw new Error('A ponte local não respondeu.');
  const session=await response.json();
  if(session.app!=='AvatarStudioWeb'||session.version!==2)throw new Error('A ponte local está desatualizada. Abra novamente Iniciar Avatar Studio Web.cmd.');
  token=session.token;worker?.postMessage({type:'token',token});return session;
}
async function api(path,body,retry=true) {
  const response=await fetch('/api/'+path,{method:body===undefined?'GET':'POST',signal:AbortSignal.timeout(path==='camera/start'?12000:5000),headers:{'X-Avatar-Token':token,'X-Avatar-Client':client,...(body!==undefined?{'Content-Type':'application/json'}:{})},body:body===undefined?undefined:JSON.stringify(body)});
  let result;
  try{result=await response.json();}catch{throw new Error('A ponte local está indisponível. Tentando reconectar…');}
  if(response.status===403&&retry) {
    if(!reconnecting)reconnecting=handshake().finally(()=>reconnecting=null);
    await reconnecting;
    // Reads are safe to retry; a failed mutation leaves its result explicit to the user.
    if(body===undefined)return api(path,undefined,false);
    throw new Error('Ponte reconectada. Tente a operação novamente.');
  }
  if(!response.ok)throw new Error(result.error||'A ponte local não respondeu.');return result;
}
const send=(type,value)=>worker?.postMessage({type,value});
function presentation(value,save=true) {
  value=value==='corporate'?'corporate':'professional';
  document.body.dataset.presentation=value;$('presentation').value=value;
  $('profileNote').textContent=value==='corporate'?'Cenário neutro, luz suave e expressões moderadas para reuniões.':'Iluminação de estúdio e expressões naturais.';
  $('background').value=value==='corporate'?'corporate':'studio';$('framing').value='bust';
  send('presentation',value);send('background',$('background').value);send('framing','bust');
  if(save)savePreferences();
}
$('poseQuality').onchange=()=>savePreferences();
$('presentation').onchange=()=>presentation($('presentation').value);
$('mirror').onchange=()=>{send('mirror',$('mirror').checked);savePreferences();};
for(const key of ['skin','hair','eyes','outfit'])$(key+'Color').onchange=()=>{const color=$(key+'Color').value;if(color)appearance.colors[key]=color;else delete appearance.colors[key];send('appearance',appearance.colors);savePreferences();};
$('resetColors').onclick=()=>{appearance.colors={};for(const key of ['skin','hair','eyes','outfit'])$(key+'Color').value='';send('appearance',{});savePreferences();};
function applyState(result) {
  latest=result;
  const foreignTracker=result.trackerOwner&&result.trackerOwner!==client,foreignCamera=result.cameraOwner&&result.cameraOwner!==client;
  tracking=result.tracking&&!foreignTracker;virtual=result.virtualCamera&&!foreignCamera;cameraSession=virtual?result.cameraSession:null;
  worker?.postMessage({type:'virtual',value:virtual,session:cameraSession});
  $('track').textContent=foreignTracker?'Webcam em uso em outra aba':tracking?'Parar rastreamento':'Iniciar rastreamento';
  $('track').disabled=pending||!!foreignTracker;$('camera').disabled=$('poseQuality').disabled=!!result.tracking||pending;
  $('virtual').textContent=foreignCamera?'Saída em uso em outra aba':virtual?'Desativar câmera virtual':'Ativar câmera virtual';
  $('virtual').disabled=pending||!!foreignCamera||!ready||result.cameraState==='stopping';
  $('virtualState').textContent=foreignCamera?'Saída ativa em outra aba':virtual?(result.streaming?'OBS Virtual Camera transmitindo':'Aguardando frames do avatar'):'Câmera virtual desligada';
  $('cameraDot').classList.toggle('on',virtual&&result.streaming);
  $('frameState').textContent=virtual?`${result.framesReceived} frames do avatar · 1280 × 720`:'Somente o avatar é enviado';
  $('trackingState').textContent=foreignTracker?'Webcam controlada por outra aba':!tracking?'Webcam desligada':!result.connected?'Aguardando imagem da webcam':`${detected.face?'Rosto detectado':'Sem rosto'} · ${detected.pose?'Corpo detectado':'Corpo fora do quadro'} · ${detected.hands} ${detected.hands===1?'mão':'mãos'}`;
}
async function operation(path,body,message) {
  if(pending)return;pending=true;epoch++;$('track').disabled=$('virtual').disabled=true;
  if(path.startsWith('tracking/'))$('camera').disabled=$('poseQuality').disabled=true;
  if(path==='camera/stop'){virtual=false;worker?.postMessage({type:'virtual',value:false,session:null});}
  try {
    const result=await api(path,body);applyState(result);
    if(path==='tracking/start'){$('demo').checked=false;send('demo',false);}
    status(message);
  }catch(error){status(error.message,true);}finally{pending=false;epoch++;if(latest)applyState(latest);}
}
$('modelFile').addEventListener('change',async event=>{const file=event.target.files[0];if(!file||!worker)return;if(file.size>150*1024*1024){status('O avatar deve ter até 150 MB.',true);return;}const source=await file.arrayBuffer();pendingModel={file};status('Carregando seu modelo VRM…');worker.postMessage({type:'load',source},[source]);event.target.value='';});
$('defaultModel').onclick=()=>{pendingModel={builtin:true};status('Carregando Seed-san…');worker?.postMessage({type:'load',source:new URL('assets/Seed-san.vrm',location.href).href});};
$('framing').onchange=()=>{send('framing',$('framing').value);savePreferences();};$('background').onchange=()=>{send('background',$('background').value);savePreferences();};$('demo').onchange=()=>send('demo',$('demo').checked);
$('calibrate').onclick=()=>{if(!tracking){status('Inicie o rastreamento antes de calibrar.');return;}send('calibrate');status('Posição neutra calibrada.');};
$('track').onclick=async()=>{
  if(!hosted)return operation('tracking/'+(tracking?'stop':'start'),{camera:Number($('camera').value),poseQuality:$('poseQuality').value},tracking?'Rastreamento encerrado. A webcam foi liberada.':'Abrindo a webcam física para rastrear rosto, mãos e corpo…');
  if(tracking){browserTracker.stop();tracking=false;$('track').textContent='Iniciar rastreamento';$('camera').disabled=$('poseQuality').disabled=false;$('trackingState').textContent='Webcam desligada';status('Rastreamento encerrado.');return;}
  $('track').disabled=$('poseQuality').disabled=true;status('Abrindo a webcam e preparando o rastreamento local no navegador…');
  try{await browserTracker.start(Number($('camera').value),$('poseQuality').value);tracking=true;$('track').textContent='Parar rastreamento';$('camera').disabled=$('poseQuality').disabled=true;$('demo').checked=false;send('demo',false);status('Rastreamento ativo. Os frames da webcam são processados no seu navegador.');}
  catch(error){browserTracker.stop();status(error.name==='NotAllowedError'?'Permita o acesso à webcam para iniciar o rastreamento.':error.message,true);}
  finally{$('track').disabled=false;$('poseQuality').disabled=tracking;}
};
$('virtual').onclick=()=>operation('camera/'+(virtual?'stop':'start'),{},virtual?'Câmera virtual desligada.':'Câmera virtual pronta. No Teams, escolha OBS Virtual Camera.');
$('clean').onclick=()=>document.body.classList.add('clean');window.addEventListener('keydown',event=>{if(event.key==='Escape')document.body.classList.remove('clean');});$('avatar').addEventListener('dblclick',()=>document.body.classList.remove('clean'));
async function pollStatus() {
  const current=epoch;
  try {
    const result=await api('status');
    if(current===epoch&&!pending){applyState(result);if(bridgeWasOffline){bridgeWasOffline=false;status('Ponte local reconectada. Ative o rastreamento e a câmera virtual para continuar.');}if(result.trackerError||result.cameraError)status(result.trackerError||result.cameraError,true);}
  }catch(error){if(current===epoch&&!pending){bridgeWasOffline=true;virtual=tracking=false;latest=null;worker?.postMessage({type:'virtual',value:false,session:null});$('virtualState').textContent='Ponte desconectada';$('cameraDot').classList.remove('on');$('track').disabled=false;$('virtual').disabled=true;status(error.name==='TimeoutError'?'A ponte local demorou a responder. Tentando reconectar…':error.message,true);}}
  setTimeout(pollStatus,1000);
}
async function initialize() {
  try {
    if(!$('avatar').transferControlToOffscreen)throw new Error('Use uma versão atual do Chrome ou Edge com suporte a OffscreenCanvas.');
    if(!hosted)await handshake();let savedModel;try{savedModel=await loadGuestModel();}catch{}
    const savedSource=savedModel instanceof Blob&&savedModel.size<=150*1024*1024?await savedModel.arrayBuffer():null;
    worker=new Worker('render-worker.js',{type:'module'});
    worker.onerror=event=>{ready=false;virtual=false;send('virtual',false);if(!hosted){$('virtual').disabled=true;api('camera/stop',{}).catch(()=>{});}else browserTracker.stop();status('Erro na renderização: '+(event.message||'Não foi possível carregar o renderer.'),true);};
    worker.onmessage=({data})=>{
      if(data.type==='loaded'){ready=true;$('modelName').textContent=data.name;$('rigInfo').textContent=`VRM · ${data.expressions} expressões · rig humanoide`;document.querySelector('.credit').textContent=`${data.name} · ${data.authors}`;if(latest)applyState(latest);status(hosted?'Avatar carregado. Inicie o rastreamento pela webcam.':'Avatar carregado. Inicie o rastreamento e depois ative a câmera virtual.');if(pendingModel){const selected=pendingModel;pendingModel=null;(selected.builtin?clearGuestModel():saveGuestModel(selected.file)).then(()=>status(selected.builtin?'Modelo padrão selecionado.':'Avatar importado salvo neste navegador para a próxima visita.')).catch(()=>status('Avatar carregado, mas o navegador não conseguiu guardá-lo para a próxima visita.',true));}}
      else if(data.type==='tracking'){detected=data;if(hosted)$('trackingState').textContent=tracking?`${data.face?'Rosto detectado':'Sem rosto'} · ${data.pose?'Corpo detectado':'Corpo fora do quadro'} · ${data.hands} mãos`:'Webcam desligada';}
      else if(data.type==='cameraError'){virtual=false;send('virtual',false);operation('camera/stop',{},data.error);}
      else if(data.type==='renderError'){ready=false;if(!hosted){$('virtual').disabled=true;operation('camera/stop',{},'Renderização interrompida: '+data.error);}else{browserTracker.stop();status('Renderização interrompida: '+data.error,true);}}
      else if(data.type==='error'){pendingModel=null;status(data.error,true);}
      else if(data.type==='cameraRetrying')status('Falha temporária no envio. Reconectando o vídeo do avatar…');
      else if(data.type==='cameraRecovered')status('Transmissão do avatar recuperada.');
      else if(data.type==='authExpired'){if(!reconnecting)reconnecting=handshake().catch(error=>status(error.message,true)).finally(()=>reconnecting=null);}
    };
    const canvas=$('avatar').transferControlToOffscreen();worker.postMessage({type:'init',token,client,canvas,hosted,source:savedSource},savedSource?[canvas,savedSource]:[canvas]);
    const restored=appearance;presentation(restored.presentation,false);
    $('background').value=restored.background;$('framing').value=restored.framing;$('mirror').checked=restored.mirror;$('poseQuality').value=restored.poseQuality;
    for(const key of ['skin','hair','eyes','outfit'])$(key+'Color').value=restored.colors[key]||'';
    send('background',restored.background);send('framing',restored.framing);send('mirror',restored.mirror);send('appearance',restored.colors);savePreferences();if(!hosted)pollStatus();
  }catch(error){status('Não foi possível iniciar o studio: '+error.message,true);}
}
if(hosted){
  document.querySelector('.brand small').textContent='WEB · NO NAVEGADOR';
  document.querySelector('.call-controls').innerHTML='<span class="eyebrow">SEU AVATAR NAS CHAMADAS</span><h2>Conecte ao Teams.</h2><p>O rastreamento funciona neste navegador. Para criar uma câmera do sistema, use uma das opções abaixo.</p><a class="file-button" href="http://127.0.0.1:8765/" target="_blank" rel="noreferrer">Abrir aplicativo local</a><p>Requer Avatar Studio instalado e aberto no computador. A versão local envia diretamente para OBS Virtual Camera.</p><hr><h2>Usar com OBS</h2><ol><li>Inicie a webcam nesta página.</li><li>Ative Tela limpa e capture esta janela no OBS.</li><li>No OBS, inicie a câmera virtual. No Teams, selecione OBS Virtual Camera.</li></ol><p class="tip">Esc ou duplo clique no avatar volta ao Studio. Esta página não instala um driver de câmera.</p><a href="https://github.com/dingous/salsa" target="_blank" rel="noreferrer">Projeto e instalação ↗</a>';
  document.querySelector('.intro p').textContent='Anime seu avatar VRM com rosto, corpo e mãos, diretamente no navegador.';
}
window.addEventListener('pagehide',()=>{browserTracker.stop();worker?.terminate();if(!hosted)for(const operation of ['tracking/stop','camera/stop'])fetch('/api/'+operation,{method:'POST',keepalive:true,headers:{'X-Avatar-Token':token,'X-Avatar-Client':client,'Content-Type':'application/json'},body:'{}'}).catch(()=>{});});
initialize();
