import unittest,io
from unittest.mock import patch
import chat_trade_pairing as pairing

class PairingTests(unittest.TestCase):
    def test_missing_route_does_not_open_browser_for_404(self):
        with patch.object(pairing.socket,'create_connection'),patch.object(pairing.urllib.request,'urlopen',return_value=io.BytesIO(b'<html>not found</html>')):
            with self.assertRaisesRegex(RuntimeError,'ainda não disponibilizou'):pairing.check_admin('https://dingous.com.br')
    def test_published_protocol_can_continue_pairing(self):
        with patch.object(pairing.socket,'create_connection'),patch.object(pairing.urllib.request,'urlopen',return_value=io.BytesIO(b'{"available":true,"protocol":1}')):
            pairing.check_admin('https://dingous.com.br')
    def test_offline_admin_does_not_open_dead_pairing_url(self):
        with patch.object(pairing.socket,'create_connection',side_effect=ConnectionRefusedError()):
            with self.assertRaisesRegex(RuntimeError,'Admin local desligado'):pairing.check_admin('http://127.0.0.1:6300')
    def test_public_server_error_does_not_recommend_localhost(self):
        with patch.object(pairing.socket,'create_connection',side_effect=TimeoutError()):
            with self.assertRaisesRegex(RuntimeError,'dingous.com.br'):pairing.check_admin('https://dingous.com.br')
if __name__=='__main__':unittest.main()
