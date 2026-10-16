import json,os,tempfile,unittest
from pathlib import Path
from unittest.mock import patch
import chat_trade_environment as environment
import chat_trade_client as client

class EnvironmentTests(unittest.TestCase):
    def test_development_uses_local_services(self):
        with patch.dict(os.environ,{'DINGOUS_ENVIRONMENT':'development'}):
            self.assertEqual(environment.endpoints(),('http://127.0.0.1:5100','http://localhost:6300'))
    def test_production_default_and_unknown_never_fall_back_local(self):
        for value in ('production','invalid',''):
            with patch.dict(os.environ,{'DINGOUS_ENVIRONMENT':value}):
                self.assertEqual(environment.endpoints(),('https://dingous.com.br','https://dingous.com.br'))
    def test_production_ignores_stale_local_url_and_conversation(self):
        with tempfile.TemporaryDirectory() as directory:
            config=Path(directory)/'config.json';config.write_text(json.dumps({'url':'http://127.0.0.1:5100','companyId':48,'applicationKey':'avatar','conversationId':'local-conversation','sessionEnvironment':'development'}))
            with patch.object(client,'CONFIG',config),patch.dict(os.environ,{'DINGOUS_ENVIRONMENT':'production'}):
                result=client.connection_config();self.assertEqual(result['url'],'https://dingous.com.br');self.assertNotIn('conversationId',result);self.assertEqual(result['companyId'],48)
    def test_local_credential_cannot_be_sent_to_production(self):
        with tempfile.TemporaryDirectory() as directory:
            config=Path(directory)/'config.json';config.write_text(json.dumps({'url':'http://127.0.0.1:5100','companyId':48,'localAiVerified':True}))
            with patch.object(client,'CONFIG',config),patch.dict(os.environ,{'DINGOUS_ENVIRONMENT':'production','DINGOUS_CHATTRADE_TOKEN':'dav_local_test-only'}):
                with self.assertRaisesRegex(RuntimeError,'não pode ser enviada'):client.settings()
if __name__=='__main__':unittest.main()
