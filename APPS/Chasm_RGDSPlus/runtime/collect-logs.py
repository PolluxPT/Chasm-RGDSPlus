import pathlib,sys,zipfile
root=pathlib.Path(sys.argv[1]);logs=root/'logs'
with zipfile.ZipFile(root/'Chasm_TestLogs.zip','w',zipfile.ZIP_DEFLATED) as z:
 for p in logs.iterdir():
  if p.is_file() and p.suffix in ('.txt','.json','.log') and p.stat().st_size<20*1024*1024:z.write(p,'logs/'+p.name)
 for p in [root/'savedata/config/chasm-controls-v1.json',root/'savedata/data/Chasm/Log.txt']:
  if p.is_file():z.write(p,str(p.relative_to(root)))
