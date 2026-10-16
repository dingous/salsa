"""Local YOLOX object perception. No face recognition or persistent image history."""
import argparse,base64,datetime,hashlib,json,os,time,uuid,urllib.request,threading,secrets
from pathlib import Path
import cv2
import numpy as np

MODEL=Path(__file__).parent/'models/yolox_nano.onnx'
MODEL_SHA='c789161ed43c8269fcd4e67c67eeeb4e80c622da2eb296a20bc6007bd18a0b7d'

class Detector:
    def __init__(self,path=MODEL):
        # This is a separate perception process; reserve the GPU and other cores for Unity/AI.
        cv2.setNumThreads(1);cv2.ocl.setUseOpenCL(False)
        if not path.exists():raise RuntimeError('Instale o modelo com Tools/setup-avatar-eyes.ps1.')
        if hashlib.sha256(path.read_bytes()).hexdigest()!=MODEL_SHA:raise RuntimeError('Modelo YOLOX inválido.')
        self.net=cv2.dnn.readNetFromONNX(str(path));self.net.setPreferableBackend(cv2.dnn.DNN_BACKEND_OPENCV);self.net.setPreferableTarget(cv2.dnn.DNN_TARGET_CPU)
        grids=[];strides=[]
        for stride in (8,16,32):
            x,y=np.meshgrid(np.arange(416//stride),np.arange(416//stride));grids.append(np.stack((x,y),axis=-1).reshape(-1,2));strides.append(np.full((x.size,1),stride))
        self.grid=np.concatenate(grids);self.stride=np.concatenate(strides)
    def detect(self,image,confidence=.45):
        h,w=image.shape[:2];ratio=min(416/h,416/w);padded=np.full((416,416,3),114,np.uint8);resized=cv2.resize(image,(int(w*ratio),int(h*ratio)));padded[:resized.shape[0],:resized.shape[1]]=resized
        self.net.setInput(padded.transpose(2,0,1)[None].astype(np.float32));pred=self.net.forward()[0].copy()
        pred[:,:2]=(pred[:,:2]+self.grid)*self.stride;pred[:,2:4]=np.exp(np.clip(pred[:,2:4],-20,20))*self.stride
        scores=pred[:,4,None]*pred[:,5:];classes=scores.argmax(axis=1);conf=scores.max(axis=1);selected=np.where(conf>=confidence)[0];boxes=[];cs=[];ss=[]
        for i in selected:
            cx,cy,bw,bh=pred[i,:4]/ratio;x=max(0,float(cx-bw/2));y=max(0,float(cy-bh/2));right=min(w,float(cx+bw/2));bottom=min(h,float(cy+bh/2))
            if right<=x or bottom<=y:continue
            boxes.append([x,y,right-x,bottom-y]);cs.append(int(classes[i]));ss.append(float(conf[i]))
        output=[]
        for cls in set(cs):
            indices=[i for i,c in enumerate(cs) if c==cls]
            keep=cv2.dnn.NMSBoxes([boxes[i] for i in indices],[ss[i] for i in indices],confidence,.45)
            for k in np.asarray(keep).reshape(-1):
                i=indices[int(k)];x,y,bw,bh=boxes[i];output.append(dict(classId=cls,confidence=ss[i],x=x/w,y=y/h,width=bw/w,height=bh/h,trackId=None))
        return sorted(output,key=lambda d:-d['confidence'])[:80]

def event(kind,code,track=None):
    return dict(id=str(uuid.uuid4()),kind=kind,code=code,trackId=track,classId=0 if track else None,observedAt=datetime.datetime.now(datetime.timezone.utc).isoformat())

class CrossingTracker:
    """Short-lived geometry only. Never a visitor identity or missing-frame exit."""
    def __init__(self):self.tracks={};self.next=1;self.policy=None
    def update(self,detections,settings,now):
        policy=(settings['lineAxis'],settings['linePosition'],settings['positiveIsEntry'])
        if policy!=self.policy:self.tracks.clear();self.policy=policy
        self.tracks={i:t for i,t in self.tracks.items() if now-t['seen']<=2.5};used=set();events=[]
        for d in detections:
            if d['classId']!=0:continue
            x=d['x']+d['width']/2;y=d['y']+d['height']/2
            candidates=[(np.hypot(x-t['x'],y-t['y']),i) for i,t in self.tracks.items() if i not in used]
            distance,identity=min(candidates,default=(999,None))
            if distance>.22:identity=None
            if identity is None:
                identity=self.next;self.next+=1;self.tracks[identity]=dict(x=x,y=y,seen=now,count=0,side=0,crossed=-999)
            t=self.tracks[identity];used.add(identity);d['trackId']=identity;t['count']+=1
            value=(x if policy[0]=='vertical' else y)-policy[1];side=1 if value>.035 else -1 if value<-.035 else 0
            if side and t['side'] and side!=t['side'] and t['count']>=3 and now-t['crossed']>2:
                events.append(event('entry' if (side>0)==policy[2] else 'exit','line_crossed',identity));t['crossed']=now
            if side:t['side']=side
            t.update(x=x,y=y,seen=now)
        return events

def preview(image,detections,mask):
    copy=image.copy();h,w=copy.shape[:2]
    if mask:
        for d in detections:
            if d['classId']==0:
                x,y=int(d['x']*w),int(d['y']*h);right=min(w,int((d['x']+d['width'])*w)+1);bottom=min(h,int((d['y']+d['height'])*h)+1);copy[y:bottom,x:right]=24
    scale=min(1,640/max(h,w));copy=cv2.resize(copy,(max(1,int(w*scale)),max(1,int(h*scale))))
    ok,jpeg=cv2.imencode('.jpg',copy,[cv2.IMWRITE_JPEG_QUALITY,55])
    return base64.b64encode(jpeg).decode() if ok and len(jpeg)<=130000 else None

def connection_error_state(error):
    import urllib.error
    if isinstance(error,urllib.error.HTTPError):
        if error.code==401:return 'auth_required'
        if error.code==403:return 'access_denied'
    return 'retrying'

def run():
    from chat_trade_client import settings as connection
    config,url,company,token=connection();app=config.get('applicationKey','')
    import re
    if not re.fullmatch('[a-z0-9][a-z0-9-]{0,119}',app):raise RuntimeError('Defina applicationKey do aplicativo vinculado no chat-trade.json local.')
    folder=Path(os.getenv('LOCALAPPDATA',str(Path.home())))/'Dingous'/'Eyes';folder.mkdir(parents=True,exist_ok=True);identity=folder/'device-id.txt'
    if not identity.exists():identity.write_text(str(uuid.uuid4()),encoding='ascii')
    device=str(uuid.UUID(identity.read_text().strip()));session=str(uuid.uuid4());sequence=0;capture=None;detector=None;tracker=CrossingTracker();pending=[];last_log=0
    # Private local handoff lets on-request vision reuse the camera owner.
    from http.server import BaseHTTPRequestHandler,ThreadingHTTPServer
    local_token=secrets.token_urlsafe(32);token_file=folder/'local-token.txt';buffer={'image':None,'at':0};lock=threading.Lock()
    class Handler(BaseHTTPRequestHandler):
        def log_message(self,*args):pass
        def do_GET(self):
            if self.headers.get('Host')!='127.0.0.1:8768' or not secrets.compare_digest(self.headers.get('Authorization',''),'Bearer '+local_token):self.send_error(403);return
            with lock:image=buffer['image'] if time.monotonic()-buffer['at']<4 else None
            if self.path!='/frame' or image is None:self.send_error(404);return
            ok,jpeg=cv2.imencode('.jpg',image,[cv2.IMWRITE_JPEG_QUALITY,75])
            if not ok:self.send_error(503);return
            data=jpeg.tobytes();self.send_response(200);self.send_header('Content-Type','image/jpeg');self.send_header('Cache-Control','no-store');self.send_header('Content-Length',str(len(data)));self.end_headers();self.wfile.write(data)
    server=ThreadingHTTPServer(('127.0.0.1',8768),Handler);server.daemon_threads=True;token_file.write_text(local_token,encoding='ascii');threading.Thread(target=server.serve_forever,daemon=True).start()
    def request(route,body=None):
        # Reload a renewed login without retaining an expired bearer for the process lifetime.
        latest,latest_url,latest_company,latest_token=connection()
        if (latest_url,latest_company,latest.get('applicationKey',''))!=(url,company,app):raise RuntimeError('connection_scope_changed')
        req=urllib.request.Request(url+'/api/dingous-partner/eyes/'+app+'/'+route,data=json.dumps(body).encode() if body is not None else None,headers={'Authorization':'Bearer '+token,'Content-Type':'application/json'})
        req.add_header('Authorization','Bearer '+latest_token)
        with urllib.request.urlopen(req,timeout=8) as response:return json.loads(response.read(300000)) if response.status!=204 else None
    try:
        while True:
            started=time.monotonic()
            try:
                policy=request('policy/'+device);s=policy['settings'];detections=[];frame=None;latency=0;state='disabled'
                if not s['enabled']:
                    if capture is not None:capture.release();capture=None;tracker=CrossingTracker()
                    with lock:buffer.update(image=None,at=0)
                else:
                    if detector is None:detector=Detector();pending.append(event('processing','model_ready'))
                    index=s['cameraIndex']
                    if capture is not None and current_camera!=index:capture.release();capture=None;tracker=CrossingTracker()
                    if capture is None:
                        capture=cv2.VideoCapture(index,cv2.CAP_DSHOW if os.name=='nt' else cv2.CAP_ANY);current_camera=index
                        capture.set(cv2.CAP_PROP_FRAME_WIDTH,640);capture.set(cv2.CAP_PROP_FRAME_HEIGHT,480)
                        capture.set(cv2.CAP_PROP_FPS,10) # Best effort: some camera drivers ignore FPS requests.
                        if not capture.isOpened():raise RuntimeError('capture_error')
                        pending.append(event('processing','capture_opened'))
                    for _ in range(3):ok,image=capture.read()
                    if not ok:raise RuntimeError('capture_error')
                    # One in-memory frame; JPEG is encoded only for an actual viewer/request.
                    with lock:buffer.update(image=image,at=time.monotonic())
                    tick=time.monotonic();detections=detector.detect(image,s['confidence']);latency=(time.monotonic()-tick)*1000;state='running';pending.extend(tracker.update(detections,s,time.monotonic()))
                    if time.monotonic()-last_log>10:pending.append(event('processing','inference'));last_log=time.monotonic()
                    if s['previewEnabled'] and policy['previewRequested']:frame=preview(image,detections,s['maskPeople'])
                pending=pending[-128:];batch=pending[:16] if s['enabled'] else []
                request('telemetry',dict(deviceId=device,sessionId=session,sequence=sequence,state=state,inferenceMs=latency,detections=detections,events=batch,previewJpegBase64=frame));sequence+=1
                if s['enabled']:del pending[:len(batch)]
                else:pending.clear()
                print(json.dumps(dict(eyes=state,objects=len(detections),inferenceMs=round(latency))),flush=True)
                time.sleep(max(.5,s['intervalMs']/1000-(time.monotonic()-started)))
            except Exception as error:
                if capture is not None:capture.release();capture=None;tracker=CrossingTracker()
                with lock:buffer.update(image=None,at=0)
                try:
                    sequence+=1
                    request('telemetry',dict(deviceId=device,sessionId=session,sequence=sequence,state='error',inferenceMs=0,detections=[],events=[event('error','capture_error')] if str(error)=='capture_error' else []))
                    sequence+=1
                except Exception:pass
                # Do not print URLs, tokens, images or server response bodies.
                if str(error)=='connection_scope_changed':raise
                state=connection_error_state(error)
                print(json.dumps(dict(eyes=state,code=type(error).__name__)),flush=True);time.sleep(5)
    finally:
        if capture is not None:capture.release()
        server.shutdown();server.server_close();token_file.unlink(missing_ok=True)

if __name__=='__main__':
    argparse.ArgumentParser(description=__doc__).parse_args()
    while True:
        try:run()
        except (RuntimeError,ValueError,OSError) as error:
            # Startup has no camera until the authenticated configuration is valid.
            print(json.dumps(dict(eyes='unconfigured',code=type(error).__name__)),flush=True)
            time.sleep(10)
