"""Explicit configured desktop profiles; never execute model-generated commands."""
import json
import os
import re
import subprocess
from pathlib import Path

KEY = 'dingous-app-launcher'
BLOCKED = {'cmd', 'powershell', 'pwsh', 'python', 'pythonw', 'wscript', 'cscript',
           'mshta', 'rundll32', 'regsvr32', 'bash', 'sh', 'dotnet', 'node', 'java', 'javaw'}

def profile_name(question):
    match = re.fullmatch(r'(?:dingous[, ]+)?(?:abra|abrir|inicie)\s+(?:o |a )?([a-z][a-z0-9-]{0,39}|bloco de notas)[.!]?', question.strip().casefold())
    if not match:return None
    return 'notepad' if match.group(1)=='bloco de notas' else match.group(1)

def response(question, settings, protected):
    result = dict(question=question, model=KEY, agentKey=KEY, fineTuned=False,
                  speechAllowed=False, actionExecuted=False)
    if not settings.get('launcher',{}).get('enabled'):
        return dict(result, answer='Entendi o pedido, mas o agente Aplicativos ainda não está habilitado no admin ChatTrade. Habilite o agente e cadastre o perfil solicitado para permitir a abertura.')
    if protected:
        return dict(result, answer='Não abri o aplicativo: a proteção de ambiente compartilhado está ativa. A abertura exige ambiente privado autorizado no admin.')
    if not settings.get('_launcherAuthorized'):
        return dict(result, answer='Não abri o aplicativo: a API ainda não autorizou o agente Aplicativos para esta sessão e aplicativo.')
    name = profile_name(question)
    try:
        profiles = json.loads(settings.get('launcher', {}).get('profilesJson', '[]'))
        if not isinstance(profiles, list) or len(profiles) > 20: raise ValueError()
        matches = [p for p in profiles if p.get('name') == name]
        if len(matches) != 1: raise ValueError()
        profile = matches[0]
        if profile.get('applicationKey') != settings.get('_launcherApplication'): raise ValueError()
        executable = Path(profile['executable'])
        arguments = profile['arguments']
        if os.name != 'nt' or not executable.is_absolute() or str(executable).startswith('\\\\'): raise ValueError()
        if executable.suffix.lower() != '.exe' or executable.stem.lower() in BLOCKED or not executable.is_file(): raise ValueError()
        if not isinstance(arguments, list) or len(arguments) > 30 or any(not isinstance(a, str) or len(a) > 1000 or '\0' in a for a in arguments): raise ValueError()
        process = subprocess.Popen([str(executable), *arguments], shell=False, cwd=str(executable.parent), close_fds=True)
        return dict(result, answer=f'Abertura do perfil {name} solicitada ao Windows.', actionExecuted=True, processId=process.pid)
    except (ValueError, TypeError, KeyError, OSError, AttributeError):
        return dict(result, answer='Não abri o aplicativo. Verifique o perfil autorizado, o executável e os parâmetros no admin.')
