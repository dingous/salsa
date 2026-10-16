import unittest
import numpy as np
from microphone_input import recognition_audio

class MicrophoneProcessingTests(unittest.TestCase):
 def test_silence_and_invalid_samples_stay_finite(self):
  self.assertFalse(recognition_audio(np.zeros(16000)).any())
  self.assertTrue(np.isfinite(recognition_audio(np.array([np.nan,np.inf,-np.inf]))).all())
  self.assertEqual(len(recognition_audio(np.array([]))),0)
 def test_dc_is_removed_and_output_cannot_clip_past_full_scale(self):
  result=recognition_audio(np.full(32000,.2))
  self.assertLess(float(np.max(np.abs(result[-8000:]))),.00001)
  self.assertLessEqual(float(np.max(np.abs(recognition_audio(np.full(1600,1000))))),1)
 def test_quiet_speech_band_is_amplified_with_bounded_gain(self):
  signal=.001*np.sin(2*np.pi*220*np.arange(32000)/16000)
  result=recognition_audio(signal)
  rms=float(np.sqrt(np.mean(result[1600:]**2)))
  self.assertGreater(rms,.005);self.assertLess(rms,.012)
