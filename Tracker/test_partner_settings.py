import io,json,tempfile,unittest
from pathlib import Path
from unittest.mock import patch
import partner_settings as settings
from dingous_listener import wake_command
class AdminSettingsTests(unittest.TestCase):
 def test_unconnected_keeps_local_defaults(self):
  with tempfile.TemporaryDirectory() as folder,patch.object(settings.chat_trade_client,'CONFIG',Path(folder)/'missing.json'):
   self.assertEqual(settings.load()['voiceName'],'Microsoft Daniel')
 def remote(self,payload):
  return patch('partner_settings.urllib.request.urlopen',return_value=io.BytesIO(json.dumps(payload).encode()))
 def test_authenticated_company_policy(self):
  with patch.object(settings.chat_trade_client.CONFIG.__class__,'exists',return_value=True),patch('chat_trade_client.settings',return_value=({},'https://example.test',7,'test-token')),self.remote({'model':'qwen3.5:4b','voiceRate':-2}) as request,patch.dict(settings.os.environ,{},clear=False):
   self.assertEqual(settings.load()['voiceRate'],-2)
   self.assertEqual(request.call_args.args[0].get_header('Authorization'),'Bearer test-token')
   self.assertNotIn('companyId',request.call_args.args[0].full_url)
 def test_disabled_policy_fails_closed(self):
  with patch.object(settings.chat_trade_client.CONFIG.__class__,'exists',return_value=True),patch('chat_trade_client.settings',return_value=({},'https://example.test',7,'token')),self.remote({'enabled':False}):
   with self.assertRaises(RuntimeError):settings.load()
 def test_custom_wake_word(self):
  self.assertEqual(wake_command('Parceiro, minha agenda','Parceiro'),'minha agenda')
  self.assertIsNone(wake_command('Dingous, oi','Parceiro'))
 def test_cloud_model_rejected(self):
  with patch.object(settings.chat_trade_client.CONFIG.__class__,'exists',return_value=True),patch('chat_trade_client.settings',return_value=({},'https://example.test',7,'token')),self.remote({'model':'model-cloud'}):
   with self.assertRaises(ValueError):settings.load()
if __name__=='__main__':unittest.main()
