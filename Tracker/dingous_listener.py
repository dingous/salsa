"""Opt-in microphone loop: offline Portuguese ASR, wake name and local speech."""
import argparse,json,queue,time,re,unicodedata,subprocess,socket,os
from collections import deque
from pathlib import Path
import numpy as np
from dingous_partner import answer,ROOT
from local_ai import ensure_local_ai
from partner_settings import load as load_settings,speak as speak_reply
from companion_events import greeting,daily_query
WAKE=re.compile(r'\b(?:dingous|dingus|dingos|dingo)\b',re.I)

class VoiceActivityGate:
 """Adapt to quiet microphone gain without interpreting a silent input as speech."""
 def __init__(self):self.noise=.0003
 def active(self,audio):
  rms=float(np.sqrt(np.mean(audio*audio)))
  threshold=max(.0015,min(.012,self.noise*3))
  active=rms>threshold
  if not active:self.noise=.98*self.noise+.02*rms
  return active

class CommandDeduplicator:
 def __init__(self):self.previous='';self.when=-100
 def accept(self,question,now=None):
  now=time.monotonic() if now is None else now
  key=' '.join(re.findall(r'\w+',question.casefold()))
  if not key:return False
  if key==self.previous and now-self.when<15:return False
  self.previous=key;self.when=now;return True
def wake_command(text,wake_word='Dingous'):
 normalized=''.join(c for c in unicodedata.normalize('NFD',text.lower()) if not unicodedata.combining(c))
 normalized_word=''.join(c for c in unicodedata.normalize('NFD',wake_word.lower()) if not unicodedata.combining(c))
 pattern=WAKE if normalized_word=='dingous' else re.compile(r'\b'+re.escape(normalized_word)+r'\b',re.I)
 match=pattern.search(normalized)
 return None if not match else text[match.end():].strip(' ,.!?:;')
