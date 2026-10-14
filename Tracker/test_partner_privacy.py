import io,json,tempfile,unittest
from pathlib import Path
from unittest.mock import patch
import dingous_partner as partner
import partner_settings
class PrivacyTests(unittest.TestCase):
 def test_unknown_environment_excludes_saved_personal_data(self):
  with tempfile.TemporaryDirectory() as folder:
   root=Path(folder);(root/'memory.json').write_text('["PRIVATE_SENTINEL"]')
   settings=dict(partner_settings.DEFAULTS,instructions='PRIVATE_INSTRUCTIONS',agentKeys=['secret'],agents=[{'agentKey':'secret','name':'PRIVATE_AGENT'}])
   with patch('dingous_partner.load_settings',return_value=settings),patch('dingous_partner.urllib.request.urlopen',return_value=io.BytesIO(b'{"response":"Ola"}')) as request:
    result=partner.answer('Oi',root);payload=request.call_args.args[0].data.decode()
    self.assertNotIn('PRIVATE_',payload);self.assertFalse(result['speechAllowed']);self.assertIsNone(result['agentKey'])
 def test_shared_mode_does_not_query_chat_trade(self):
  with patch('dingous_partner.load_settings',return_value=dict(partner_settings.DEFAULTS,backend='chattrade')),patch('chat_trade_client.ask') as ask:
   self.assertFalse(partner.answer('Minha agenda')['speechAllowed']);ask.assert_not_called()
 def test_tts_blocked_before_process_start(self):
  with patch('partner_settings.subprocess.run') as run:
   self.assertFalse(partner_settings.speak(Path('unused'),dict(partner_settings.DEFAULTS)));run.assert_not_called()
 def test_missing_privacy_setting_blocks_voice(self):
  with patch('partner_settings.subprocess.run') as run:
   self.assertFalse(partner_settings.speak(Path('unused'),{}));run.assert_not_called()
if __name__=='__main__':unittest.main()
