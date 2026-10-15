"""Company policy from authenticated ChatTrade Admin; no secrets in the payload."""
import json,os,subprocess,urllib.request
from pathlib import Path
import chat_trade_client
DEFAULTS=dict(enabled=True,backend='ollama',model='qwen3.5:4b',voiceName='Microsoft Daniel',voiceRate=-1,voiceVolume=85,wakeWord='Dingous',memoryEnabled=True,trainingApprovalEnabled=True,instructions='',sharedEnvironmentProtection=True)

def load():
 if not chat_trade_client.CONFIG.exists():return dict(DEFAULTS)
 _,url,_,token=chat_trade_client.settings()
 request=urllib.request.Request(url+'/api/dingous-partner/settings',headers={'Authorization':'Bearer '+token})
 with urllib.request.urlopen(request,timeout=10) as response:data=json.loads(response.read(32000))
 if not isinstance(data,dict):raise ValueError('Configuração administrativa inválida.')
 result=dict(DEFAULTS,**data)
 if not result['enabled']:raise RuntimeError('Parceiro Dingous desabilitado pelo administrador.')
 if result['backend'] not in ('ollama','chattrade'):raise ValueError('Motor inválido.')
 if not isinstance(result['model'],str) or 'cloud' in result['model'].lower() or '://' in result['model']:raise ValueError('Modelo remoto bloqueado.')
 os.environ['DINGOUS_AI_MODEL']=result['model']
 if result.get('agentKeys') and not result.get('sharedEnvironmentProtection',True):
  request=urllib.request.Request(url+'/api/dingous-partner/agents',headers={'Authorization':'Bearer '+token})
  with urllib.request.urlopen(request,timeout=10) as response:result['agents']=json.loads(response.read(256000))
 return result

def speak(path,settings=None):
 settings=load() if settings is None else settings
 if settings.get('sharedEnvironmentProtection',True):return False
 print('SPEECH|START',flush=True)
 try:subprocess.run(['powershell.exe','-NoProfile','-File',str(Path(__file__).with_name('dingous_speak.ps1')),'-TextPath',str(path),'-VoiceName',settings['voiceName'],'-Rate',str(settings['voiceRate']),'-Volume',str(settings['voiceVolume'])],check=True,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
 finally:print('SPEECH|END',flush=True)
 return True
