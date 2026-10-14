import os,unittest
from unittest.mock import patch
import local_ai
class LocalTests(unittest.TestCase):
 def test_reject_cloud(self):
  with patch.dict(os.environ,{'DINGOUS_AI_MODEL':'qwen:cloud'}):
   with self.assertRaises(ValueError):local_ai.model_name()
 def test_missing_model(self):
  with patch('local_ai.tags',return_value={'models':[]}):
   with self.assertRaises(RuntimeError):local_ai.ensure_local_ai()
 def test_remote_metadata_rejected(self):
  with patch.dict(os.environ,{'DINGOUS_AI_MODEL':'model:local'}),patch('local_ai.tags',return_value={'models':[{'name':'model:local','remote_host':'remote'}]}):
   with self.assertRaises(ValueError):local_ai.ensure_local_ai()
 def test_start_hidden_loopback(self):
  with patch.dict(os.environ,{'DINGOUS_AI_MODEL':'model:local'}),patch('local_ai.tags',side_effect=[OSError(),{'models':[{'name':'model:local'}]}]),patch('local_ai.Path.is_file',return_value=True),patch('local_ai.subprocess.Popen') as start,patch('local_ai.time.sleep'):
   self.assertEqual(local_ai.ensure_local_ai(),'model:local');self.assertEqual(start.call_args.kwargs['env']['OLLAMA_NO_CLOUD'],'1');self.assertEqual(start.call_args.kwargs['env']['OLLAMA_HOST'],'127.0.0.1:11434')
if __name__=='__main__':unittest.main()
