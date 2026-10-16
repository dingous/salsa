import unittest
from unittest.mock import patch, Mock
import application_agent as agent
import json

class ApplicationAgentTests(unittest.TestCase):
    def settings(self, **changes):
        profile = dict(name='notas', applicationKey='avatar', executable='C:/Windows/notepad.exe', arguments=['arquivo com espaço.txt'])
        profile.update(changes)
        return dict(_launcherAuthorized=True, _launcherApplication='avatar', launcher=dict(enabled=True,profilesJson=json.dumps([profile])))

    def test_explicit_command_only(self):
        self.assertEqual(agent.profile_name('Dingous, abra notas'), 'notas')
        self.assertEqual(agent.profile_name('Dingous, abra o notepad'), 'notepad')
        self.assertEqual(agent.profile_name('abra o bloco de notas'), 'notepad')
        for text in ['explique como abrir notas', 'abra notas && calc', 'abra notas --delete', 'não abra notas']:
            self.assertIsNone(agent.profile_name(text))

    @patch.object(agent.subprocess, 'Popen')
    def test_privacy_and_auth_fail_closed(self, launch):
        self.assertFalse(agent.response('abra notas', self.settings(), True)['actionExecuted'])
        self.assertFalse(agent.response('abra notas', {}, False)['actionExecuted'])
        launch.assert_not_called()

    @patch.object(agent.subprocess, 'Popen')
    def test_wrong_application_and_unknown_profile(self, launch):
        self.assertFalse(agent.response('abra notas', self.settings(applicationKey='other'), False)['actionExecuted'])
        self.assertFalse(agent.response('abra outro', self.settings(), False)['actionExecuted'])
        launch.assert_not_called()

    @patch.object(agent.subprocess, 'Popen')
    def test_invalid_and_failed_process(self, launch):
        for executable in ['C:/Windows/cmd.exe', 'relative.exe', '//server/share/app.exe']:
            self.assertFalse(agent.response('abra notas', self.settings(executable=executable), False)['actionExecuted'])
        launch.assert_not_called()

    def test_windows_argv_preserves_parameters_without_shell(self):
        # Mock only filesystem/platform/process boundaries; no app is opened by tests.
        path = Mock()
        path.is_absolute.return_value = True
        path.is_file.return_value = True
        path.suffix = '.exe'
        path.stem = 'notepad'
        path.parent = 'C:/Windows'
        path.__str__ = Mock(return_value='C:/Windows/notepad.exe')
        with patch.object(agent, 'Path', return_value=path), patch.object(agent.os, 'name', 'nt'), patch.object(agent.subprocess, 'Popen', return_value=Mock(pid=123)) as launch:
            result = agent.response('abra notas', self.settings(), False)
            self.assertTrue(result['actionExecuted'])
            launch.assert_called_once_with(['C:/Windows/notepad.exe', 'arquivo com espaço.txt'], shell=False, cwd='C:/Windows', close_fds=True)
            launch.side_effect = OSError('unavailable')
            self.assertFalse(agent.response('abra notas', self.settings(), False)['actionExecuted'])

if __name__ == '__main__': unittest.main()
