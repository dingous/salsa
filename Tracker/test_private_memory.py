import io,json,tempfile,unittest
from pathlib import Path
from unittest.mock import patch
import dingous_partner as partner
import partner_settings
from private_memory import save,statement
class PrivateMemoryTests(unittest.TestCase):
 def test_natural_fact_is_saved_without_model_or_echo(self):
  with tempfile.TemporaryDirectory() as folder,patch('dingous_partner.urllib.request.urlopen') as request:
   root=Path(folder);result=partner.answer('Tenho uma filha chamada Pessoa Ficticia',root)
   self.assertFalse(result['speechAllowed']);self.assertNotIn('Ficticia',result['answer']);request.assert_not_called()
   self.assertEqual(json.loads((root/'private-memory.json').read_text())[0]['classification'],'personal')
 def test_shared_environment_cannot_read_fact_or_call_model(self):
  with tempfile.TemporaryDirectory() as folder,patch('dingous_partner.urllib.request.urlopen') as request:
   root=Path(folder);save('Minha filha se chama Pessoa Ficticia',root);result=partner.answer('Qual o nome da minha filha?',root)
   self.assertNotIn('Ficticia',result['answer']);self.assertFalse(result['speechAllowed']);request.assert_not_called()
 def test_private_recall_is_text_only_and_not_trainable(self):
  with tempfile.TemporaryDirectory() as folder:
   root=Path(folder);save('Minha filha se chama Pessoa Ficticia',root)
   with patch('dingous_partner.load_settings',return_value=dict(partner_settings.DEFAULTS,sharedEnvironmentProtection=False)),patch('dingous_partner.urllib.request.urlopen',return_value=io.BytesIO(b'{"response":"Pessoa Ficticia"}')):
    result=partner.answer('Qual o nome da minha filha?',root)
   self.assertFalse(result['speechAllowed']);self.assertTrue(result['personalMemoryUsed'])
   with self.assertRaises(RuntimeError):partner.approve(result,root)
 def test_private_fact_never_sent_to_chat_trade(self):
  with tempfile.TemporaryDirectory() as folder:
   root=Path(folder);save('Minha filha se chama Pessoa Ficticia',root)
   with patch('dingous_partner.load_settings',return_value=dict(partner_settings.DEFAULTS,backend='chattrade',sharedEnvironmentProtection=False)),patch('chat_trade_client.ask') as ask:
    result=partner.answer('Qual o nome da minha filha?',root);ask.assert_not_called();self.assertFalse(result['speechAllowed'])
 def test_explicit_save_and_questions_are_distinct(self):
  self.assertEqual(statement('Guarde: prefiro respostas curtas'),'prefiro respostas curtas')
  self.assertIsNone(statement('Qual o nome da minha filha?'))
if __name__=='__main__':unittest.main()
