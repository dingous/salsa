"""Fetch the selected official asset packs from MakeHuman's mirror."""
import re
import urllib.request
import zipfile
from pathlib import Path

root = Path(__file__).resolve().parents[1] / 'Downloads'
root.mkdir(exist_ok=True)
for pack in ('makehuman_system_assets', 'faceunits01', 'visemes02', 'bodyparts05', 'glasses01'):
    path = root / f'{pack}.zip'
    if path.exists() and zipfile.is_zipfile(path):
        print(pack, 'already downloaded', flush=True)
        continue
    if pack in ('faceunits01', 'visemes02'):
        url=f'https://files.makehumancommunity.org/functional/{pack}.zip'
    else:
        html = urllib.request.urlopen(f'https://static.makehumancommunity.org/assets/assetpacks/{pack}.html', timeout=30).read().decode('utf-8')
        url = re.findall(r'href="([^"]+\.zip)"', html)[0].replace('files2.', 'files.')
    urllib.request.urlretrieve(url, path)
    print(pack, zipfile.ZipFile(path).namelist()[:15], flush=True)
