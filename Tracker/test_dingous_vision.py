import io,json,unittest
from unittest.mock import patch
import dingous_vision as vision
class VisionTests(unittest.TestCase):
 def test_visual_intent(self):
  self.assertTrue(vision.matches('Dingous, o que estou fazendo?'))
  self.assertFalse(vision.matches('Qual minha agenda?'))
 def test_model_without_vision_never_captures(self):
  with patch.object(vision,'ensure_local_ai',return_value='local'),patch.object(vision,'capture') as capture,patch.object(vision.urllib.request,'urlopen',return_value=io.BytesIO(b'{"capabilities":["completion"]}')):
   with self.assertRaises(RuntimeError):vision.answer('O que estou fazendo?')
   capture.assert_not_called()
 def test_image_only_sent_to_loopback_without_persistence(self):
  requests=[]
  def response(request,**kwargs):
   requests.append(request)
   return io.BytesIO(json.dumps({'capabilities':['vision']} if len(requests)==1 else {'response':'Vejo uma pessoa com a mão levantada; não posso confirmar um movimento por uma imagem.'}).encode())
  with patch.object(vision,'ensure_local_ai',return_value='qwen3.5:4b'),patch.object(vision,'capture',return_value='test-image'),patch.object(vision.urllib.request,'urlopen',side_effect=response):
   result=vision.answer('O que estou fazendo?')
  self.assertTrue(all(r.full_url.startswith('http://127.0.0.1:11434/') for r in requests))
  self.assertEqual(json.loads(requests[1].data)['images'],['test-image'])
  self.assertFalse(result['vision']['persisted']);self.assertFalse(result['vision']['identityVerified']);self.assertFalse(result['speechAllowed'])
if __name__=='__main__':unittest.main()
