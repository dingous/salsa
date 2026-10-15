"""Authenticated loopback bridge for Android over adb reverse. No audio/video upload."""
import argparse,json,secrets,threading
from http.server import BaseHTTPRequestHandler,ThreadingHTTPServer
from pathlib import Path
from dingous_partner import answer,ROOT
from local_ai import ensure_local_ai
from fitness_agent import matches as fitness_matches
from nutrition_agent import matches as nutrition_matches
from dingous_vision import matches as vision_matches
LOCK=threading.Lock()
def token():
 path=ROOT/'android-pair-token.txt';path.parent.mkdir(parents=True,exist_ok=True)
 if not path.exists():path.write_text(secrets.token_urlsafe(32),encoding='utf-8')
 return path.read_text(encoding='utf-8').strip()
def handler(secret):
 class Handler(BaseHTTPRequestHandler):
  def log_message(self,*_):pass
  def reply(self,status,data):
   body=json.dumps(data,ensure_ascii=False).encode();self.send_response(status);self.send_header('Content-Type','application/json; charset=utf-8');self.send_header('Content-Length',str(len(body)));self.send_header('Cache-Control','no-store');self.end_headers();self.wfile.write(body)
  def authorized(self):return secrets.compare_digest(self.headers.get('Authorization',''),'Bearer '+secret)
  def do_GET(self):
   if not self.authorized():self.reply(401,{'error':'Pareamento necessário.'});return
   self.reply(200,{'app':'DingousAndroidBridge','privacy':'shared','audioUploaded':False})
  def do_POST(self):
   if not self.authorized():self.reply(401,{'error':'Pareamento necessário.'});return
   if self.path!='/ask':self.reply(404,{'error':'Rota inválida.'});return
   try:
    size=int(self.headers.get('Content-Length','0'))
    if not 0<size<=8192:raise ValueError('Requisição inválida.')
    self.connection.settimeout(10);body=json.loads(self.rfile.read(size));question=body.get('question')
    if not isinstance(question,str) or not 0<len(question.strip())<=2000:raise ValueError('Pergunta inválida.')
    if vision_matches(question):raise ValueError('A percepção Android precisa de uma imagem deste aparelho; ela ainda não está conectada. A câmera do computador não será usada como se fosse a do celular.')
    if not LOCK.acquire(blocking=False):self.reply(409,{'error':'Dingous já está respondendo.'});return
    try:
     if not fitness_matches(question) and not nutrition_matches(question):ensure_local_ai()
     result=answer(question,public_environment=True)
    finally:LOCK.release()
    self.reply(200,result)
   except Exception as e:self.reply(400,{'error':str(e)})
 return Handler
def main():
 p=argparse.ArgumentParser();p.add_argument('--port',type=int,default=8767);a=p.parse_args()
 secret=token()
 # MSIX hosts can redirect LocalAppData. Show the physical file location
 # visible in Explorer, rather than a virtual path that only this process sees.
 print('Dingous Android: ponte loopback ativa; código em '+str((ROOT/'android-pair-token.txt').resolve()),flush=True)
 ThreadingHTTPServer(('127.0.0.1',a.port),handler(secret)).serve_forever()
if __name__=='__main__':main()
