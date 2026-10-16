import io,json,tempfile,unittest
from pathlib import Path
from unittest.mock import patch
import avatar_session

class SessionTests(unittest.TestCase):
    def test_logout_deletes_credential_and_conversation_only(self):
        with tempfile.TemporaryDirectory() as directory:
            key=Path(directory)/'token.dpapi';key.write_bytes(b'encrypted')
            config=Path(directory)/'config.json';config.write_text(json.dumps({'companyId':48,'conversationId':'private','applicationKey':'avatar'}))
            with patch.object(avatar_session.credentials,'FILE',key),patch.object(avatar_session.client,'CONFIG',config):avatar_session.logout();avatar_session.logout()
            self.assertFalse(key.exists());self.assertEqual(json.loads(config.read_text()),{'companyId':48,'applicationKey':'avatar'})
    def test_cached_credential_must_be_validated_by_api(self):
        with patch.object(avatar_session.client,'settings',return_value=({},'http://127.0.0.1:5100',48,'secret')),patch.object(avatar_session.urllib.request,'urlopen',side_effect=OSError()),patch('sys.stdout',new_callable=io.StringIO) as output:
            self.assertFalse(avatar_session.status());self.assertNotIn('secret',output.getvalue())
    def test_server_approved_session_unlocks(self):
        response=io.BytesIO(b'{"enabled":true}')
        with patch.object(avatar_session.client,'settings',return_value=({},'http://127.0.0.1:5100',48,'secret')),patch.object(avatar_session.urllib.request,'urlopen',return_value=response),patch('sys.stdout',new_callable=io.StringIO) as output:
            self.assertTrue(avatar_session.status());self.assertIn('AUTH_OK|',output.getvalue());self.assertNotIn('secret',output.getvalue())
    def test_rejection_distinguishes_expired_session_and_permissions(self):
        from urllib.error import HTTPError
        for code,message in [(401,'Credencial salva recusada'),(403,'não tem acesso'),(503,'API estiver disponível')]:
            with self.subTest(code=code),patch.object(avatar_session.client,'settings',return_value=({},'http://127.0.0.1:5100',48,'secret')),patch.object(avatar_session.urllib.request,'urlopen',side_effect=HTTPError('http://localhost',code,'private-server-detail',{},None)),patch('sys.stdout',new_callable=io.StringIO) as output:
                self.assertFalse(avatar_session.status());self.assertIn(message,output.getvalue());self.assertNotIn('secret',output.getvalue());self.assertNotIn('private-server-detail',output.getvalue())
if __name__=='__main__':unittest.main()
