"""Local nutrition education, with explicit profile and shared-environment guards."""
import re
import unicodedata

SOURCE = 'https://www.gov.br/saude/pt-br/composicao/saps/promocao-da-saude/guias-alimentares'

def matches(question):
    value = ''.join(c for c in unicodedata.normalize('NFD', question.casefold()) if not unicodedata.combining(c))
    if re.search(r'\b(dieta|alimentacao|alimentar|cardapio|comer|refeicao|refeicoes|nutricao|calorias)\b', value):
        return True
    return bool(re.search(r'\bemagrecer\b', value)) and not re.search(r'\b(treino|treinar|fitness|caminhada|caminhar|exercicio)\b', value)

def response(question, settings, protected):
    result = dict(question=question, model='nutrition-guidance', agentKey='dingous-nutrition',
                  fineTuned=False, speechAllowed=not protected)
    if protected:
        return dict(result, answer='Seu planejamento alimentar é pessoal. Consulte em ambiente privado; os detalhes não serão carregados nem falados no modo compartilhado.')
    p = settings.get('nutrition') or {}
    linked = 'dingous-nutrition' in settings.get('agentKeys', [])
    active = any(a.get('agentKey') == 'dingous-nutrition' for a in settings.get('agents', []))
    if not p.get('enabled') or not linked or not active:
        return dict(result, answer='Habilite e vincule o agente Dingous Alimentação no admin do Parceiro Dingous.')
    if p.get('ageGroup') != 'adult' or p.get('restrictions') != 'none' or p.get('eatingPattern') not in ('omnivore', 'vegetarian', 'vegan'):
        return dict(result, answer='Antes de personalizar, confirme no admin: maioridade, alergias/restrições, condições de saúde e padrão alimentar. Se houver gravidez, amamentação, condição clínica ou histórico de transtorno alimentar, organize o plano com nutricionista. Posso apoiar hábitos gerais, sem dieta restritiva ou metas de calorias.')
    # Educational organization, never a calorie prescription or clinical meal plan.
    protein = {'omnivore': 'feijão, lentilha, ovos, peixe ou frango',
               'vegetarian': 'feijão, lentilha, grão-de-bico ou ovos',
               'vegan': 'feijão, lentilha, grão-de-bico ou tofu'}[p['eatingPattern']]
    days = [
        ('Segunda', 'Planejar refeições e verificar o que já há em casa.'),
        ('Terça', 'Preparar uma base de arroz e leguminosas e higienizar hortaliças.'),
        ('Quarta', 'Revisar fome, saciedade e praticidade, sem compensar refeições.'),
        ('Quinta', 'Variar legumes e frutas disponíveis na estação.'),
        ('Sexta', 'Planejar o fim de semana com flexibilidade.'),
        ('Sábado', 'Comprar alimentos de acordo com o orçamento e evitar desperdício.'),
        ('Domingo', 'Rever o que funcionou e preparar a próxima semana, sem punições.')]
    lines = ['Apoio à organização alimentar para emagrecimento, sem promessa de resultado ou dieta clínica.',
             'Priorize alimentos in natura ou minimamente processados, faça refeições com atenção e respeite fome e saciedade.',
             'Ideias para organizar refeições: fruta e aveia no café da manhã; arroz, verduras e uma opção como '+protein+' no almoço ou jantar. São exemplos opcionais, não porções prescritas.',
             'Lista de compras de referência: frutas, hortaliças, arroz, aveia e fontes alimentares compatíveis com seu padrão. Ajuste às suas preferências e ao orçamento.']
    lines.extend(day+': '+activity for day, activity in days)
    lines.extend(['Não calculo déficit calórico, jejum, suplementos ou metas de peso. Para uma dieta individualizada, procure nutricionista.', 'Referência: '+SOURCE])
    return dict(result, answer='\n'.join(lines), nutritionPlan=dict(ready=True, kind='educational-organization', days=[dict(day=d, activity=a) for d,a in days], source=SOURCE))
