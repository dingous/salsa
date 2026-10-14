"""Loopback-only fault injection proxy used by the E2E, never by the launcher."""
import argparse
import json
import socket
import threading
import urllib.error
import urllib.request
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

lock = threading.Lock()
failures = 0
injected = 0


def handler(target):
    class Proxy(BaseHTTPRequestHandler):
        def log_message(self, *_): pass
        def do_GET(self): self.forward()
        def do_POST(self): self.forward()
        def forward(self):
            global failures, injected
            body = self.rfile.read(int(self.headers.get('Content-Length', 0))) if self.command=='POST' else None
            if self.path=='/__e2e/fail-frames' and self.command=='POST':
                with lock: failures=int(json.loads(body).get('count', 2))
                data=json.dumps({'remaining':failures,'injected':injected}).encode()
                self.send_response(200);self.send_header('Content-Type','application/json');self.send_header('Content-Length',str(len(data)));self.end_headers();self.wfile.write(data);return
            if self.path=='/__e2e/status':
                with lock: data=json.dumps({'remaining':failures,'injected':injected}).encode()
                self.send_response(200);self.send_header('Content-Type','application/json');self.send_header('Content-Length',str(len(data)));self.end_headers();self.wfile.write(data);return
            with lock:
                fail=self.path=='/api/frame' and failures>0
                if fail: failures-=1;injected+=1
            if fail:
                self.connection.shutdown(socket.SHUT_RDWR);self.connection.close();return
            headers={k:v for k,v in self.headers.items() if k.lower() not in ('host','origin','connection','accept-encoding')}
            headers['Origin']=target
            request=urllib.request.Request(target+self.path,data=body,headers=headers,method=self.command)
            try: response=urllib.request.urlopen(request,timeout=15)
            except urllib.error.HTTPError as error: response=error
            except OSError:
                self.send_error(502,'Local bridge unavailable');return
            with response:
                data=response.read();self.send_response(response.status)
                for key,value in response.headers.items():
                    if key.lower() not in ('connection','content-length','server','date'): self.send_header(key,value)
                self.send_header('Content-Length',str(len(data)));self.end_headers()
                try: self.wfile.write(data)
                except (BrokenPipeError,ConnectionResetError): pass
    return Proxy


if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--port',type=int,default=8767);parser.add_argument('--target',default='http://127.0.0.1:8765');args=parser.parse_args()
    server=ThreadingHTTPServer(('127.0.0.1',args.port),handler(args.target))
    try: server.serve_forever()
    except KeyboardInterrupt: pass
    finally: server.server_close()
