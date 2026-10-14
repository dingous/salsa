import unittest
from agent_routing import select
class RoutingTests(unittest.TestCase):
 def settings(self):return {'agentKeys':['agenda','suporte'],'agents':[{'agentKey':'agenda','name':'Organizador','role':'schedule'},{'agentKey':'suporte','name':'Técnico','role':'support'},{'agentKey':'fora','name':'Outro','role':'sales'}]}
 def test_agenda(self):self.assertEqual(select('Minha reunião de hoje',self.settings())['agentKey'],'agenda')
 def test_named(self):self.assertEqual(select('Técnico, explique isso',self.settings())['agentKey'],'suporte')
 def test_unknown_does_not_choose_arbitrary_agent(self):self.assertIsNone(select('bom dia',self.settings()))
 def test_primary(self):self.assertEqual(select('bom dia',dict(self.settings(),primaryAgentKey='suporte'))['agentKey'],'suporte')
 def test_unlinked_agent_excluded(self):self.assertIsNone(select('Outro',self.settings()))
if __name__=='__main__':unittest.main()
