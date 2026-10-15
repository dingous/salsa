"""Device-local capture routing; never modifies Windows audio devices or permissions."""
import json,os
import numpy as np
from pathlib import Path
FILE=Path(os.getenv('LOCALAPPDATA',str(Path.home())))/'Dingous/Partner/microphone-windows.json'

def recognition_audio(audio):
    """Bounded gain for quiet voice; silence remains silence and VAD still decides speech."""
    rms=float(np.sqrt(np.mean(audio*audio)))
    gain=min(16,.025/max(rms,.00001))
    return np.clip(audio*gain,-1,1).astype(np.float32)

def capture_settings(sd):
    if not FILE.exists():return dict(device=None,channels=1,channel=0)
    data=json.loads(FILE.read_text(encoding='utf-8'))
    devices=sd.query_devices();apis=sd.query_hostapis()
    matches=[(i,d) for i,d in enumerate(devices) if d['name']==data.get('deviceName') and apis[d['hostapi']]['name']==data.get('hostApi') and d['max_input_channels']>0]
    if len(matches)!=1:raise RuntimeError('Microfone configurado não está disponível. Confira a entrada local do Dingous.')
    index,device=matches[0];channel=data.get('channel',0)
    if not isinstance(channel,int) or not 0<=channel<device['max_input_channels']:raise ValueError('Canal do microfone inválido.')
    channels=int(device['max_input_channels'])
    sd.check_input_settings(device=index,samplerate=16000,channels=channels,dtype='float32')
    return dict(device=index,channels=channels,channel=channel)
