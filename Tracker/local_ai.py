"""Start installed Ollama on loopback; never select remote models."""
import json
import os
import shutil
import subprocess
import time
import urllib.request
from pathlib import Path

def model_name():
    name = os.getenv('DINGOUS_AI_MODEL', 'qwen3.5:4b')
    if not name or 'cloud' in name.lower() or '://' in name:
        raise ValueError('Dingous permite somente modelos Ollama locais.')
    return name

def tags():
    with urllib.request.urlopen('http://127.0.0.1:11434/api/tags', timeout=2) as response:
        return json.loads(response.read(1000000))

def ensure_local_ai():
    model = model_name()
    try:
        state = tags()
    except OSError:
        executable = shutil.which('ollama') or str(Path(os.getenv('LOCALAPPDATA', '')) / 'Programs/Ollama/ollama.exe')
        if not Path(executable).is_file():
            raise RuntimeError('Ollama não instalado nesta máquina.')
        env = dict(os.environ, OLLAMA_HOST='127.0.0.1:11434', OLLAMA_NO_CLOUD='1', OLLAMA_NUM_PARALLEL='1')
        subprocess.Popen([executable, 'serve'], env=env, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
        for _ in range(40):
            time.sleep(.25)
            try:
                state = tags()
                break
            except OSError:
                continue
        else:
            raise RuntimeError('Não foi possível iniciar Ollama local.')
    matches = [entry for entry in state.get('models', []) if entry.get('name') == model or entry.get('model') == model]
    if not matches:
        raise RuntimeError('Modelo local não instalado: ' + model)
    if matches[0].get('remote_host') or matches[0].get('remote_model'):
        raise ValueError('Modelo remoto bloqueado pelo Dingous.')
    return model
