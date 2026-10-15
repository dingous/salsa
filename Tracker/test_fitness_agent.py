import unittest
from datetime import date
from unittest.mock import patch
import fitness_agent as fitness
import dingous_partner as partner
from partner_settings import DEFAULTS

class FitnessTests(unittest.TestCase):
    def profile(self, **changes):
        return dict(fitness.DEFAULTS, enabled=True, location='home', restrictions='none', **changes)

    def test_week_and_two_nonconsecutive_strength_days(self):
        plan=fitness.weekly_plan(self.profile(), date(2026,10,14))
        self.assertEqual(plan['weekStart'], '2026-10-12')
        self.assertEqual([s['type'] for s in plan['sessions']], ['walk','strength','walk','strength','rest','rest','rest'])
        exercise=plan['sessions'][1]['activities'][0]
        self.assertEqual((exercise['sets'],exercise['reps'],exercise['restSeconds']), (2,8,60))

    def test_missing_health_confirmation_does_not_prescribe(self):
        plan=fitness.weekly_plan(dict(fitness.DEFAULTS,enabled=True,location='home'))
        self.assertFalse(plan['ready']);self.assertNotIn('sessions',plan)

    def test_no_automatic_progression_across_weeks(self):
        a=fitness.weekly_plan(self.profile(),date(2026,10,14))
        b=fitness.weekly_plan(self.profile(),date(2026,11,14))
        self.assertEqual(a['sessions'][1]['activities'],b['sessions'][1]['activities'])

    def test_invalid_days_and_dose_rejected(self):
        for change in (dict(days=[1,1]),dict(days=[0]),dict(sets=99),dict(walkMinutes=-1),dict(days=[])):
            with self.subTest(change=change),self.assertRaises(ValueError):fitness.weekly_plan(self.profile(**change))

    def test_shared_environment_never_loads_profile(self):
        with patch('fitness_agent.weekly_plan') as generate:
            result=fitness.response('meu treino',{'fitness':{'restrictions':'PRIVATE'}},True)
        generate.assert_not_called();self.assertFalse(result['speechAllowed']);self.assertNotIn('PRIVATE',result['answer'])

    def test_fitness_routed_without_llm_or_chat_history(self):
        settings=dict(DEFAULTS,sharedEnvironmentProtection=False,fitness=self.profile())
        with patch('dingous_partner.load_settings',return_value=settings),patch('dingous_partner.urllib.request.urlopen') as llm:
            result=partner.answer('Dingous, qual treino da semana?')
        llm.assert_not_called();self.assertTrue(result['fitnessPlan']['ready']);self.assertEqual(result['agentKey'],'dingous-fitness')

    def test_gym_has_machine_exercises(self):
        profile=self.profile();profile['location']='gym'
        self.assertEqual(fitness.weekly_plan(profile)['sessions'][1]['activities'][0]['name'],'Leg press')

    def test_disabled_agent_and_keyword_accent(self):
        self.assertFalse(fitness.weekly_plan(fitness.DEFAULTS)['ready'])
        self.assertTrue(fitness.matches('Exercício da semana'))

    def test_removed_or_inactive_link_blocks_plan(self):
        for policy in (dict(agentKeys=[]),dict(agentKeys=['dingous-fitness'],agents=[])):
            result=fitness.response('meu treino',dict(policy,fitness=self.profile()),False)
            self.assertFalse(result['fitnessPlan']['ready'])

if __name__=='__main__':unittest.main()
