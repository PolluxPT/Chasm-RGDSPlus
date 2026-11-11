#!/usr/bin/env python3
"""Read raw BTN_START/BTN_SELECT identities as Yokoi does; never guess JS button indices."""
import fcntl,glob,os,pathlib,signal,sys,time
START=315;SELECT=314;GET_KEYS=0x80604518

def combo(keys):
    return bool(keys[START//8]&(1<<(START%8)) and keys[SELECT//8]&(1<<(SELECT%8)))

def main():
    parent=int(sys.argv[1]);request=pathlib.Path(sys.argv[2]);devices=[];held=None;requested=None
    for path in sorted(glob.glob('/sys/class/input/event*')):
        fd=None
        try:
            name=(pathlib.Path(path)/'device/name').read_text().strip()
            if 'anbernic' not in name.lower():continue
            node='/dev/input/'+pathlib.Path(path).name
            fd=os.open(node,os.O_RDONLY|os.O_NONBLOCK|os.O_CLOEXEC)
            keys=bytearray(96);fcntl.ioctl(fd,GET_KEYS,keys,True)
            devices.append(fd)
            print('Exit guard:',node,name,'START=BTN_START(315) SELECT=BTN_SELECT(314); raw current key state.',flush=True)
        except OSError as ex:
            if fd is not None:os.close(fd)
            print('Exit guard:',path,ex,flush=True)
    if not devices:
        print('No raw ANBERNIC node. Calibrated SDL START+SELECT handling remains active.',flush=True);return
    try:
        while devices:
            pressed=False
            for fd in devices[:]:
                try:
                    keys=bytearray(96);fcntl.ioctl(fd,GET_KEYS,keys,True);pressed|=combo(keys)
                except OSError:os.close(fd);devices.remove(fd)
            now=time.monotonic()
            if pressed:
                if held is None:held=now
                if now-held>=1.5 and requested is None:
                    request.write_text('START+SELECT\n');requested=now
                    print('Graceful exit requested.',flush=True)
            else:held=None
            if requested is not None and now-requested>=8:
                print('Exit timeout; asking launcher to clean up.',flush=True)
                os.kill(parent,signal.SIGTERM);return
            time.sleep(0.05)
    finally:
        for fd in devices:os.close(fd)
if __name__=='__main__':main()
