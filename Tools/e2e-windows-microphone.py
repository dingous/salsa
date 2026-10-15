"""Assisted hardware/ASR check. Audio and transcripts remain in memory only."""
import sys,time,json,queue,argparse
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'Tracker'))
import numpy as np
import sounddevice as sd
from faster_whisper import WhisperModel
from microphone_input import capture_settings,recognition_audio
from dingous_listener import wake_command,CommandDeduplicator,VoiceActivityGate

p=argparse.ArgumentParser();p.add_argument('--go',required=True);p.add_argument('--report',required=True);args=p.parse_args()
model=WhisperModel('small',device='cpu',compute_type='int8',download_root=str(Path(__file__).resolve().parents[1]/'Tracker/models/whisper'),local_files_only=True,cpu_threads=4)
capture=capture_settings(sd)
print('MIC_E2E_READY',flush=True)
deadline=time.monotonic()+180
while not Path(args.go).exists():
 if time.monotonic()>deadline:raise TimeoutError('Assisted start timed out')
 time.sleep(.1)
q=queue.Queue();chunks=[]
def callback(data,frames,timing,status):q.put(data[:,capture['channel']].copy())
with sd.InputStream(device=capture['device'],channels=capture['channels'],samplerate=16000,blocksize=1600,dtype='float32',callback=callback):
 print('MIC_E2E_RECORDING',flush=True)
 for _ in range(250):chunks.append(q.get(timeout=3))
audio=np.concatenate(chunks);gate=VoiceActivityGate();active=sum(gate.active(c) for c in chunks)
print('MIC_E2E_PROCESSING speechFrames='+str(active),flush=True)
segments,_=model.transcribe(recognition_audio(audio),language='pt',beam_size=5,vad_filter=True,initial_prompt='Dingous é o nome do assistente.')
texts=[s.text.strip() for s in segments if s.no_speech_prob<.6]
commands=[wake_command(t) for t in texts];commands=[c for c in commands if c]
# ASR can split the wake name and command across adjacent segments.
combined=wake_command(' '.join(texts))
guard=CommandDeduplicator();dedup=False
if combined:dedup=guard.accept(combined,100) and not guard.accept(combined,101)
report=dict(capture=capture,peak=float(np.max(np.abs(audio))),speechFrames=active,transcribed=bool(texts),wakeRecognized=combined is not None,commandRecognized=bool(combined),duplicateGuard=dedup,audioSaved=False,actionsExecuted=False)
report['passed']=active>0 and bool(combined) and dedup
Path(args.report).write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps(report),flush=True)
sys.exit(0 if report['passed'] else 1)
