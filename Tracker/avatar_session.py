"""Session protocol for the Unity login screen. No secret is written to stdout."""
import argparse,json,urllib.request,urllib.error
import chat_trade_client as client
import chat_trade_credentials as credentials

def logout():
    credentials.FILE.unlink(missing_ok=True)
    if client.CONFIG.exists():
        config=json.loads(client.CONFIG.read_text(encoding='utf-8'));config.pop('conversationId',None)
        client.CONFIG.write_text(json.dumps(config,indent=2),encoding='utf-8')

def status():
    try:
        config,url,company,token=client.settings()
        request=urllib.request.Request(url+'/api/dingous-partner/settings',headers={'Authorization':'Bearer '+token})
        with urllib.request.urlopen(request,timeout=8) as response:policy=json.loads(response.read(128000))
        if not policy.get('enabled'):
            print('AUTH_REQUIRED|Avatar desabilitado para esta empresa. Confira o vínculo no admin ChatTrade.',flush=True);return False
    except urllib.error.HTTPError as error:
        if error.code==401:
            message='Credencial salva recusada ou expirada. Entre novamente com ChatTrade para autorizar este Avatar.'
        elif error.code==403:
            message='A conta não tem acesso ao Avatar nesta empresa/aplicativo. Confira seus vínculos no admin ChatTrade.'
        else:
            message='O ChatTrade não conseguiu validar a sessão agora. Tente novamente quando a API estiver disponível.'
        print('AUTH_REQUIRED|'+message,flush=True);return False
    except (OSError,TimeoutError):
        print('AUTH_REQUIRED|Não foi possível alcançar a API ChatTrade. A sessão não foi validada; confira a conexão e tente novamente.',flush=True);return False
    except Exception:
        print('AUTH_REQUIRED|Entre no ChatTrade para conectar o Avatar.',flush=True);return False
    print('AUTH_OK|Sessão ChatTrade validada.',flush=True);return True

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--login',action='store_true');parser.add_argument('--logout',action='store_true');args=parser.parse_args()
    if args.logout:logout();print('AUTH_LOGGED_OUT|Sessão removida deste computador.',flush=True);return
    if args.login:
        import sys
        from chat_trade_pairing import main as pair
        sys.argv=[sys.argv[0],'--open'];pair()
    status()
if __name__=='__main__':main()
