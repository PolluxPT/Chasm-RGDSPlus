import pathlib,sys,json,hashlib
root=pathlib.Path(sys.argv[1]);m=json.loads((root/'runtime/install-manifest.json').read_text());bad=[]
for name,size in m['sizes'].items():
 if name=='game/config.ini':continue
 p=root/name
 if not p.is_file() or p.stat().st_size!=size:bad.append(name)
for name,digest in m['sha256'].items():
 p=root/name
 if p.is_file() and hashlib.sha256(p.read_bytes()).hexdigest()!=digest:bad.append(name+' (checksum)')
if bad:
 print('Incomplete or changed installation. Extract the complete ZIP again.');print('\n'.join(bad));sys.exit(1)
print('Installation check passed.')
