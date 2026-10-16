"""Local admin lifecycle, invoked only by the person's explicit Unity login click."""
import argparse,hashlib,json,os,shutil,socket,subprocess,time,urllib.request
from pathlib import Path
import chat_trade_client as client

def ready():
    try:
        with urllib.request.urlopen('http://127.0.0.1:6300/admin/avatar/status',timeout=2) as response:
            value=json.loads(response.read(4096))
        return value.get('available') is True and value.get('protocol')==1
    except Exception:return False

def validated_project():
    config=json.loads(client.CONFIG.read_text(encoding='utf-8'))
    project=Path(config.get('adminProjectPath','')).resolve()
    artifact=project/'artifacts/avatar-local-admin-secure'
    dll=artifact/'ChatTrade.Admin.dll';manifest=artifact/'local-login-security.json'
    if not dll.is_file() or not manifest.is_file():raise RuntimeError('A versão local do ChatTrade com login seguro não está preparada.')
    proof=json.loads(manifest.read_text(encoding='utf-8'))
    if proof.get('authentication')!='explicit-user-login' or hashlib.sha256(dll.read_bytes()).hexdigest()!=proof.get('sha256'):
        raise RuntimeError('A versão local mudou. Valide e compile novamente o admin com login seguro.')
    return project,dll

def ensure_admin(start=False):
    if os.getenv('DINGOUS_ENVIRONMENT')!='development':raise RuntimeError('Inicialização local indisponível em produção.')
    if ready():return
    if not start:raise RuntimeError('ChatTrade local ainda não iniciado.')
    project,dll=validated_project()
    # Never stop or replace an unrelated process using the configured port.
    try:
        with socket.create_connection(('127.0.0.1',6300),timeout=1):raise RuntimeError('A porta 6300 está ocupada por um serviço incompatível.')
    except OSError:pass
    dotnet=shutil.which('dotnet')
    if not dotnet:raise RuntimeError('O runtime .NET não foi encontrado neste computador.')
    environment=dict(os.environ,ASPNETCORE_ENVIRONMENT='Development',Api__BaseUrl='http://127.0.0.1:5100',Api__InternalUrl='http://127.0.0.1:5100',Api__BrowserBaseUrl='http://127.0.0.1:5100')
    logs=client.CONFIG.parent/'Logs';logs.mkdir(parents=True,exist_ok=True)
    with (logs/'local-admin.log').open('ab') as log:
        process=subprocess.Popen([dotnet,str(dll),'--urls','http://127.0.0.1:6300'],cwd=project/'src/ChatTrade.Admin',env=environment,stdin=subprocess.DEVNULL,stdout=log,stderr=log,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
    print('LOCAL_STARTING|Iniciando ChatTrade local. Aguarde…',flush=True)
    deadline=time.monotonic()+120
    while time.monotonic()<deadline:
        if ready():return
        if process.poll() is not None:raise RuntimeError('O ChatTrade local encerrou durante a inicialização. Consulte o diagnóstico em Dingous/Logs/local-admin.log.')
        time.sleep(1)
    raise RuntimeError('O ChatTrade local ainda está iniciando. Aguarde e tente entrar novamente.')

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--start',action='store_true');args=parser.parse_args()
    try:ensure_admin(args.start)
    except Exception as error:print('LOCAL_ERROR|'+str(error),flush=True);raise SystemExit(1)
    print('LOCAL_READY|ChatTrade local disponível.',flush=True)
if __name__=='__main__':main()
