import hashlib,json,os,tempfile,unittest
from pathlib import Path
from unittest.mock import patch,MagicMock
import local_chattrade_admin as admin

class LocalAdminTests(unittest.TestCase):
    def test_production_never_starts_local_service(self):
        with patch.dict(os.environ,{'DINGOUS_ENVIRONMENT':'production'}),patch.object(admin.subprocess,'Popen') as launch:
            with self.assertRaisesRegex(RuntimeError,'produção'):admin.ensure_admin(True)
            launch.assert_not_called()
    def test_read_only_check_never_starts_service(self):
        with patch.dict(os.environ,{'DINGOUS_ENVIRONMENT':'development'}),patch.object(admin,'ready',return_value=False),patch.object(admin.subprocess,'Popen') as launch:
            with self.assertRaisesRegex(RuntimeError,'não iniciado'):admin.ensure_admin(False)
            launch.assert_not_called()
    def test_ready_admin_is_reused(self):
        with patch.dict(os.environ,{'DINGOUS_ENVIRONMENT':'development'}),patch.object(admin,'ready',return_value=True),patch.object(admin.subprocess,'Popen') as launch:
            admin.ensure_admin(True);launch.assert_not_called()
    def test_changed_binary_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            project=Path(directory);artifact=project/'artifacts/avatar-local-admin-secure';artifact.mkdir(parents=True)
            (artifact/'ChatTrade.Admin.dll').write_bytes(b'changed')
            (artifact/'local-login-security.json').write_text(json.dumps({'authentication':'explicit-user-login','sha256':hashlib.sha256(b'original').hexdigest()}))
            config=project/'config.json';config.write_text(json.dumps({'adminProjectPath':directory}))
            with patch.object(admin.client,'CONFIG',config):
                with self.assertRaisesRegex(RuntimeError,'mudou'):admin.validated_project()
    def test_explicit_start_uses_safe_artifact_and_loopback(self):
        with tempfile.TemporaryDirectory() as directory:
            project=Path(directory);dll=project/'ChatTrade.Admin.dll';dll.write_bytes(b'fixture')
            with patch.dict(os.environ,{'DINGOUS_ENVIRONMENT':'development'}),patch.object(admin.client,'CONFIG',project/'config.json'),patch.object(admin,'validated_project',return_value=(project,dll)),patch.object(admin,'ready',side_effect=[False,True]),patch.object(admin.socket,'create_connection',side_effect=ConnectionRefusedError()),patch.object(admin.shutil,'which',return_value='dotnet.exe'),patch.object(admin.subprocess,'Popen',return_value=MagicMock()) as launch:
                admin.ensure_admin(True)
                args=launch.call_args.args[0];self.assertEqual(args[-1],'http://127.0.0.1:6300');self.assertNotIn('shell',launch.call_args.kwargs)
if __name__=='__main__':unittest.main()
