"""Scoped, offline daily music discovery. Explicit feedback, no fine-tuning or playback."""
import hashlib,json,os,re,unicodedata
from contextlib import contextmanager
from datetime import datetime,date,timedelta
from zoneinfo import ZoneInfo
from urllib.parse import urlsplit

def norm(value):return ''.join(c for c in unicodedata.normalize('NFD',value.casefold()) if not unicodedata.combining(c))
def matches(question,settings=None):
 q=norm(question)
 if re.search(r'\b(musica|musicas|musical|cancao|playlist|artista|jazz|mpb|rock|pop|disco|instrumental|blues|gospel|samba|sertanejo|sertaneja|eletronica|funk)\b',q):return True
 if settings and re.search(r'\b(gosto de|prefiro)\b',q):
  try:return any(norm(t['artist'].split(' feat.')[0]) in q for t in catalog(settings))
  except (KeyError,ValueError):pass
 return False

def ready(settings,protected):
 p=settings.get('music') or {}
 return not protected and settings.get('enabled',True) and settings.get('memoryEnabled',True) and p.get('enabled') and 'dingous-music' in settings.get('agentKeys',[]) and any(a.get('agentKey')=='dingous-music' for a in settings.get('agents',[])) and re.fullmatch('[a-f0-9]{64}',settings.get('_musicScope',''))

def catalog(settings):
 rows=json.loads(settings['music'].get('catalogJson','[]'))
 if not isinstance(rows,list) or not 1<=len(rows)<=200:raise ValueError('Catálogo musical inválido.')
 seen=set()
 for t in rows:
  if not isinstance(t,dict) or not re.fullmatch('[a-z0-9-]{1,80}',str(t.get('id',''))) or t['id'] in seen:raise ValueError('Identificador musical inválido.')
  seen.add(t['id'])
  if any(not isinstance(t.get(k),str) or not t[k].strip() or len(t[k])>limit for k,limit in [('title',160),('artist',240),('url',500)]):raise ValueError('Faixa inválida.')
  tags=t.get('tags');u=urlsplit(t['url'])
  if not isinstance(tags,list) or not 1<=len(tags)<=8 or any(not isinstance(s,str) or not s.strip() or len(s)>50 for s in tags) or u.scheme!='https' or not u.hostname or u.username or u.password:raise ValueError('Tags/link musical inválidos.')
 return rows

@contextmanager
def state(root,scope):
 folder=root/'Music'/scope;folder.mkdir(parents=True,exist_ok=True)
 with (folder/'state.lock').open('a+b') as lock:
  lock.seek(0,2)
  if lock.tell()==0:lock.write(b'0');lock.flush()
  lock.seek(0)
  if os.name=='nt':
   import msvcrt
   msvcrt.locking(lock.fileno(),msvcrt.LK_LOCK,1)
  else:
   import fcntl
   fcntl.flock(lock,fcntl.LOCK_EX)
  try:
   path=folder/'state.json';s=json.loads(path.read_text(encoding='utf-8')) if path.exists() else dict(preferences=[],feedback={},history=[])
   s.setdefault('dislikedPreferences',[])
   yield s
   s['history']=s['history'][-400:];s['preferences']=s['preferences'][-100:];s['dislikedPreferences']=s['dislikedPreferences'][-100:]
   temporary=folder/'state.tmp';temporary.write_text(json.dumps(s,ensure_ascii=False),encoding='utf-8');temporary.replace(path)
  finally:
   lock.seek(0)
   if os.name=='nt':msvcrt.locking(lock.fileno(),msvcrt.LK_UNLCK,1)
   else:fcntl.flock(lock,fcntl.LOCK_UN)

def current_time(settings,now=None):
 zone=ZoneInfo(settings.get('timeZone','America/Sao_Paulo'))
 return now.replace(tzinfo=zone) if now and now.tzinfo is None else now.astimezone(zone) if now else datetime.now(zone)

