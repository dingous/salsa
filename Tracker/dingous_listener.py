"""Opt-in microphone loop: offline Portuguese ASR, wake name and local speech."""
import argparse,json,queue,time,re,unicodedata,subprocess,socket
from pathlib import Path
import numpy as np
from dingous_partner import answer
from local_ai import ensure_local_ai
from partner_settings import load as load_settings,speak as speak_reply
WAKE=re.compile(r'\b(?:dingous|dingus|dingos|dingo)\b',re.I)
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
 root=Path(__file__).parent/'models/whisper';model=WhisperModel('base',device='cpu',compute_type='int8',download_root=str(root),local_files_only=True,cpu_threads=4)
 packets=queue.Queue(maxsize=200)
 def callback(data,frames,timing,status):
  try:packets.put_nowait(bytes(data))
  except queue.Full:pass
 print('LISTEN|Microfone ativo: diga Dingous e sua pergunta',flush=True)
 start=time.monotonic();activeUntil=0;blocks=[];silence=0;speech=False;wasActive=False
 with sd.RawInputStream(samplerate=16000,blocksize=1600,dtype='int16',channels=1,callback=callback) as stream:
  while not args.seconds or time.monotonic()-start<args.seconds:
   data=packets.get(timeout=3);audio=np.frombuffer(data,np.int16).astype(np.float32)/32768
   loud=float(np.sqrt(np.mean(audio*audio)))>.012
   if not speech and loud:speech=True;wasActive=time.monotonic()<activeUntil
   if not speech:continue
   blocks.append(audio);silence=0 if loud else silence+1
   if silence<9 and len(blocks)<120:continue
   sample=np.concatenate(blocks);blocks=[];silence=0;speech=False
   if len(sample)<6400:continue
   stream.stop()
   try:
    segments,_=model.transcribe(sample,language='pt',beam_size=3,initial_prompt='Dingous é o nome do assistente pessoal.',vad_filter=True)
    text=' '.join(s.text for s in segments if s.no_speech_prob<.6).strip()
    command=wake_command(text,settings['wakeWord'])
    if command is None and not wasActive:continue
    if command is not None and not command:activeUntil=time.monotonic()+8;print('WAKE|Dingous ativado. Faça sua pergunta.',flush=True);continue
    question=command if command is not None else text
    if not question:continue
    activeUntil=0;print('QUESTION|'+question,flush=True);print('LISTEN|Microfone pausado enquanto Dingous responde',flush=True);settings=load_settings()
    if settings['backend']=='ollama':ensure_local_ai()
    result=answer(question)
    path=Path(args.output);path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(result,ensure_ascii=False),encoding='utf-8');print('ANSWER|'+str(path),flush=True)
    speechPath=path.with_suffix('.txt');speechPath.write_text(result['answer'],encoding='utf-8')
    speak_reply(speechPath,settings)
   except Exception as error:print('VOICE_ERROR|'+str(error),flush=True)
   finally:
    while not packets.empty():packets.get_nowait()
    stream.start();print('LISTEN|Microfone ativo: aguardando Dingous',flush=True)
def main():
 with socket.socket(socket.AF_INET,socket.SOCK_DGRAM) as lock:
  try:lock.bind(('127.0.0.1',15061))
  except OSError:raise RuntimeError('Escuta Dingous já ativa em outro aplicativo. Pare a outra escuta primeiro.')
  listen()
if __name__=='__main__':main()
