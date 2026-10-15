"""Company policy from authenticated ChatTrade Admin; no secrets in the payload."""
import json,os,subprocess,urllib.request,urllib.parse,tempfile,time,wave,re
from pathlib import Path
import chat_trade_client
DEFAULTS=dict(enabled=True,backend='ollama',model='qwen3.5:4b',voiceName='Microsoft Daniel',voiceRate=-1,voiceVolume=85,wakeWord='Dingous',memoryEnabled=True,trainingApprovalEnabled=True,instructions='',sharedEnvironmentProtection=True)

def load():
 if not chat_trade_client.CONFIG.exists():return dict(DEFAULTS)
 config,url,_,token=chat_trade_client.settings()
 request=urllib.request.Request(url+'/api/dingous-partner/settings',headers={'Authorization':'Bearer '+token})
 with urllib.request.urlopen(request,timeout=10) as response:data=json.loads(response.read(128000))
 if not isinstance(data,dict):raise ValueError('Configuração administrativa inválida.')
 result=dict(DEFAULTS,**data)
 if not result['enabled']:raise RuntimeError('Parceiro Dingous desabilitado pelo administrador.')
 if result['backend'] not in ('ollama','chattrade'):raise ValueError('Motor inválido.')
 if not isinstance(result['model'],str) or 'cloud' in result['model'].lower() or '://' in result['model']:raise ValueError('Modelo remoto bloqueado.')
 os.environ['DINGOUS_AI_MODEL']=result['model']
 if result.get('agentKeys') and not result.get('sharedEnvironmentProtection',True):
  request=urllib.request.Request(url+'/api/dingous-partner/agents',headers={'Authorization':'Bearer '+token})
  with urllib.request.urlopen(request,timeout=10) as response:result['agents']=json.loads(response.read(256000))
  if result.get('music',{}).get('enabled'):
   app=config.get('applicationKey','')
   if re.fullmatch('[a-z0-9][a-z0-9-]{0,119}',app):
    request=urllib.request.Request(url+'/api/dingous-partner/music-context?app='+urllib.parse.quote(app),headers={'Authorization':'Bearer '+token})
    try:
     with urllib.request.urlopen(request,timeout=10) as response:context=json.loads(response.read(2048))
     if re.fullmatch('[a-f0-9]{64}',context.get('contextKey','')):result['_musicScope']=context['contextKey']
    except OSError:result['_musicScopeError']='Vínculo musical autenticado indisponível.'
 return result

def speak(path,settings=None):
 settings=load() if settings is None else settings
 if settings.get('sharedEnvironmentProtection',True):return False
 from tts_preferences import selected
 settings=dict(settings,voiceName=selected() or settings['voiceName'])
 print('SPEECH|START',flush=True)
 try:
  command=['powershell.exe','-NoProfile','-File',str(Path(__file__).with_name('dingous_speak.ps1')),'-TextPath',str(path),'-VoiceName',settings['voiceName'],'-Rate',str(settings['voiceRate']),'-Volume',str(settings['voiceVolume'])]
  root=os.getenv('DINGOUS_UNITY_SPEECH_ROOT')
  if not root:
   subprocess.run(command,check=True,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
  else:
   folder=Path(root);folder.mkdir(parents=True,exist_ok=True)
   with tempfile.TemporaryDirectory(prefix='speech-',dir=folder) as temporary:
    audio=Path(temporary)/'voice.wav';done=audio.with_suffix('.done')
    subprocess.run(command+['-OutputPath',str(audio)],check=True,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
    with wave.open(str(audio),'rb') as wav:duration=wav.getnframes()/wav.getframerate()
    if duration>300:raise RuntimeError('A fala excede cinco minutos. Use respostas mais curtas.')
    print('SPEECH_AUDIO|'+str(audio),flush=True)
    deadline=time.monotonic()+duration+20
    while not done.exists():
     if time.monotonic()>deadline:raise RuntimeError('O avatar não confirmou a reprodução do áudio.')
     time.sleep(.04)
    status=done.read_text(encoding='utf-8')
    if status=='cancelled':return False
    if status!='ok':raise RuntimeError('O avatar não conseguiu reproduzir o áudio de fala.')
 finally:print('SPEECH|END',flush=True)
 return True