def listen():
 import sounddevice as sd
 from faster_whisper import WhisperModel
 parser=argparse.ArgumentParser();parser.add_argument('--output',required=True);parser.add_argument('--seconds',type=float,default=0);args=parser.parse_args()
 settings=load_settings()
 from microphone_input import capture_settings,recognition_audio
 capture=capture_settings(sd)
 root=Path(__file__).parent/'models/whisper';asr_model=settings.get('asrModel','small')
 if asr_model not in ('base','small'):raise ValueError('Modelo de voz inválido; use base ou small no admin.')
 try:model=WhisperModel(asr_model,device='cpu',compute_type='int8',download_root=str(root),local_files_only=True,cpu_threads=4)
 except Exception as error:raise RuntimeError('Modelo de voz '+asr_model+' não instalado. Execute Tools/setup-local-asr.ps1 para instalar small ou ajuste o modelo no admin.') from error
 packets=queue.Queue(maxsize=200)
 def callback(data,frames,timing,status):
  # Intel microphone arrays can expose silent processed channels alongside live raw channels.
  mono=data[:,capture['channel']]
  pcm=(np.clip(mono,-1,1)*32767).astype(np.int16).tobytes()
  try:packets.put_nowait(pcm)
  except queue.Full:pass
 print('LISTEN|Preparando entrada de áudio...',flush=True)
 greeting_path=Path(args.output).with_suffix('.greeting.txt');greeting_path.parent.mkdir(parents=True,exist_ok=True);greeting_path.write_text(greeting(),encoding='utf-8')
 # Only fixed, non-personal greeting text can use the public voice path.
 try:
  from datetime import date
  greeting_stamp=ROOT/'last-greeting-day.txt';day=date.today().isoformat()
  if not greeting_stamp.exists() or greeting_stamp.read_text(encoding='utf-8')!=day:
   speak_reply(greeting_path,dict(settings,sharedEnvironmentProtection=False))
   greeting_stamp.write_text(day,encoding='utf-8')
 except Exception as error:print('VOICE_ERROR|'+str(error),flush=True)
 last_briefing=None;next_policy=0
 start=time.monotonic();activeUntil=0;blocks=[];silence=0;speech=False;wasActive=False;voice_gate=VoiceActivityGate();commands=CommandDeduplicator();preroll=deque(maxlen=3)
 with sd.InputStream(device=capture['device'],samplerate=16000,blocksize=1600,dtype='float32',channels=capture['channels'],callback=callback) as stream:
  actual_device=sd.query_devices(stream.device,'input')
  device_name=actual_device['name']+' / '+sd.query_hostapis(actual_device['hostapi'])['name']+' / canal '+str(capture['channel']+1)
  try:Path(__file__).with_name('microphone-runtime.json').write_text(json.dumps(dict(pid=os.getpid(),startedAt=time.time(),device=stream.device,channels=capture['channels'],channel=capture['channel'],name=device_name)),encoding='utf-8')
  except OSError:pass
  print('LISTEN|Microfone aberto: '+device_name+'. Diga Dingous e sua pergunta.',flush=True)
  last_signal=time.monotonic();no_signal_warned=False
  while not args.seconds or time.monotonic()-start<args.seconds:
   if time.monotonic()>=next_policy and not speech:
    next_policy=time.monotonic()+30;event=None
    music_event=None
    try:
     settings=load_settings();event=daily_query(settings,last_briefing)
     if not event:
      from music_agent import daily_response
      music_event=daily_response(settings,ROOT)
    except Exception as error:print('VOICE_ERROR|Não foi possível atualizar a configuração administrativa: '+str(error),flush=True)
    if event or music_event:
     if event:last_briefing,briefing_question=event
     stream.stop()
     try:
      result=music_event or answer(briefing_question);path=Path(args.output);path.write_text(json.dumps(result,ensure_ascii=False),encoding='utf-8');print('ANSWER|'+str(path),flush=True)
      if result.get('speechAllowed',False):
       speechPath=path.with_suffix('.txt');speechPath.write_text(result.get('speechText',result['answer']),encoding='utf-8');speak_reply(speechPath,load_settings())
     except Exception as error:print('VOICE_ERROR|'+str(error),flush=True)
     finally:
      while not packets.empty():packets.get_nowait()
      stream.start()
   try:data=packets.get(timeout=3)
   except queue.Empty:
    print('VOICE_ERROR|O microfone não está entregando áudio. Confira a entrada e o mute do dispositivo.',flush=True)
    continue
   audio=np.frombuffer(data,np.int16).astype(np.float32)/32768
   if np.max(np.abs(audio))>.0005:
    last_signal=time.monotonic()
    if no_signal_warned:print('LISTEN|Sinal do microfone recuperado. Diga Dingous e sua pergunta.',flush=True)
    no_signal_warned=False
   elif time.monotonic()-last_signal>8 and not no_signal_warned:
    print('VOICE_ERROR|Microfone sem sinal: '+device_name+'. Confira o mute físico, a tecla de microfone e a entrada selecionada no Windows.',flush=True)
    no_signal_warned=True
   loud=voice_gate.active(audio)
   if not speech and loud:speech=True;wasActive=time.monotonic()<activeUntil;blocks.extend(preroll);preroll.clear()
   if not speech:preroll.append(audio);continue
   blocks.append(audio);silence=0 if loud else silence+1
   if silence<9 and len(blocks)<120:continue
   sample=np.concatenate(blocks);blocks=[];silence=0;speech=False
   if len(sample)<6400:continue
   stream.stop()
   try:
    segments,_=model.transcribe(recognition_audio(sample),language='pt',beam_size=5,initial_prompt='Dingous é o nome do assistente.',vad_filter=True)
    text=' '.join(s.text for s in segments if s.no_speech_prob<.6).strip()
    command=wake_command(text,settings['wakeWord'])
    if command is None and not wasActive:continue
    if command is not None and not command:activeUntil=time.monotonic()+8;print('WAKE|Dingous ativado. Faça sua pergunta.',flush=True);continue
    question=command if command is not None else text
    if not question:continue
    if not commands.accept(question):continue
    print('QUESTION|'+question,flush=True)
    activeUntil=0;print('LISTEN|Microfone pausado enquanto Dingous responde',flush=True);settings=load_settings()
    from music_agent import matches as music_matches
    if settings['backend']=='ollama' and not music_matches(question,settings):ensure_local_ai()
    result=answer(question)
    path=Path(args.output);path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(result,ensure_ascii=False),encoding='utf-8');print('ANSWER|'+str(path),flush=True)
    if result.get('speechAllowed',False):
     speechPath=path.with_suffix('.txt');speechPath.write_text(result.get('speechText',result['answer']),encoding='utf-8')
     speak_reply(speechPath,settings)
   except Exception as error:print('VOICE_ERROR|'+str(error),flush=True)
   finally:
    time.sleep(.6) # Let the speaker/room tail decay before reopening capture.
    preroll.clear()
    while not packets.empty():packets.get_nowait()
    stream.start();print('LISTEN|Microfone ativo: aguardando Dingous',flush=True)
def main():
 with socket.socket(socket.AF_INET,socket.SOCK_DGRAM) as lock:
  try:lock.bind(('127.0.0.1',15061))
  except OSError:raise RuntimeError('Outra janela ou versão do Dingous já está usando a escuta, inclusive se estiver na bandeja. Desative a escuta nessa janela antes de ativar aqui.')
  listen()
if __name__=='__main__':main()
