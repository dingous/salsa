"""Play fixed TTS through the default Windows output and recognize the physical mic in RAM."""
import sys,os,queue,wave,subprocess,time,json,tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'Tracker'))
import numpy as np,sounddevice as sd
from faster_whisper import WhisperModel
from microphone_input import capture_settings,recognition_audio
from dingous_listener import wake_command
from dingous_partner import answer
OUT=ROOT/'Validation/agent-autonomous';capture=capture_settings(sd)
model=WhisperModel('small',device='cpu',compute_type='int8',download_root=str(ROOT/'Tracker/models/whisper'),local_files_only=True,cpu_threads=4)
results=[]
for key,expected in [('query','capability-guard'),('music','local-music-feedback')]:
 file=OUT/(key+'.wav')
 with wave.open(str(file)) as w:seconds=w.getnframes()/w.getframerate()+3
 q=queue.Queue()
 def callback(data,n,t,status):q.put(data[:,capture['channel']].copy())
 with sd.InputStream(device=capture['device'],channels=capture['channels'],samplerate=16000,blocksize=1600,dtype='float32',callback=callback):
  env=dict(os.environ,DINGOUS_TEST_WAV=str(file))
  playback=subprocess.Popen(['powershell.exe','-NoProfile','-Command','$ErrorActionPreference="Stop";$p=New-Object System.Media.SoundPlayer;$p.SoundLocation=$env:DINGOUS_TEST_WAV;$p.PlaySync()'],env=env,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
  chunks=[q.get(timeout=3) for _ in range(int(seconds*10))]
 playback.wait(timeout=15)
 audio=np.concatenate(chunks)
 segments,_=model.transcribe(recognition_audio(audio),language='pt',beam_size=5,vad_filter=True,initial_prompt='Dingous é o nome do assistente.')
 text=' '.join(s.text for s in segments if s.no_speech_prob<.6);command=wake_command(text)
 routed=False
 if command:
  with tempfile.TemporaryDirectory() as d:routed=answer(command,Path(d)).get('model')==expected
 result=dict(case=key,playbackExit=playback.returncode,microphonePeak=float(np.max(np.abs(audio))),speechRecognized=bool(text),wakeRecognized=command is not None,routedToExpectedHandler=routed,audioSaved=False)
 result['passed']=playback.returncode==0 and bool(command) and routed;results.append(result);print(json.dumps(result),flush=True)
report=dict(capture=capture,results=results,passed=all(r['passed'] for r in results),source='physical Windows playback-to-microphone; not human-distance validation')
(OUT/'physical-audio.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
sys.exit(0 if report['passed'] else 1)
