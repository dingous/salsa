"""Validate an existing API session, then save it encrypted for this Windows user."""
import sys,json,getpass,base64,urllib.request
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'Tracker'))
import chat_trade_client as client
from chat_trade_credentials import save
config=json.loads(client.CONFIG.read_text(encoding='utf-8'))
token=getpass.getpass('Cole o token de uma sessao API autenticada (entrada oculta): ').strip()
try:
    middle=token.split('.')[1];claims=json.loads(base64.urlsafe_b64decode(middle+'='*(-len(middle)%4)))
except Exception:raise SystemExit('Formato de token API invalido; nada salvo.')
company=claims.get('company_id',claims.get('CompanyId'))
if str(company)!=str(config['companyId']):raise SystemExit('A sessao pertence a outra empresa; nada salvo.')
request=urllib.request.Request(config['url'].rstrip('/')+'/api/dingous-partner/settings',headers={'Authorization':'Bearer '+token})
try:
    with urllib.request.urlopen(request,timeout=10) as response:policy=json.loads(response.read(128000))
    if not policy.get('enabled'):raise ValueError('Parceiro desabilitado no admin.')
except Exception:raise SystemExit('A API nao validou esta sessao para o Avatar; nada salvo.')
save(token);print('Sessao validada e protegida pelo Windows. Reabra o Avatar.')
