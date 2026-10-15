import unittest
from types import SimpleNamespace as P
from hand_assignment import anatomical_hand,assign_hands,hands_together

class Hands(unittest.TestCase):
    def test_image_prayer_contact_ignores_incorrect_depth(self):
        hands=[]
        for x in (.49,.51):
            h=[P(x=x,y=.5,z=3*x) for _ in range(21)];h[0].y=.65;hands.append(h)
        self.assertTrue(hands_together(hands))
        for p in hands[1]:p.x+=.3
        self.assertFalse(hands_together(hands))
        self.assertFalse(hands_together(hands[:1]))
    def test_prayer_hands_never_overwrite_same_side(self):
        pose=[P(x=.5,y=.5,visibility=.9) for _ in range(33)]
        sides=assign_hands(['Left','Right'],[P(x=.49,y=.5),P(x=.495,y=.5)],pose)
        self.assertEqual(set(sides),{'leftHand','rightHand'})
    def test_two_hands_globally_match_pose(self):
        pose=[P(x=.5,y=.5,visibility=.9) for _ in range(33)]
        pose[15].x=.7;pose[16].x=.3
        self.assertEqual(assign_hands(['Left','Left'],[P(x=.69,y=.5),P(x=.31,y=.5)],pose),['leftHand','rightHand'])
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
