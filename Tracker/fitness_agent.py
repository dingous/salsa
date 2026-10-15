"""Structured weekly fitness plans; personal profiles never enter shared mode."""
from datetime import date, timedelta
import unicodedata

DEFAULTS = dict(enabled=False, goal='weight-loss', level='light', location='unconfirmed',
                restrictions='unconfirmed', days=[1, 2, 3, 4], walkMinutes=20,
                sets=2, reps=8, restSeconds=60)
DAYS = ('Segunda', 'Terça', 'Quarta', 'Quinta', 'Sexta', 'Sábado', 'Domingo')

def matches(question):
    value = ''.join(c for c in unicodedata.normalize('NFD', question.casefold()) if not unicodedata.combining(c))
    return any(word in value for word in ('fitness', 'treino', 'treinar', 'caminhada', 'caminhar', 'emagrecer', 'exercicio'))

def weekly_plan(profile, today=None):
    p = dict(DEFAULTS, **profile)
    if not p['enabled']:
        return dict(ready=False, message='Habilite o agente Fitness no admin do Parceiro Dingous.')
    days = p['days']
    if not isinstance(days, list) or not days or len(set(days)) != len(days) or any(type(d) is not int or not 1 <= d <= 7 for d in days):
        raise ValueError('Selecione dias únicos de 1 (segunda) a 7 (domingo).')
    for key, minimum, maximum in (('walkMinutes', 10, 60), ('sets', 1, 3), ('reps', 6, 15), ('restSeconds', 45, 120)):
        if type(p[key]) is not int or not minimum <= p[key] <= maximum:
            raise ValueError('Parâmetro fitness inválido: ' + key)
    if p['level'] not in ('beginner', 'light') or p['goal'] != 'weight-loss':
        raise ValueError('Perfil fitness inválido.')
    if p['location'] not in ('home', 'gym') or p['restrictions'] != 'none':
        return dict(ready=False, message='Antes de montar suas séries, confirme no admin: treino em casa ou academia e se há lesões/restrições. Com restrições, use um plano revisado por profissional.')
    today = today or date.today()
    monday = today - timedelta(days=today.weekday())
    # Alternate available days, but never place strength on consecutive dates.
    strength = set()
    for day in sorted(days)[1::2]:
        if not strength or day - max(strength) > 1:
            strength.add(day)
    movements = [('Sentar e levantar de uma cadeira estável', 'Desça com controle e mantenha os pés apoiados.'),
                 ('Flexão na parede', 'Mantenha o corpo alinhado e aproxime o peito da parede.'),
                 ('Ponte de glúteos no solo', 'Suba o quadril sem arquear a lombar.'),
                 ('Elevação de panturrilhas com apoio', 'Use apoio estável e faça o movimento lentamente.')]
    if p['location'] == 'gym':
        movements = [('Leg press', 'Use carga leve e ajuste com o instrutor.'),
                     ('Remada sentada na máquina', 'Puxe com controle, sem balançar o tronco.'),
                     ('Supino na máquina', 'Ajuste o banco com o instrutor e mantenha carga confortável.'),
                     ('Elevação de panturrilhas com apoio', 'Faça o movimento lentamente.')]
    sessions = []
    for offset, name in enumerate(DAYS):
        day = offset + 1
        entry = dict(day=name, date=(monday + timedelta(days=offset)).isoformat(), type='rest', activities=[])
        if day in days:
            entry['type'] = 'strength' if day in strength else 'walk'
            if day in strength:
                entry['warmup'] = '5 minutos de caminhada leve e movimentos confortáveis das articulações.'
                entry['activities'] = [dict(name=n, sets=p['sets'], reps=p['reps'], restSeconds=p['restSeconds'], technique=t) for n, t in movements]
                entry['intensity'] = 'Carga confortável, sem chegar à falha; termine com repetições ainda possíveis.'
            else:
                entry['activities'] = [dict(name='Caminhada', minutes=p['walkMinutes'], technique='Comece e termine devagar; no trecho central, mantenha ritmo em que consiga conversar.')]
            entry['cooldown'] = '3 minutos diminuindo o ritmo.'
        sessions.append(entry)
    return dict(ready=True, weekStart=monday.isoformat(), goal=p['goal'], sessions=sessions,
                progression='Repita a base semanal. Só aumente duração ou carga após avaliar conforto, recuperação e ausência de dor; não há aumento automático.',
                note='Plano inicial de atividade física; emagrecimento também depende de alimentação e outros fatores. Pare diante de dor, tontura ou mal-estar.')

def response(question, settings, protected, today=None):
    if protected:
        return dict(question=question, answer='Seu planejamento fitness é pessoal. Consulte-o em ambiente privado; os detalhes não serão falados nem carregados no modo compartilhado.', model='fitness-planner', agentKey='dingous-fitness', speechAllowed=False, fineTuned=False)
    profile = settings.get('fitness', {})
    if 'agentKeys' in settings and 'dingous-fitness' not in settings['agentKeys']:
        profile = dict(profile, enabled=False)
    if 'agents' in settings and not any(a.get('agentKey') == 'dingous-fitness' for a in settings['agents']):
        profile = dict(profile, enabled=False)
    plan = weekly_plan(profile, today)
    lines = [plan.get('message', 'Plano da semana de ' + plan.get('weekStart', ''))]
    for session in plan.get('sessions', []):
        lines.append(session['day'] + ' (' + session['date'] + '): ' + ('Recuperação, sem treino planejado.' if session['type'] == 'rest' else session.get('warmup', '')))
        for activity in session['activities']:
            dose = f"{activity['minutes']} minutos" if 'minutes' in activity else f"{activity['sets']} séries de {activity['reps']} repetições; descanso {activity['restSeconds']} segundos"
            lines.append(activity['name'] + ': ' + dose + '. ' + activity['technique'])
        if 'cooldown' in session: lines.append(session['cooldown'])
        if 'intensity' in session: lines.append(session['intensity'])
    if plan.get('ready'): lines.extend([plan['progression'], plan['note']])
    return dict(question=question, answer='\n'.join(lines), model='fitness-planner', agentKey='dingous-fitness', speechAllowed=True, fineTuned=False, fitnessPlan=plan)
