#!/usr/bin/env python3
"""Prepare owner-supplied Chasm assemblies on the device. No game bytes embedded."""
import hashlib,json,pathlib,shutil,subprocess,sys,tempfile
root=pathlib.Path(sys.argv[1]).resolve();rt=root/'runtime';original=root/'original';game=root/'game'
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
try:
 supported=json.loads((rt/'supported-game.json').read_text())
 for name,wanted in supported.items():
  p=original/name
  if not p.is_file():raise ValueError('Copy your official Linux Chasm game files into Chasm_RGDSPlus/original first. Missing: '+name)
  if digest(p)!=wanted:raise ValueError('Unsupported game build: '+name+'. This release requires official Linux Chasm 1.111 GOG build 91697. Your input was not changed.')
 if not (original/'Content').is_dir() and not (game/'Content').is_dir():raise ValueError('Missing Content folder. Copy the full official Linux game folder into original.')
 fingerprint={n:digest(rt/n) for n in ['patcher/Patcher.dll','patcher/FNA.Base.dll']}
 fingerprint.update(supported);fingerprint['host']=digest(game/'ChasmRGDS.Host.dll')
 marker=game/'.prepared.json'
 prepared=json.loads(marker.read_text()) if marker.exists() else {}
 outputs=['Chasm.exe','bitkid.common.dll','FNA.dll']
 valid=prepared.get('input')==fingerprint and all((game/n).is_file() and digest(game/n)==prepared.get('output',{}).get(n) for n in outputs)
 if not valid:
  with tempfile.TemporaryDirectory(prefix='prepare-',dir=root) as work:
   dotnet=sys.argv[2] if len(sys.argv)>2 else str(rt/'dotnet/dotnet')
   result=subprocess.run([dotnet,str(rt/'patcher/Patcher.dll'),str(original),work,str(game/'ChasmRGDS.Host.dll'),str(rt/'patcher/FNA.Base.dll')])
   if result.returncode:raise ValueError('Local game preparation failed. See Preparation.txt.')
   for n in outputs:shutil.copy2(pathlib.Path(work)/n,game/n)
  marker.write_text(json.dumps({'input':fingerprint,'output':{n:digest(game/n) for n in outputs}},indent=2))
 if not (game/'config.ini').exists():
  config=original/'config.ini'
  if not config.is_file():raise ValueError('Missing config.ini. Copy the complete original Linux game folder.')
  text=config.read_text(encoding='utf-8-sig').replace('Fullscreen=0','Fullscreen=1').replace('FitToScreen=0','FitToScreen=1').replace('Scaling=3','Scaling=2')
  (game/'config.ini').write_text(text)
 if not (game/'Content').exists():shutil.move(str(original/'Content'),str(game/'Content'))
 print('Game ready. Owner-supplied content remains on this SD card.');sys.exit(0)
except (OSError,ValueError,KeyError) as ex:
 print('PREPARATION ERROR:',ex);sys.exit(3)
