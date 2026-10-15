"""Proactive companion events: fixed greetings and an admin-controlled daily query."""
from datetime import datetime
from zoneinfo import ZoneInfo

def greeting(now=None):
    hour=(now or datetime.now()).hour
    salutation='Bom dia' if hour<12 else 'Boa tarde' if hour<18 else 'Boa noite'
    return salutation+'. Estou por aqui. Diga Dingous quando precisar de mim.'

def daily_query(settings, last_day, now=None):
    if not settings.get('dailyBriefingEnabled') or settings.get('sharedEnvironmentProtection',True):return None
    if settings.get('backend')!='chattrade' or settings.get('agendaProvider','none')=='none':return None
    try:
        current=now or datetime.now(ZoneInfo(settings.get('timeZone','America/Sao_Paulo')))
        target=datetime.strptime(settings.get('dailyTime','09:00'),'%H:%M').time()
    except (ValueError,KeyError):return None
    day=current.date().isoformat()
    # No catch-up announcements hours after opening or repeated polling.
    elapsed=current.hour*60+current.minute-target.hour*60-target.minute
    if last_day==day or not 0<=elapsed<2:return None
    return day, 'Consulte minha agenda real de hoje e as prioridades autorizadas. Informe apenas compromissos confirmados pelas ferramentas; se a integração não estiver disponível, diga isso. Não crie nem altere eventos.'
