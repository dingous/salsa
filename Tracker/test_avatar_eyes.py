import unittest
import cv2,numpy as np
from avatar_eyes import CrossingTracker,Detector,preview,MODEL

def person(x):return dict(classId=0,confidence=.9,x=x-.05,y=.2,width=.1,height=.5,trackId=None)
S=dict(lineAxis='vertical',linePosition=.5,positiveIsEntry=True)
class EyesTests(unittest.TestCase):
 def test_crossing_and_return(self):
  t=CrossingTracker();events=[]
  for n,x in enumerate((.3,.4,.48,.6,.65,.6,.48,.38)):
   events+=t.update([person(x)],S,n*.6)
  self.assertEqual([e['kind'] for e in events],['entry','exit'])
 def test_jitter_and_disappearance_are_not_exit(self):
  t=CrossingTracker();events=[]
  for n,x in enumerate((.49,.51,.48,.52,.495)):events+=t.update([person(x)],S,n*.5)
  events+=t.update([],S,5)
  self.assertEqual(events,[])
 def test_expired_track_and_changed_line_do_not_count(self):
  t=CrossingTracker();t.update([person(.3)],S,0)
  self.assertEqual(t.update([person(.6)],S,4),[])
  self.assertEqual(t.update([person(.7)],dict(S,linePosition=.6),4.5),[])
 def test_non_person_does_not_count(self):
  t=CrossingTracker();d=person(.4);d['classId']=2;t.update([d],S,0)
  self.assertIsNone(d['trackId'])
 def test_mask_does_not_change_original(self):
  import base64
  image=np.full((100,100,3),255,np.uint8);result=preview(image,[person(.5)],True)
  decoded=cv2.imdecode(np.frombuffer(base64.b64decode(result),np.uint8),1)
  self.assertLess(decoded[40,50].mean(),40);self.assertEqual(image[40,50].mean(),255)
 @unittest.skipUnless(MODEL.with_name('yolox-test-dog.jpg').exists(),'Public reference not downloaded')
 def test_real_model_public_reference(self):
  classes={d['classId'] for d in Detector().detect(cv2.imread(str(MODEL.with_name('yolox-test-dog.jpg'))),.45)}
  self.assertTrue({1,2,16}.issubset(classes)) # bicycle, car, dog
if __name__=='__main__':unittest.main()
