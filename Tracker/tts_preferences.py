"""Device-local Windows voice choice; never changes company privacy policy."""
import argparse,json,os,subprocess,tempfile
from pathlib import Path
FILE=Path(os.getenv('LOCALAPPDATA',str(Path.home())))/'Dingous'/'Partner'/'tts-windows.json'
def selected():
 try:
  value=json.loads(FILE.read_text(encoding='utf-8')).get('voiceName','')
  return value if isinstance(value,str) and len(value)<=200 else ''
 except (OSError,ValueError,AttributeError):return ''
def voices():
 result=subprocess.run(['powershell.exe','-NoProfile','-File',str(Path(__file__).with_name('dingous_voices.ps1'))],capture_output=True,check=True,encoding='utf-8-sig',creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
 return json.loads(result.stdout)['voices']
def save(name,available):
 if name and name not in available:raise ValueError('Voz local não disponível neste computador.')
 FILE.parent.mkdir(parents=True,exist_ok=True)
 with tempfile.NamedTemporaryFile(mode='w',encoding='utf-8',dir=FILE.parent,delete=False) as f:
  json.dump({'voiceName':name},f,ensure_ascii=False);temporary=Path(f.name)
 temporary.replace(FILE)
def main():
 p=argparse.ArgumentParser();p.add_argument('--select');p.add_argument('--preview',action='store_true');args=p.parse_args();available=voices()
 if args.select is not None:save(args.select,available)
 print('VOICES|'+json.dumps(dict(voices=available,selected=selected()),ensure_ascii=False),flush=True)
 if args.preview:
  with tempfile.TemporaryDirectory() as folder:
   path=Path(folder)/'preview.txt';path.write_text('Olá, eu sou o Dingous. Esta é a voz escolhida para conversar com você.',encoding='utf-8')
   # Only this fixed, non-personal preview may bypass shared-environment speech protection.
   from partner_settings import speak,DEFAULTS
   speak(path,dict(DEFAULTS,sharedEnvironmentProtection=False))
if __name__=='__main__':main()
