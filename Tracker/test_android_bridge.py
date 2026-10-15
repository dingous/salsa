import io,json,threading,urllib.request,urllib.error,unittest
from http.server import ThreadingHTTPServer
from unittest.mock import patch
import android_bridge
class AndroidBridgeTests(unittest.TestCase):
 def setUp(self):
  self.server=ThreadingHTTPServer(('127.0.0.1',0),android_bridge.handler('test-secret'));self.thread=threading.Thread(target=self.server.serve_forever);self.thread.start();self.url='http://127.0.0.1:'+str(self.server.server_port)
 def tearDown(self):self.server.shutdown();self.thread.join();self.server.server_close()
 def test_unauthenticated_request_rejected(self):
  with self.assertRaises(urllib.error.HTTPError) as error:urllib.request.urlopen(self.url+'/health')
  self.assertEqual(error.exception.code,401)
 def test_health_has_no_credentials(self):
  req=urllib.request.Request(self.url+'/health',headers={'Authorization':'Bearer test-secret'})
  with urllib.request.urlopen(req) as response:data=json.load(response)
  self.assertFalse(data['audioUploaded']);self.assertNotIn('token',data)
 def test_question_forces_shared_environment(self):
  with patch('android_bridge.ensure_local_ai'),patch('android_bridge.answer',return_value={'answer':'Olá','speechAllowed':False}) as ask:
   req=urllib.request.Request(self.url+'/ask',data=json.dumps({'question':'Olá'}).encode(),headers={'Authorization':'Bearer test-secret'})
   with urllib.request.urlopen(req) as response:self.assertFalse(json.load(response)['speechAllowed'])
   self.assertEqual(ask.call_args.kwargs,{'public_environment':True})
 def test_invalid_question_not_sent_to_ai(self):
  with patch('android_bridge.answer') as ask:
   req=urllib.request.Request(self.url+'/ask',data=b'{"question":""}',headers={'Authorization':'Bearer test-secret'})
   with self.assertRaises(urllib.error.HTTPError):urllib.request.urlopen(req)
   ask.assert_not_called()
 def test_android_never_uses_desktop_camera_as_phone(self):
  with patch('android_bridge.answer') as ask:
   req=urllib.request.Request(self.url+'/ask',data=json.dumps({'question':'O que estou fazendo?'}).encode(),headers={'Authorization':'Bearer test-secret'})
   with self.assertRaises(urllib.error.HTTPError) as error:urllib.request.urlopen(req)
   self.assertEqual(error.exception.code,400);ask.assert_not_called()
if __name__=='__main__':unittest.main()
