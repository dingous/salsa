import json,os,tempfile,unittest,io
from pathlib import Path
from unittest.mock import patch
from dingous_listener import wake_command
import chat_trade_client as client
from voice_service import VoiceService
class VoiceTests(unittest.TestCase):
 def test_wake_and_command(self):self.assertEqual(wake_command('Dingous, diga olá'),'diga olá')
 def test_no_wake(self):self.assertIsNone(wake_command('como está o tempo'))
 def test_wake_only(self):self.assertEqual(wake_command('Dingous'),'')
 def test_no_silent_chat_trade_fallback(self):
  with tempfile.TemporaryDirectory() as folder,patch.object(client,'CONFIG',Path(folder)/'absent.json'):
   with self.assertRaises(RuntimeError):client.settings()
 def test_cloud_provider_confirmation_required(self):
  with tempfile.TemporaryDirectory() as folder:
   config=Path(folder)/'config.json';config.write_text(json.dumps({'url':'http://localhost:5100','companyId':1}))
   with patch.object(client,'CONFIG',config):
    with self.assertRaises(RuntimeError):client.settings()
 def test_chat_trade_existing_contract(self):
  with tempfile.TemporaryDirectory() as folder:
   config=Path(folder)/'config.json';config.write_text(json.dumps({'url':'http://localhost:5100','companyId':7,'localAiVerified':True}))
   response=io.BytesIO(b'{"assistantMessage":"Ola","conversationId":12}')
   with patch.object(client,'CONFIG',config),patch.dict(os.environ,{'DINGOUS_CHATTRADE_TOKEN':'test-only'}),patch('chat_trade_client.urllib.request.urlopen',return_value=response) as post:
    result=client.ask('Oi');request=post.call_args.args[0];self.assertTrue(request.full_url.endswith('/api/chat/start'));self.assertEqual(json.loads(request.data)['companyId'],7);self.assertEqual(result['answer'],'Ola');self.assertNotIn('role',json.loads(request.data))
 def test_other_tab_cannot_stop(self):
  voice=VoiceService();voice.owner='a'
  with self.assertRaises(RuntimeError):voice.stop('b')
if __name__=='__main__':unittest.main()
