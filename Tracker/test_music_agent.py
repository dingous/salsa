import copy,io,json,tempfile,unittest,urllib.error
from pathlib import Path
from datetime import datetime,timedelta,timezone
from unittest.mock import patch
import music_agent as music
import dingous_partner as partner
import partner_settings

def policy():
 tracks=[dict(id=str(i),title='Faixa '+str(i),artist='Artista '+str(i),tags=['jazz' if i%2 else 'rock'],url='https://example.org/'+str(i)) for i in range(1,6)]
 return dict(partner_settings.DEFAULTS,sharedEnvironmentProtection=False,_musicScope='a'*64,agentKeys=['dingous-music'],agents=[{'agentKey':'dingous-music'}],timeZone='America/Sao_Paulo',music=dict(enabled=True,dailySuggestionEnabled=True,dailyTime='09:00',avoidRepeatDays=7,discoveryPercent=0,catalogJson=json.dumps(tracks)))
NOW=datetime(2026,10,15,9)
class MusicTests(unittest.TestCase):
 def setUp(self):self.temp=tempfile.TemporaryDirectory();self.root=Path(self.temp.name);self.settings=policy()
 def tearDown(self):self.temp.cleanup()
 def ask(self,q='Sugira uma música',day=0):return music.response(q,self.settings,False,self.root,NOW+timedelta(days=day))
 def test_same_daily_choice_after_restart(self):
  first=self.ask();second=self.ask();self.assertEqual(first['music']['id'],second['music']['id']);self.assertNotIn('https',first['speechText'])
 def test_artist_preference_routes_without_music_keyword(self):
  self.assertTrue(music.matches('Gosto de Artista 1',self.settings));self.assertFalse(music.matches('Gosto de cozinhar',self.settings))
 def test_no_repetition_within_window(self):
  ids=[self.ask(day=i)['music']['id'] for i in range(5)];self.assertEqual(len(set(ids)),5);self.assertNotIn('music',self.ask(day=5))
 def test_feedback_is_idempotent_and_disliked_song_is_excluded(self):
  first=self.ask();self.ask('Não gostei dessa música');self.ask('Não gostei dessa música')
  self.settings['music']['avoidRepeatDays']=1
  self.assertNotEqual(first['music']['id'],self.ask(day=1)['music']['id'])
  state=json.loads(next(self.root.rglob('state.json')).read_text());self.assertEqual(len(state['feedback']),1)
 def test_positive_and_negative_preferences(self):
  self.ask('Gosto de jazz');self.assertIn('jazz',self.ask()['music']['tags'])
  self.ask('Não gosto de rock');self.assertIn('jazz',self.ask(day=1)['music']['tags'])
 def test_protected_never_reads_or_writes_profile(self):
  with patch('music_agent.state') as store:
   result=music.response('Qual a música do dia?',self.settings,True,self.root);self.assertFalse(result['speechAllowed']);store.assert_not_called()
 def test_bound_context_required(self):
  self.settings.pop('_musicScope');self.assertNotIn('music',self.ask());self.assertFalse(list(self.root.iterdir()))
 def test_user_company_app_context_isolation(self):
  self.ask('Gosto de jazz');self.settings['_musicScope']='b'*64;self.ask()
  files=list(self.root.rglob('state.json'));self.assertEqual(len(files),2);self.assertEqual(json.loads(files[1].read_text())['preferences'],[])
 def test_daily_exactly_once_in_local_time_after_restart(self):
  result=music.daily_response(self.settings,self.root,datetime(2026,10,15,12,tzinfo=timezone.utc));self.assertIsNotNone(result)
  self.assertIsNone(music.daily_response(self.settings,self.root,NOW));self.assertIsNone(music.daily_response(self.settings,self.root,NOW+timedelta(hours=1)))
 def test_manual_choice_prevents_duplicate_proactive_suggestion(self):
  self.ask();self.assertIsNone(music.daily_response(self.settings,self.root,NOW))
 def test_disabled_memory_or_agent_prevents_learning(self):
  self.settings['memoryEnabled']=False;self.ask('Gosto de jazz');self.assertFalse(list(self.root.iterdir()))
 def test_music_does_not_require_ollama_or_chat_trade_chat(self):
  with patch('dingous_partner.load_settings',return_value=self.settings),patch('dingous_partner.urllib.request.urlopen') as request:
   result=partner.answer('Sugira uma música',self.root);self.assertEqual(result['agentKey'],'dingous-music');request.assert_not_called()
 def test_reject_non_https_catalog(self):
  self.settings['music']['catalogJson']='[{"id":"bad","title":"A","artist":"B","tags":["jazz"],"url":"file:///private"}]'
  with self.assertRaises(ValueError):self.ask()
 def test_reset_preserves_repeat_control(self):
  self.ask('Gosto de jazz');first=self.ask();self.ask('Apague meu gosto musical');self.assertEqual(first['music']['id'],self.ask()['music']['id'])
  state=json.loads(next(self.root.rglob('state.json')).read_text());self.assertEqual(state['preferences'],[])
 def test_authenticated_music_scope_is_fetched_separately(self):
  payload={k:v for k,v in self.settings.items() if not k.startswith('_') and k!='agents'}
  replies=[io.BytesIO(json.dumps(payload).encode()),io.BytesIO(json.dumps(self.settings['agents']).encode()),io.BytesIO(json.dumps({'contextKey':'c'*64}).encode())]
  with patch.object(partner_settings.chat_trade_client.CONFIG.__class__,'exists',return_value=True),patch('chat_trade_client.settings',return_value=({'applicationKey':'avatar'},'https://example.test',7,'test-only')),patch('partner_settings.urllib.request.urlopen',side_effect=replies) as request:
   loaded=partner_settings.load();self.assertEqual(loaded['_musicScope'],'c'*64);self.assertTrue(request.call_args.args[0].full_url.endswith('/music-context?app=avatar'))
 def test_unavailable_music_context_does_not_break_other_agent_settings(self):
  payload={k:v for k,v in self.settings.items() if not k.startswith('_') and k!='agents'}
  replies=[io.BytesIO(json.dumps(payload).encode()),io.BytesIO(json.dumps(self.settings['agents']).encode()),urllib.error.HTTPError('https://example.test',403,'Forbidden',{},None)]
  with patch.object(partner_settings.chat_trade_client.CONFIG.__class__,'exists',return_value=True),patch('chat_trade_client.settings',return_value=({'applicationKey':'avatar'},'https://example.test',7,'test-only')),patch('partner_settings.urllib.request.urlopen',side_effect=replies):
   loaded=partner_settings.load();self.assertNotIn('_musicScope',loaded);self.assertIn('voiceName',loaded)
if __name__=='__main__':unittest.main()
