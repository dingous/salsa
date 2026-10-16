"""Device-local capture routing; never modifies Windows audio devices or permissions."""
import json,os
import numpy as np
from pathlib import Path
DEVICE_FILE=Path(__file__).with_name('microphone-device.json')
FILE=Path(os.getenv('LOCALAPPDATA',str(Path.home())))/'Dingous/Partner/microphone-windows.json'

def recognition_audio(audio):
    """Remove DC/low rumble and normalize speech locally, with bounded gain."""
    from scipy.signal import butter,sosfilt
    audio=np.nan_to_num(np.asarray(audio,dtype=np.float32),nan=0,posinf=0,neginf=0)
    if not audio.size:return audio
    filtered=sosfilt(butter(2,80,btype='highpass',fs=16000,output='sos'),audio)
    frame=1600;energies=[float(np.sqrt(np.mean(filtered[i:i+frame]**2))) for i in range(0,len(filtered),frame)]
    level=float(np.percentile(energies,90))
    gain=min(16,.025/max(level,.00001))
    return np.clip(filtered*gain,-1,1).astype(np.float32)

def capture_settings(sd):
    profile=DEVICE_FILE if DEVICE_FILE.exists() else FILE
    if not profile.exists():return dict(device=None,channels=1,channel=0)
    data=json.loads(profile.read_text(encoding='utf-8'))
    devices=sd.query_devices();apis=sd.query_hostapis()
    matches=[(i,d) for i,d in enumerate(devices) if d['name']==data.get('deviceName') and apis[d['hostapi']]['name']==data.get('hostApi') and d['max_input_channels']>0]
    if len(matches)!=1:raise RuntimeError('Microfone configurado não está disponível. Confira a entrada local do Dingous.')
    index,device=matches[0];channel=data.get('channel',0)
    if not isinstance(channel,int) or not 0<=channel<device['max_input_channels']:raise ValueError('Canal do microfone inválido.')
    channels=int(device['max_input_channels'])
    sd.check_input_settings(device=index,samplerate=16000,channels=channels,dtype='float32')
    return dict(device=index,channels=channels,channel=channel)
