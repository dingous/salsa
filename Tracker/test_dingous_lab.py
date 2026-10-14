import unittest,json,math,io
from unittest.mock import patch
import dingous_lab as lab
class LaboratoryTests(unittest.TestCase):
 def test_gpu_budget_settings(self):
  response=io.BytesIO(json.dumps({'response':json.dumps(lab.DEMO)}).encode())
  with patch('dingous_lab.urllib.request.urlopen',return_value=response) as call:
   lab.generate('sorria')
   body=json.loads(call.call_args.args[0].data)
   self.assertEqual(body['options']['num_ctx'],2048);self.assertEqual(body['options']['num_predict'],768)
   self.assertFalse(body['think']);self.assertEqual(body['keep_alive'],0)
 def test_demo_frames(self):
  packets=list(lab.frames(lab.DEMO));self.assertEqual(len(packets),540)
  for packet in packets:
   self.assertEqual(len(packet['points']),33);self.assertEqual(len(packet['leftHand']),21);self.assertEqual(len(packet['rightHand']),21);json.dumps(packet,allow_nan=False)
 def test_reject_untrusted_actions(self):
  with self.assertRaises(ValueError):lab.validate({'version':1,'steps':[{'action':'run_shell'}]})
 def test_reject_malformed_action(self):
  with self.assertRaises(ValueError):lab.validate({'version':1,'steps':[{'action':[]}]})
 def test_reject_boolean_version(self):
  with self.assertRaises(ValueError):lab.validate({'version':True,'steps':[{'action':'smile'}]})
 def test_reject_nan(self):
  with self.assertRaises(ValueError):lab.validate({'version':1,'steps':[{'action':'smile','duration':math.nan}]})
 def test_limits(self):
  result=lab.validate({'version':1,'steps':[{'action':'smile','duration':99,'intensity':99}]});self.assertEqual(result['steps'][0]['duration'],5);self.assertEqual(result['steps'][0]['intensity'],1)
 def test_winks_independent(self):
  packet=lab.frame('wink_left',.5,1);self.assertEqual(packet['shapes'],[{'name':'eyeBlinkLeft','value':1}])
if __name__=='__main__':unittest.main()
