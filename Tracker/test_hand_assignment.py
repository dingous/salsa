import unittest
from types import SimpleNamespace as P
from hand_assignment import anatomical_hand

class Hands(unittest.TestCase):
    def test_unmirrored_selfie_fallback(self):
        self.assertEqual(anatomical_hand('Left',P(x=.2,y=.3),None),'rightHand')
        self.assertEqual(anatomical_hand('Right',P(x=.2,y=.3),None),'leftHand')
    def test_pose_wrist_overrides_classification(self):
        pose=[P(x=.5,y=.5,visibility=0) for _ in range(33)]
        pose[15]=P(x=.8,y=.4,visibility=.9);pose[16]=P(x=.2,y=.4,visibility=.9)
        self.assertEqual(anatomical_hand('Left',P(x=.79,y=.4),pose),'leftHand')
        self.assertEqual(anatomical_hand('Right',P(x=.21,y=.4),pose),'rightHand')
    def test_low_confidence_pose_is_ignored(self):
        pose=[P(x=.8,y=.4,visibility=.1) for _ in range(33)]
        self.assertEqual(anatomical_hand('Right',P(x=.8,y=.4),pose),'leftHand')
    def test_single_visible_wrist_overrides_wrong_label(self):
        pose=[P(x=.5,y=.5,visibility=0) for _ in range(33)]
        pose[15]=P(x=.8,y=.4,visibility=.9)
        self.assertEqual(anatomical_hand('Left',P(x=.79,y=.4),pose),'leftHand')
        pose[15].visibility=0;pose[16]=P(x=.2,y=.4,visibility=.9)
        self.assertEqual(anatomical_hand('Right',P(x=.21,y=.4),pose),'rightHand')
    def test_distant_visible_wrist_does_not_steal_other_hand(self):
        pose=[P(x=.5,y=.5,visibility=0) for _ in range(33)]
        pose[15]=P(x=.8,y=.4,visibility=.9)
        self.assertEqual(anatomical_hand('Left',P(x=.2,y=.4),pose),'rightHand')

if __name__=='__main__':unittest.main()
