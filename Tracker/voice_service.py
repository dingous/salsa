"""Owner-scoped web voice session; microphone belongs to requesting tab."""
import json,os,subprocess,sys,threading,time
from pathlib import Path
class VoiceService:
 def __init__(self):self.lock=threading.RLock();self.process=None;self.owner=None;self.state={'active':False,'status':'Microfone desligado'};self.lastSeen=0
 def start(self,owner):
  with self.lock:
   if self.process and self.process.poll() is None:
    if self.owner!=owner:raise RuntimeError('Microfone em uso em outra aba.')
    return self.status(owner)
   target=Path(os.getenv('LOCALAPPDATA',str(Path.home())))/'Dingous/Partner/voice-web.json'
   self.process=subprocess.Popen([sys.executable,'-u',str(Path(__file__).with_name('dingous_listener.py')),'--output',str(target)],stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,encoding='utf-8',errors='replace',env=dict(os.environ,PYTHONIOENCODING='utf-8'),creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0));self.owner=owner;self.state={'active':True,'status':'Abrindo microfone local…'}
   self.lastSeen=time.monotonic();threading.Thread(target=self.read,args=(self.process,),daemon=True).start();threading.Thread(target=self.watch,args=(self.process,),daemon=True).start();return self.status(owner)
 def watch(self,process):
  while process.poll() is None:
   time.sleep(3)
   with self.lock:
    if self.process is not process:return
    if time.monotonic()-self.lastSeen>20:self.stop(self.owner);return
 def read(self,process):
  for line in process.stdout:
   kind,sep,value=line.strip().partition('|')
   with self.lock:
    if self.process is not process:return
    if kind=='ANSWER':
     try:self.state.update(json.loads(Path(value).read_text(encoding='utf-8')))
     except OSError:self.state['status']='Falha na resposta de voz.'
    elif kind=='QUESTION':self.state['question']=value
    elif kind in ('LISTEN','WAKE','VOICE_ERROR'):self.state['status']=value
  with self.lock:
   if self.process is process:self.state['active']=False;self.state['status']='Escuta encerrada. Confira o microfone e o modelo de voz.'
 def status(self,owner):
  with self.lock:
   if self.owner and self.owner!=owner:return {'active':False,'status':'Microfone em uso em outra aba.'}
   self.lastSeen=time.monotonic();return dict(self.state)
 def stop(self,owner=None):
  with self.lock:
   if owner and self.owner and owner!=self.owner:raise RuntimeError('Não é possível encerrar a escuta de outra aba.')
   if self.process and self.process.poll() is None:
    if os.name=='nt':subprocess.run(['taskkill','/PID',str(self.process.pid),'/T','/F'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
    else:self.process.terminate()
    self.process.wait(timeout=5)
   self.process=None;self.owner=None;self.state={'active':False,'status':'Microfone desligado'}