def recommend(s,tracks,settings,day):
 existing=next((h for h in s['history'] if h['day']==day),None)
 if existing:return next((t for t in tracks if t['id']==existing['id']),None)
 cutoff=(date.fromisoformat(day)-timedelta(days=int(settings['music'].get('avoidRepeatDays',7)))).isoformat()
 recent={h['id'] for h in s['history'] if h['day']>cutoff}
 def features(t):return {norm(t['artist'])}|{norm(g) for g in t['tags']}
 options=[t for t in tracks if t['id'] not in recent and s['feedback'].get(t['id'])!=-1 and not any(p in x or x in p for p in s['dislikedPreferences'] for x in features(t))]
 if not options:return None
 known={t['id']:t for t in tracks}
 def score(t):
  f=features(t);value=sum(4 for p in s['preferences'] if any(p in x or x in p for x in f))
  for identity,rating in s['feedback'].items():
   if identity in known:value+=rating*len(f.intersection(features(known[identity])))
  return value
 def tie(t):return hashlib.sha256((day+settings['_musicScope']+t['id']).encode()).hexdigest()
 explore=int(hashlib.sha256((day+settings['_musicScope']).encode()).hexdigest()[:8],16)%100<int(settings['music'].get('discoveryPercent',20))
 chosen=max(options,key=lambda t:(0 if explore else score(t),-sum(h['id']==t['id'] for h in s['history']),tie(t)))
 s['history'].append(dict(day=day,id=chosen['id'],delivered=True));return chosen

def result(question,track):
 spoken=f"Sua sugestão musical de hoje é {track['title']}, de {track['artist']}. Depois, me diga se gostou dessa música."
 return dict(question=question,answer=spoken+'\n'+track['url'],speechText=spoken,model='local-music-feedback',agentKey='dingous-music',fineTuned=False,personalMemoryUsed=True,speechAllowed=True,music=track)

def response(question,settings,protected,root,now=None):
 base=dict(question=question,model='local-music-feedback',agentKey='dingous-music',fineTuned=False,personalMemoryUsed=True,speechAllowed=False)
 if protected:return dict(base,answer='Seu gosto musical e histórico são pessoais. Consulte em ambiente privado; não serão carregados nem anunciados agora.')
 if not ready(settings,protected):return dict(base,answer='Habilite e vincule Dingous Música no admin e conecte o aplicativo com seu usuário autenticado.')
 tracks=catalog(settings);day=current_time(settings,now).date().isoformat();q=norm(question)
 with state(root,settings['_musicScope']) as s:
  if 'apague meu gosto musical' in q or 'esqueca meu gosto musical' in q:
   s.update(preferences=[],dislikedPreferences=[],feedback={})
   return dict(base,answer='Preferências e avaliações musicais removidas. O controle de sugestões recentes foi preservado para evitar repetições.')
  dislike=bool(re.search(r'\bnao gostei\b',q));like=not dislike and bool(re.search(r'\bgostei\b',q))
  if like or dislike:
   today=next((h for h in s['history'] if h['day']==day),None)
   if today is None:return dict(base,answer='Peça primeiro a música do dia para eu saber qual faixa você está avaliando.')
   s['feedback']={k:v for k,v in s['feedback'].items() if any(t['id']==k for t in tracks)};s['feedback'][today['id']]=-1 if dislike else 1
   return dict(base,answer='Avaliação registrada. Vou usá-la nas próximas sugestões, sem presumir outros gostos seus.')
  preference=re.search(r'\b(?:gosto de|prefiro)\s+(.+)',q)
  if preference:
   negative=bool(re.search(r'\bnao gosto de\b',q));key='dislikedPreferences' if negative else 'preferences';opposite='preferences' if negative else 'dislikedPreferences'
   for value in re.split(r',|;|\s+e\s+',preference.group(1)):
    value=value.strip(' .!')[:100]
    if value and value not in s[key]:s[key].append(value)
    if value in s[opposite]:s[opposite].remove(value)
   return dict(base,answer='Preferência musical guardada no seu perfil local. Vou considerá-la nas próximas sugestões.')
  track=recommend(s,tracks,settings,day)
  return result(question,track) if track else dict(base,answer='O catálogo não tem outra faixa disponível dentro da regra sem repetição. Amplie o catálogo no admin; não vou inventar uma música.')

def daily_response(settings,root,now=None):
 if not ready(settings,settings.get('sharedEnvironmentProtection',True)) or not settings['music'].get('dailySuggestionEnabled'):return None
 current=current_time(settings,now)
 try:hour,minute=map(int,settings['music'].get('dailyTime','09:00').split(':'))
 except (ValueError,AttributeError):return None
 if not (0<=hour<24 and 0<=minute<60 and 0<=current.hour*60+current.minute-hour*60-minute<2):return None
 day=current.date().isoformat();tracks=catalog(settings)
 with state(root,settings['_musicScope']) as s:
  if any(h['day']==day and h.get('delivered') for h in s['history']):return None
  track=recommend(s,tracks,settings,day)
  return result('Sugestão musical diária',track) if track else None
