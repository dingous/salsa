"""Route local answers to configured company profiles, without executing tools."""
import unicodedata
def normalized(value):
 return ''.join(c for c in unicodedata.normalize('NFD',value.casefold()) if not unicodedata.combining(c))
def select(question,settings):
 allowed=set(settings.get('agentKeys',[]))
 agents=[a for a in settings.get('agents',[]) if a.get('agentKey') in allowed]
 q=normalized(question)
 for agent in agents:
  if any(normalized(value) in q for value in (agent.get('name',''),agent['agentKey']) if value):return agent
 roles={'schedule':('agenda','reuniao','compromisso'), 'sales':('venda','cliente','orcamento'), 'billing':('fatura','pagamento','cobranca'), 'support':('erro','problema','suporte')}
 for role,keywords in roles.items():
  if any(word in q for word in keywords):
   match=next((a for a in agents if a.get('role')==role),None)
   if match:return match
 return next((a for a in agents if a['agentKey']==settings.get('primaryAgentKey')),None) or (agents[0] if len(agents)==1 else None)
