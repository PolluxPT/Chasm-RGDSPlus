#!/usr/bin/env python3
"""Session-only supported CPU/GPU governor selection and clock diagnostics.
No frequency limits, voltage or thermal controls are changed.
"""
import json,os,pathlib,sys,time

def nodes(root):
    root=pathlib.Path(root)
    return sorted((root/'devices/system/cpu/cpufreq').glob('policy*/scaling_governor'))+sorted((root/'class/devfreq').glob('*gpu*/governor'))

def read(path):
    try:return pathlib.Path(path).read_text().strip()
    except OSError:return '?'

def save(path,state):
    p=pathlib.Path(path);tmp=p.with_name(p.name+'.new');tmp.write_text(json.dumps(state));os.replace(tmp,p)

def apply(statefile,root='/sys'):
    state=[];save(statefile,state)
    for node in nodes(root):
        old=read(node);available=read(node.with_name('scaling_available_governors' if node.name=='scaling_governor' else 'available_governors')).split()
        print('GOVERNOR:',node,'original='+old,'available='+','.join(available),flush=True)
        if old=='performance' or 'performance' not in available or old=='?':continue
        state.append({'path':str(node),'original':old});save(statefile,state)
        try:node.write_text('performance\n');print('GOVERNOR: performance selected:',node,flush=True)
        except OSError as ex:print('GOVERNOR: unchanged:',ex,flush=True)
    return state

def restore(statefile):
    try:state=json.loads(pathlib.Path(statefile).read_text())
    except (OSError,ValueError):return
    for item in reversed(state):
        node=pathlib.Path(item['path'])
        try:
            if read(node)=='performance':node.write_text(item['original']+'\n');print('GOVERNOR: restored',node,item['original'],flush=True)
            else:print('GOVERNOR: changed externally; retained',node,read(node),flush=True)
        except OSError as ex:print('GOVERNOR restore:',ex,flush=True)

def watch(pid,root='/sys'):
    root=pathlib.Path(root);cpu=sorted((root/'devices/system/cpu/cpufreq').glob('policy*/scaling_cur_freq'));gpu=sorted((root/'class/devfreq').glob('*gpu*/cur_freq'));temp=sorted((root/'class/thermal').glob('thermal_zone*/temp'))
    hz=os.sysconf('SC_CLK_TCK');start=time.monotonic();previous=None
    while True:
        now=time.monotonic()
        try:
            stat=pathlib.Path('/proc/%d/stat'%pid).read_text().rsplit(')',1)[1].split()
            if stat[0]=='Z':return
            ticks=int(stat[11])+int(stat[12])
        except (OSError,ValueError,IndexError):return
        load=0 if previous is None else 100*(ticks-previous[1])/hz/max(now-previous[0],.01)
        previous=(now,ticks)
        values=['t=%.1f CPU%%=%.1f'%(now-start,load)]
        values+=['CPU:'+p.parent.name+'='+read(p)+'kHz' for p in cpu]
        values+=['GPU:'+p.parent.name+'='+read(p)+'Hz load='+read(p.with_name('load')) for p in gpu]
        values+=['TEMP:'+read(p.with_name('type'))+'='+read(p)+'mC' for p in temp]
        print('; '.join(values),flush=True);time.sleep(2)
if __name__=='__main__':
    if sys.argv[1]=='apply':apply(sys.argv[2])
    elif sys.argv[1]=='restore':restore(sys.argv[2])
    elif sys.argv[1]=='watch':watch(int(sys.argv[2]))
    else:raise SystemExit(2)
