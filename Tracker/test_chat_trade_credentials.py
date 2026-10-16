import tempfile,unittest,os
from pathlib import Path
from unittest.mock import patch
import chat_trade_credentials as credentials

@unittest.skipUnless(os.name=='nt','Windows DPAPI required')
class CredentialTests(unittest.TestCase):
 def test_roundtrip_encrypts_without_plaintext_on_disk(self):
  with tempfile.TemporaryDirectory() as d,patch.object(credentials,'FILE',Path(d)/'session.dpapi'):
   token='synthetic-session-fixture-not-a-real-credential'
   credentials.save(token)
   self.assertNotIn(token.encode(),credentials.FILE.read_bytes())
   self.assertEqual(credentials.load(),token)
 def test_missing_and_corrupt_session_do_not_create_credentials(self):
  with tempfile.TemporaryDirectory() as d,patch.object(credentials,'FILE',Path(d)/'session.dpapi'):
   self.assertEqual(credentials.load(),'')
   credentials.FILE.write_bytes(b'corrupt')
   with self.assertRaises(RuntimeError):credentials.load()
