"""Personal facts stay in the local personal-agent store, never training or speech."""
import datetime,json,re,threading,unicodedata
LOCK=threading.RLock()
def normalize(value):return ''.join(c for c in unicodedata.normalize('NFD',value.lower()) if not unicodedata.combining(c))
def statement(question):
 q=normalize(question)
 explicit=re.match(r'^(?:dingous[, ]+)?(?:lembre(?:-se)?(?: de que)?|guarde(?: esta memoria)?|anote(?: que)?|memorize)\s*[:,-]?\s+',q)
 if explicit:return question[explicit.end():].strip()
 if '?' not in q and re.search(r'\b(?:tenho (?:uma? )?(?:filha|filho)|minha filha se chama|meu filho se chama)\b',q):return question
 return None
def save(value,root):
 if not isinstance(value,str) or not 0<len(value.strip())<=2000:raise ValueError('Memória pessoal inválida.')
 with LOCK:
  root.mkdir(parents=True,exist_ok=True);p=root/'private-memory.json';rows=json.loads(p.read_text(encoding='utf-8')) if p.exists() else []
  if not any(r['text']==value.strip() for r in rows):rows.append(dict(text=value.strip(),recordedAt=datetime.datetime.now(datetime.timezone.utc).isoformat(),classification='personal',speechAllowed=False))
  temp=root/'private-memory.tmp';temp.write_text(json.dumps(rows[-100:],ensure_ascii=False),encoding='utf-8');temp.replace(p)
def recall(question,root):
 p=root/'private-memory.json'
 if not p.exists():return []
 rows=json.loads(p.read_text(encoding='utf-8'));q=normalize(question)
 personal=any(term in q for term in ('filha','filho','familia','sobre mim','memoria pessoal','o que voce lembra','meu nome'))
 words=set(re.findall(r'\b\w{4,}\b',q))-{'voce','qual','como','nome','tenho','minha','meus','para','sobre','chama','anos','apelido'}
 return [r['text'] for r in rows if personal or words.intersection(set(re.findall(r'\b\w{4,}\b',normalize(r['text']))))]
