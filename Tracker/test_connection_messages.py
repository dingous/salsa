import unittest
from unittest.mock import patch
from urllib.error import HTTPError
from avatar_eyes import connection_error_state
import dingous_partner as partner
from partner_settings import DEFAULTS

class ConnectionMessagesTests(unittest.TestCase):
    def test_eyes_does_not_request_login_for_forbidden(self):
        self.assertEqual(connection_error_state(HTTPError('local',401,'',{},None)), 'auth_required')
        self.assertEqual(connection_error_state(HTTPError('local',403,'',{},None)), 'access_denied')
        self.assertEqual(connection_error_state(OSError()), 'retrying')

    def test_agenda_messages_follow_actual_policy(self):
        for authenticated, protected, provider, expected in [
            (True,False,'none','nenhuma integração'),
            (True,False,'google','respostas locais'),
            (True,True,'google','ambiente compartilhado'),
            (False,False,'none','conecte o Avatar')]:
            settings=dict(DEFAULTS,_authenticated=authenticated,sharedEnvironmentProtection=protected,agendaProvider=provider)
            with self.subTest(expected=expected),patch.object(partner,'load_settings',return_value=settings),patch('private_memory.recall',return_value=[]),patch.object(partner.urllib.request,'urlopen') as network:
                result=partner.answer('quais meus compromissos hoje')
                self.assertIn(expected,result['answer'])
                self.assertFalse(result['actionExecuted'])
                network.assert_not_called()

if __name__=='__main__':unittest.main()
