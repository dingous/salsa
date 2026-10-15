import io,json,tempfile,unittest
from pathlib import Path
from unittest.mock import patch
import dingous_partner as partner
class PartnerTests(unittest.TestCase):
 def test_local_model_cannot_confirm_a_reminder_without_tools(self):
  with patch('dingous_partner.load_settings',return_value=dict(partner.load_settings(),backend='ollama')),patch('dingous_partner.urllib.request.urlopen') as llm:
   result=partner.answer('Me lembre de revisar o documento às 18h')
   self.assertEqual(result['model'],'capability-guard')
   self.assertFalse(result['actionExecuted']);self.assertFalse(result['speechAllowed']);llm.assert_not_called()
 def test_memory_deduplicates(self):
  with tempfile.TemporaryDirectory() as folder:
   root=Path(folder);partner.remember('Prefiro português',root);self.assertEqual(partner.remember('Prefiro português',root),1)
 def test_memory_in_prompt(self):
  with tempfile.TemporaryDirectory() as folder:
   root=Path(folder);partner.remember('Prefiro português',root)
   with patch('dingous_partner.load_settings',return_value=dict(partner.load_settings(),sharedEnvironmentProtection=False)),patch('dingous_partner.urllib.request.urlopen',return_value=io.BytesIO(b'{"response":"Ola"}')) as call:
    result=partner.answer('Quem voce e?',root);self.assertFalse(result['fineTuned']);self.assertIn('Prefiro',json.loads(call.call_args.args[0].data)['prompt'])
 def test_approval_is_separate(self):
  with tempfile.TemporaryDirectory() as folder:
   root=Path(folder);path=partner.approve({'question':'Oi','answer':'Ola'},root);row=json.loads(Path(path).read_text());self.assertEqual(row['messages'][1]['role'],'assistant');self.assertFalse((root/'memory.json').exists())
 def test_empty_question_rejected(self):
  with self.assertRaises(ValueError):partner.text('')
if __name__=='__main__':unittest.main()
