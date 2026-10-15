import tempfile,unittest,os,wave
from pathlib import Path
from unittest.mock import patch
import partner_settings as voice
class UnityTtsTests(unittest.TestCase):
 def test_audio_is_synthesized_and_waits_for_playback_not_spoken_twice(self):
  with tempfile.TemporaryDirectory() as folder:
   calls=[]
   def synth(command,**kwargs):
    calls.append(command);output=Path(command[command.index('-OutputPath')+1])
    with wave.open(str(output),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(16000);w.writeframes(b'\0\0'*1600)
   def receive(message,**kwargs):
    if str(message).startswith('SPEECH_AUDIO|'):Path(str(message)[13:]).with_suffix('.done').write_text('ok')
   with patch.dict(os.environ,{'DINGOUS_UNITY_SPEECH_ROOT':folder}),patch('partner_settings.subprocess.run',side_effect=synth),patch('builtins.print',side_effect=receive):
    self.assertTrue(voice.speak(Path('phrase.txt'),dict(voice.DEFAULTS,sharedEnvironmentProtection=False)))
   self.assertEqual(len(calls),1);self.assertIn('-OutputPath',calls[0]);self.assertEqual(list(Path(folder).iterdir()),[])
 def test_privacy_prevents_even_wav_generation(self):
  with patch.dict(os.environ,{'DINGOUS_UNITY_SPEECH_ROOT':'unused'}),patch('partner_settings.subprocess.run') as synth:
   self.assertFalse(voice.speak(Path('private.txt'),voice.DEFAULTS));synth.assert_not_called()
if __name__=='__main__':unittest.main()
