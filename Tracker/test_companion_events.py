import unittest
from datetime import datetime
from companion_events import greeting,daily_query
class CompanionEventsTests(unittest.TestCase):
 def test_greetings(self):
  for hour,expected in ((9,'Bom dia'),(15,'Boa tarde'),(21,'Boa noite')):self.assertTrue(greeting(datetime(2026,10,14,hour)).startswith(expected))
 def test_no_private_briefing_without_integration(self):
  self.assertIsNone(daily_query({},None,datetime(2026,10,14,9)))
 def test_privacy_and_daily_window(self):
  s=dict(dailyBriefingEnabled=True,sharedEnvironmentProtection=False,backend='chattrade',agendaProvider='google',dailyTime='09:00')
  self.assertIsNotNone(daily_query(s,None,datetime(2026,10,14,9)))
  self.assertIsNone(daily_query(s,'2026-10-14',datetime(2026,10,14,9)))
  self.assertIsNone(daily_query(s,None,datetime(2026,10,14,10)))
  self.assertIsNone(daily_query(dict(s,sharedEnvironmentProtection=True),None,datetime(2026,10,14,9)))
if __name__=='__main__':unittest.main()
