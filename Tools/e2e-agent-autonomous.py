"""Windows autonomous checks. Synthetic data only; no production calendar or identity writes."""
import sys,json,os,subprocess,tempfile,threading,wave,urllib.request,urllib.error
from pathlib import Path
from http.server import BaseHTTPRequestHandler,ThreadingHTTPServer
from unittest.mock import patch
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'Tracker'))
import numpy as np,cv2,mediapipe as mp
from faster_whisper import WhisperModel
from dingous_listener import wake_command,CommandDeduplicator
from microphone_input import recognition_audio
import dingous_partner as partner,chat_trade_client as client
from tracker import model
OUT=ROOT/'Validation/agent-autonomous';OUT.mkdir(parents=True,exist_ok=True)
results=[]
def check(name,mode,fn):
 try:fn();r=dict(name=name,mode=mode,passed=True)
 except Exception as e:r=dict(name=name,mode=mode,passed=False,error=type(e).__name__+': '+str(e)[:220])
 results.append(r);print(json.dumps(r),flush=True)
def require(ok,message):
 if not ok:raise AssertionError(message)

asr=WhisperModel('small',device='cpu',compute_type='int8',download_root=str(ROOT/'Tracker/models/whisper'),local_files_only=True,cpu_threads=4)
phrases=[('query','Dingous, veja minha agenda.','capability-guard'),('create','Dingous, cadastre um compromisso na agenda de teste.','capability-guard'),('music','Dingous, sugira uma música para hoje.','local-music-feedback'),('enroll','Dingous, cadastre meu rosto.','access-capability-guard'),('door','Dingous, abra o portão.','access-capability-guard')]
for key,phrase,expected in phrases:
 def voice_case(key=key,phrase=phrase,expected=expected):
  text=OUT/(key+'.txt');audio=OUT/(key+'.wav');text.write_text(phrase,encoding='utf-8')
  subprocess.run(['powershell.exe','-NoProfile','-File',str(ROOT/'Tracker/dingous_speak.ps1'),'-TextPath',str(text),'-VoiceName','Microsoft Daniel','-OutputPath',str(audio)],check=True,timeout=30)
  with wave.open(str(audio)) as w:
   rate=w.getframerate();x=np.frombuffer(w.readframes(w.getnframes()),np.int16).astype(np.float32)/32768
  from scipy.signal import resample_poly
  from math import gcd
  factor=gcd(rate,16000);x=resample_poly(x,16000//factor,rate//factor)
  segments,_=asr.transcribe(recognition_audio(x),language='pt',beam_size=5,vad_filter=True,initial_prompt='Dingous é o nome do assistente.')
  transcript=' '.join(s.text for s in segments if s.no_speech_prob<.6);command=wake_command(transcript)
  require(bool(command),'Wake/command was not recognized from synthesized audio')
  with tempfile.TemporaryDirectory() as d,patch.object(partner,'load_settings',return_value=dict(partner.load_settings(),backend='ollama')):
   reply=partner.answer(command,Path(d))
  require(reply.get('model')==expected,'Command did not reach the expected capability guard')
  if key=='music':require(reply.get('speechAllowed') is False and 'music' not in reply,'Unbound/private music must not disclose preferences')
  else:require(reply.get('actionExecuted') is False,'Unimplemented action must not be confirmed')
 check('voice-'+key,'real Windows TTS + local Whisper + application routing; no physical microphone',voice_case)

def duplicate():
 g=CommandDeduplicator();require(g.accept('veja minha agenda',100),'first');require(not g.accept('Veja minha agenda!',101),'duplicate');require(g.accept('veja minha agenda',116),'retry')
check('duplicate-command','application regression',duplicate)

events=[];calls=[]
class Fixture(BaseHTTPRequestHandler):
 def log_message(self,*args):pass
 def do_GET(self):
  if self.headers.get('Authorization')!='Bearer fixture-only':self.send_error(401);return
  if self.path=='/api/dingous-partner/settings':
   from test_music_agent import policy
   payload=dict(policy(),backend='chattrade')
  elif self.path=='/api/dingous-partner/agents':payload=[dict(agentKey='dingous-music',name='Música de teste',role='music')]
  elif self.path.startswith('/api/dingous-partner/music-context?'):payload=dict(contextKey='a'*64)
  else:self.send_error(404);return
  response=json.dumps(payload).encode();self.send_response(200);self.send_header('Content-Type','application/json');self.end_headers();self.wfile.write(response)
 def do_POST(self):
  body=json.loads(self.rfile.read(int(self.headers['Content-Length'])))
  if self.headers.get('Authorization')!='Bearer fixture-only':self.send_error(401);return
  if body.get('companyId')!=700001:self.send_error(403);return
  calls.append(self.path)
  if 'cadastre' in body['message'].lower():events.append({'id':'fixture-event','title':'Evento sintético'})
  response=json.dumps(dict(assistantMessage='SIMULAÇÃO: '+json.dumps(events),conversationId=900001)).encode()
  self.send_response(200);self.send_header('Content-Type','application/json');self.end_headers();self.wfile.write(response)
server=ThreadingHTTPServer(('127.0.0.1',0),Fixture);threading.Thread(target=server.serve_forever,daemon=True).start()
def contract():
 with tempfile.TemporaryDirectory() as d,patch.object(client,'CONFIG',Path(d)/'connection.json'),patch.dict(os.environ,{'DINGOUS_CHATTRADE_TOKEN':'fixture-only'}):
  client.CONFIG.write_text(json.dumps(dict(url='http://127.0.0.1:'+str(server.server_port),companyId=700001,localAiVerified=True)),encoding='utf-8')
  client.ask('Veja minha agenda de teste');require(not events,'empty calendar')
  client.ask('Cadastre um evento sintético');require(len(events)==1,'fixture creation')
  answer=client.ask('Veja minha agenda de teste');require('fixture-event' in answer['answer'],'fixture read-after-write')
  require(calls==['/api/chat/start','/api/chat/send','/api/chat/send'],'conversation continuity')
  with patch.dict(os.environ,{'DINGOUS_CHATTRADE_TOKEN':'invalid-fixture'}):
   try:client.ask('Cadastre um evento sintético')
   except RuntimeError:pass
   else:raise AssertionError('invalid credential accepted')
  require(len(events)==1,'unauthorized write')
check('agenda-query-create-query-and-auth-failure','isolated HTTP ChatTrade contract simulator; not Google Agenda',contract)
def music_contract():
 with tempfile.TemporaryDirectory() as d,patch.object(client,'CONFIG',Path(d)/'connection.json'),patch.dict(os.environ,{'DINGOUS_CHATTRADE_TOKEN':'fixture-only'}):
  client.CONFIG.write_text(json.dumps(dict(url='http://127.0.0.1:'+str(server.server_port),companyId=700001,applicationKey='avatar-fixture',localAiVerified=True)),encoding='utf-8')
  root=Path(d)/'profile'
  partner.answer('Gosto de jazz',root)
  result=partner.answer('Qual a música do dia?',root)
  require(result.get('agentKey')=='dingous-music' and 'music' in result,'bound music profile did not recommend')
  require('jazz' in result['music']['tags'],'explicit preference was ignored')
  require(partner.answer('Qual a música do dia?',root)['music']['id']==result['music']['id'],'same-day answer changed unexpectedly')
check('music-admin-policy-authenticated-context-preference-and-recommendation','isolated ChatTrade HTTP simulator + real music agent; synthetic catalog',music_contract)
server.shutdown();server.server_close()

def face_presence():
 opts=mp.tasks.vision.FaceLandmarkerOptions(base_options=mp.tasks.BaseOptions(model_asset_buffer=Path(model('face')).read_bytes()),num_faces=2)
 # AI-generated fictional adult, deliberately unrelated to a real enrolled identity.
 image=cv2.cvtColor(cv2.imread(str(ROOT/'Tools/Fixtures/synthetic-face.png')),cv2.COLOR_BGR2RGB)
 with mp.tasks.vision.FaceLandmarker.create_from_options(opts) as face:
  require(len(face.detect(mp.Image(image_format=mp.ImageFormat.SRGB,data=image)).face_landmarks)==1,'synthetic portrait face missing')
  require(not face.detect(mp.Image(image_format=mp.ImageFormat.SRGB,data=np.zeros((480,640,3),np.uint8))).face_landmarks,'empty image falsely contains a face')
check('face-presence-positive-and-empty-frame','real MediaPipe; synthetic portrait; no identity recognition',face_presence)

def live_binding():
 require(client.CONFIG.exists(),'Avatar has no authenticated ChatTrade binding')
 client.settings()
check('live-chatTrade-binding','real installation preflight; no writes',live_binding)
blocked=['Google Agenda read/write E2E: authenticated integration not verified','Face enrollment/identity recognition: production feature absent','Electronic door access: controller, consent/enrollment and authorization integration absent','Distance/noise/real-speaker rejection: requires physical acoustic validation']
report=dict(passed=all(r['passed'] for r in results) and not blocked,results=results,blocked=blocked,productionWrites=False,realFacesEnrolled=0,doorActuated=False)
(OUT/'report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('AUTONOMOUS_FULL_E2E='+str(report['passed']),flush=True)
sys.exit(0 if report['passed'] else 1)
