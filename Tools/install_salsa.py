"""Restore the licensed runtime DLL from the owner's existing Unity Asset Store cache."""
import argparse,os,tarfile
from pathlib import Path
def install(package=None):
 root=Path(__file__).resolve().parents[1]/'CorporateUnity'
 name='Assets/Plugins/Crazy Minnow Studio/SALSA LipSync/Plugins/SALSA-LipSync.dll'
 target=root/name
 if target.exists():return target
 if package is None:
  cache=Path(os.getenv('APPDATA',''))/'Unity/Asset Store-5.x/Crazy Minnow Studio LLC'
  packages=list(cache.rglob('SALSA LipSync Suite.unitypackage'))
  if not packages:raise RuntimeError('Importe seu pacote licenciado SALSA LipSync Suite ou informe --package.')
  package=packages[0]
 with tarfile.open(package,'r:gz') as archive:
  for entry in archive:
   if not entry.name.endswith('/pathname'):continue
   pathname=archive.extractfile(entry).read().decode('utf-8').splitlines()[0]
   if pathname!=name:continue
   prefix=entry.name.rsplit('/',1)[0];target.parent.mkdir(parents=True,exist_ok=True)
   for source,destination in [('asset',target),('asset.meta',Path(str(target)+'.meta'))]:
    destination.write_bytes(archive.extractfile(prefix+'/'+source).read())
   return target
 raise RuntimeError('O pacote não contém o runtime SALSA esperado.')
if __name__=='__main__':
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--package',type=Path);args=p.parse_args();print('SALSA runtime instalado:',install(args.package))
