"""Offline preflight using Epic OpenRigLogic; never substitutes a DNA for a mesh."""
import argparse,json,subprocess
from pathlib import Path

def inspect(path):
    path=Path(path).resolve()
    if path.suffix.lower()!='.dna' or not path.is_file() or not 0<path.stat().st_size<=150*1024*1024:
        raise ValueError('Use um arquivo DNA existente, de até 150 MB.')
    executable=Path(__file__).parent/'native/MetaHuman/MetaHumanDnaProbe.exe'
    if not executable.is_file():raise RuntimeError('Prepare o Devkit com Tools/build-metahuman-devkit.ps1 no Windows.')
    result=subprocess.run([str(executable),str(path)],capture_output=True,text=True,encoding='utf-8',timeout=20,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
    if result.returncode:raise RuntimeError('DNA inválido ou incompatível com o OpenRigLogic instalado: '+result.stderr.strip()[:300])
    data=json.loads(result.stdout)
    if data.get('engine')!='Epic OpenRigLogic' or data.get('evaluationSteps')!=5:raise RuntimeError('Resposta inválida do avaliador DNA.')
    return data

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--file',required=True);args=parser.parse_args()
    print('METAHUMAN_DNA|'+json.dumps(inspect(args.file),ensure_ascii=False),flush=True)
