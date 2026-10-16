"""Browser-authorized company/app chat session, received on a one-use loopback callback."""
import argparse,json,secrets,sys,time,urllib.parse,urllib.request,socket
from http.server import HTTPServer,BaseHTTPRequestHandler
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'Tracker'))
import chat_trade_client as client
from chat_trade_credentials import save

def check_admin(url):
    parsed=urllib.parse.urlsplit(url)
    if parsed.scheme not in ('http','https') or not parsed.hostname or parsed.username or parsed.password:
        raise ValueError('Endereço do admin inválido.')
    try:
        with socket.create_connection((parsed.hostname,parsed.port or (443 if parsed.scheme=='https' else 80)),timeout=5):pass
    except OSError:
        if parsed.hostname in ('127.0.0.1','localhost'):
            raise RuntimeError('Admin local desligado na porta 6300. O ChatTrade precisa estar em execução para concluir o login.') from None
        raise RuntimeError('Não foi possível conectar a dingous.com.br. Verifique a conexão e tente novamente.') from None
    try:
        with urllib.request.urlopen(url.rstrip('/')+'/admin/avatar/status',timeout=8) as response:
            protocol=json.loads(response.read(4096))
        if protocol.get('available') is not True or protocol.get('protocol')!=1:raise ValueError()
    except Exception:
        raise RuntimeError('O admin ainda não disponibilizou a conexão do Avatar. Atualize o servidor ChatTrade e tente entrar novamente.') from None

def main():
    from chat_trade_environment import endpoints
    parser=argparse.ArgumentParser();parser.add_argument('--admin',default=endpoints()[1]);parser.add_argument('--open',action='store_true');args=parser.parse_args()
    check_admin(args.admin)
    config=client.connection_config();state=secrets.token_urlsafe(32);done=False
    class Callback(BaseHTTPRequestHandler):
        def log_message(self,*args):pass
        def do_POST(self):
            nonlocal done
            if self.path!='/callback':self.send_error(404);return
            length=int(self.headers.get('Content-Length','0'))
            if not 0<length<40000:self.send_error(400);return
            form=urllib.parse.parse_qs(self.rfile.read(length).decode())
            if not secrets.compare_digest(form.get('state',[''])[0],state):self.send_error(403);return
            token=form.get('token',[''])[0]
            try:
                import base64
                middle=token.split('.')[1];claims=json.loads(base64.urlsafe_b64decode(middle+'='*(-len(middle)%4)))
                if str(claims.get('company_id'))!=str(config['companyId']):raise ValueError('Company mismatch')
                if 'app:'+config['applicationKey'] not in claims.get('scope','').split():raise ValueError('App mismatch')
                req=urllib.request.Request(config['url'].rstrip('/')+'/api/dingous-partner/settings',headers={'Authorization':'Bearer '+token})
                with urllib.request.urlopen(req,timeout=15) as response:policy=json.loads(response.read(128000))
                if not policy.get('enabled'):raise ValueError('Disabled')
                save(token)
            except Exception:
                self.send_error(502,'A API nao validou a sessao. Nenhuma credencial salva.');return
            done=True;self.send_response(200);self.send_header('Content-Type','text/html; charset=utf-8');self.send_header('Cache-Control','no-store');self.end_headers()
            self.wfile.write('<h1>Avatar conectado</h1><p>Sessão validada pela API e protegida pelo Windows. Pode fechar esta aba.</p>'.encode())
    with HTTPServer(('127.0.0.1',0),Callback) as server:
        server.timeout=1
        query=urllib.parse.urlencode({'callback':f'http://127.0.0.1:{server.server_port}/callback','state':state,'app':config['applicationKey']})
        url=args.admin.rstrip('/')+'/admin/avatar/conectar?'+query
        print(url,flush=True)
        if args.open:
            import webbrowser
            webbrowser.open(url)
        deadline=time.monotonic()+300
        while not done and time.monotonic()<deadline:server.handle_request()
    if not done:raise SystemExit('Pareamento expirou; nenhuma credencial salva.')
    print('AUTHENTICATED: API validated company/application; credential encrypted with Windows DPAPI.',flush=True)
if __name__=='__main__':main()
