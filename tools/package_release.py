"""Package only distributable build/project files; verify every ZIP entry."""
import argparse, hashlib, json, re, zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / 'output'
BALANCE_VERSION = json.loads((ROOT/'SummonersTable/Assets/Resources/Data/catalog.json').read_text(encoding='utf-8'))['version']
parser=argparse.ArgumentParser()
player_version=re.search(r'^\s*bundleVersion: (\S+)',(ROOT/'SummonersTable/ProjectSettings/ProjectSettings.asset').read_text(encoding='utf-8'),re.MULTILINE).group(1)
parser.add_argument('--version',default=player_version,help='Player build version (defaults to Unity project settings)')
VERSION=parser.parse_args().version
assert all(part.isdigit() for part in VERSION.split('.')) and len(VERSION.split('.'))==3
BUILD = ROOT / ('Builds/Windows-v'+VERSION)
required = ['ZaOdnimStolom.exe', 'UnityPlayer.dll', 'steam_appid.txt',
            'ZaOdnimStolom_Data/Plugins/x86_64/steam_api64.dll',
            'Cards-and-rules-RU.pdf', 'READ-ME-RU.txt', 'THIRD-PARTY.txt']
for name in required:
    assert (BUILD / name).is_file(), f'Missing build dependency: {name}'
assert (BUILD / 'steam_appid.txt').read_text().strip() == '480'
assert len(list((ROOT/'SummonersTable/Assets/Resources/Art').glob('*.png'))) == 33

def package(path, items):
    with zipfile.ZipFile(path, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as z:
        for source, archive_name in sorted(items, key=lambda item:item[1]):
            z.write(source, archive_name)
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None, 'Corrupt archive: '+str(path)
        count=len(z.namelist())
    return dict(file=path.name, bytes=path.stat().st_size, entries=count,
                sha256=hashlib.sha256(path.read_bytes()).hexdigest())

build_items=[(p, 'ZaOdnimStolom/'+p.relative_to(BUILD).as_posix()) for p in BUILD.rglob('*') if p.is_file() and not any('DoNotShip' in part for part in p.parts)]
project_items=[]
for folder in ['SummonersTable/Assets','SummonersTable/Packages','SummonersTable/ProjectSettings','docs','tools','output/tests','output/pdf']:
    for p in (ROOT/folder).rglob('*'):
        if p.is_file() and '__pycache__' not in p.parts:
            project_items.append((p,'ZaOdnimStolom-Unity/'+p.relative_to(ROOT).as_posix()))
for name in ['README.md','.gitignore','.gitattributes','SummonersTable/steam_appid.txt']:
    project_items.append((ROOT/name,'ZaOdnimStolom-Unity/'+name))
manifest={'version':VERSION,'balanceVersion':BALANCE_VERSION,'steamAppId':480,'networkProtocol':4,'unity':'6000.3.21f1','artAssets':34,'uniqueCards':30,'selectableHeroes':8,
          'artMode':'built-in image_gen','pdfPages':22,'archives':[
    package(OUTPUT/('ZaOdnimStolom-Windows-v'+VERSION+'.zip'),build_items),
    package(OUTPUT/('ZaOdnimStolom-Unity-v'+VERSION+'.zip'),project_items)]}
(OUTPUT/'release-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(manifest,ensure_ascii=False,indent=2))
