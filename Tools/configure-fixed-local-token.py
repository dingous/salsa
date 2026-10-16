"""Provision/reuse a local-only Avatar credential. Never print or put its secret in Git."""
import hashlib,json,secrets,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'Tracker'))
import chat_trade_client as client
from chat_trade_credentials import load,save

def main():
    config=json.loads(client.CONFIG.read_text(encoding='utf-8'))
    if config.get('url','').rstrip('/') not in ('http://127.0.0.1:5100','http://localhost:5100'):
        raise SystemExit('Configuração permitida somente para a API localhost:5100.')
    if not isinstance(config.get('companyId'),int) or config['companyId']<=0 or not config.get('applicationKey'):
        raise SystemExit('Empresa/aplicativo ausentes.')
    token=load()
    if not token.startswith('dav_local_'):token='dav_local_'+secrets.token_urlsafe(48)
    file=client.CONFIG.with_name('avatar-local-device.json')
    previous=json.loads(file.read_text(encoding='utf-8')) if file.exists() else {}
    subject=previous.get('subject') or 'avatar-device:'+secrets.token_hex(16)
    device={'tokenHash':hashlib.sha256(token.encode()).hexdigest(),'companyId':config['companyId'],
            'applicationKey':config['applicationKey'],'subject':subject}
    file.write_text(json.dumps(device,indent=2),encoding='utf-8');save(token)
    print('Credential stored with Windows DPAPI. Server hash configuration: '+str(file))
if __name__=='__main__':main()
