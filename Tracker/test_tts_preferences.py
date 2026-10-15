import tempfile,unittest
from pathlib import Path
from unittest.mock import patch
import tts_preferences as tts
import partner_settings

class TtsPreferencesTests(unittest.TestCase):
 def test_selection_persists_and_default_clears_override(self):
  with tempfile.TemporaryDirectory() as folder,patch.object(tts,'FILE',Path(folder)/'tts.json'):
   tts.save('Local Voice',['Local Voice']);self.assertEqual(tts.selected(),'Local Voice')
   tts.save('',['Local Voice']);self.assertEqual(tts.selected(),'')
 def test_unavailable_voice_cannot_overwrite_selection(self):
  with tempfile.TemporaryDirectory() as folder,patch.object(tts,'FILE',Path(folder)/'tts.json'):
   tts.save('Local Voice',['Local Voice'])
   with self.assertRaises(ValueError):tts.save('Unknown',['Local Voice'])
   self.assertEqual(tts.selected(),'Local Voice')
 def test_corrupt_file_uses_admin_default(self):
  with tempfile.TemporaryDirectory() as folder,patch.object(tts,'FILE',Path(folder)/'tts.json'):
   tts.FILE.write_text('invalid');self.assertEqual(tts.selected(),'')
 def test_device_choice_preserves_privacy_policy(self):
  with patch.object(tts,'selected',return_value='Local Voice'),patch.object(partner_settings.subprocess,'run') as runner:
   partner_settings.speak(Path('unused.txt'),dict(partner_settings.DEFAULTS,sharedEnvironmentProtection=True));runner.assert_not_called()
   partner_settings.speak(Path('test.txt'),dict(partner_settings.DEFAULTS,sharedEnvironmentProtection=False))
   self.assertIn('Local Voice',runner.call_args.args[0])
if __name__=='__main__':unittest.main()
