"""Use existing authenticated ChatTrade chat orchestration; no permission changes."""
import json,os,urllib.request,urllib.parse
from pathlib import Path
CONFIG=Path(os.getenv('LOCALAPPDATA',str(Path.home()))) / 'Dingous' / 'chat-trade.json'

def connection_config():
 if not CONFIG.exists():raise RuntimeError('ChatTrade não conectado: configure empresa e aplicativo localmente.')
 config=json.loads(CONFIG.read_text(encoding='utf-8'))
 if os.getenv('DINGOUS_ENVIRONMENT'):
  from chat_trade_environment import endpoints
  config['url']=endpoints()[0]
  # A conversation ID must never cross the development/production boundary.
  if config.get('sessionEnvironment')!=os.getenv('DINGOUS_ENVIRONMENT'):config.pop('conversationId',None)
  config['sessionEnvironment']=os.getenv('DINGOUS_ENVIRONMENT')
 return config
def settings():
 if not CONFIG.exists():raise RuntimeError('ChatTrade não conectado: configure URL, empresa e autenticação localmente.')
 config=connection_config();url=config.get('url','').rstrip('/');parsed=urllib.parse.urlsplit(url)
 if parsed.scheme not in ('http','https') or not parsed.hostname or parsed.username or parsed.password:raise ValueError('URL ChatTrade inválida.')
 if parsed.scheme=='http' and parsed.hostname not in ('127.0.0.1','localhost','::1'):raise ValueError('Use HTTPS fora do computador local.')
 if config.get('localAiVerified') is not True:raise RuntimeError('Confirme o provedor local da empresa antes de usar agentes: localAiVerified deve ser true. Não habilitamos APIs pagas automaticamente.')
 company=config.get('companyId');token=os.getenv('DINGOUS_CHATTRADE_TOKEN')
 if not token:
  from chat_trade_credentials import load
  token=load()
 if not isinstance(company,int) or company<=0:raise RuntimeError('Empresa ChatTrade ausente ou inválida na configuração local.')
 if not token:raise RuntimeError('URL e empresa configuradas. Falta autenticar o Avatar na API ChatTrade; a sessão do navegador não substitui essa credencial.')
 if token.startswith('dav_local_') and parsed.hostname not in ('127.0.0.1','localhost','::1'):raise RuntimeError('A chave fixa local não pode ser enviada ao ambiente de produção. Entre no ChatTrade de produção.')
 return config,url,company,token
def ask(question):
 config,url,company,token=settings();conversation=config.get('conversationId');body={'message':question,'companyId':company,'channel':'web'}
 if conversation:body['conversationId']=conversation
 endpoint='/api/chat/send' if conversation else '/api/chat/start'
 request=urllib.request.Request(url+endpoint,data=json.dumps(body).encode(),headers={'Content-Type':'application/json','Authorization':'Bearer '+token})
 try:
  with urllib.request.urlopen(request,timeout=90) as response:result=json.loads(response.read(2000000))
 except urllib.error.HTTPError as error:raise RuntimeError('ChatTrade recusou a requisição HTTP '+str(error.code)+'. Confira login, empresa e permissões.') from error
 reply=result.get('assistantMessage')
 if not isinstance(reply,str) or not reply.strip():raise RuntimeError('ChatTrade não retornou uma resposta textual compatível.')
 if result.get('conversationId'):config['conversationId']=result['conversationId'];CONFIG.write_text(json.dumps(config,indent=2),encoding='utf-8')
 return {'question':question,'answer':reply,'model':'ChatTrade orchestration','fineTuned':False,'conversationId':result.get('conversationId')}
