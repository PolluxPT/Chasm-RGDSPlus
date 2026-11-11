// RGDS+ integration, MIT. Curve and input selection follow PolluxPT/SonicMania-RGDSPlus.
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
[assembly: InternalsVisibleTo("VolumeTest")]
namespace ChasmRGDS;
public static class RGDSVolume {
    static readonly object Gate=new();
    static string node, saved;
    static int level;
    static float gain;
    static bool initialized, running;
    static Thread worker;
    static long lastRepeat, lastPress;
    static int lastCode, lastFd=-1;
    static readonly List<int> inputs=new();
    public static float Gain=>Volatile.Read(ref gain);
    public static int Level=>Volatile.Read(ref level);
    public static float Curve(int value) { float fraction=Math.Clamp(value,0,10)/10f; return 0.35f*fraction*fraction; }
    public static void Initialize() => InitializeCore(Environment.GetEnvironmentVariable("CHASM_VOLUME_NODE")??"/sys/class/anbernic_misc/openbor_volume",true);
    internal static void InitializeCore(string path,bool discover) {
        lock(Gate) {
            if(initialized)return;
            node=path; saved=null; level=2; lastRepeat=lastPress=0; lastCode=0;lastFd=-1;
            if(TryRead(out int initial)) { level=initial;saved=initial.ToString(); }
            Volatile.Write(ref gain,Curve(level)); initialized=true;
            Console.WriteLine($"VOLUME: entry OS level={(saved??"unavailable; fallback 2")}/10, gain={Gain:F6}; curve=0.35*(level/10)^2");
            if(!discover)return;
            try {
                foreach(var p in Directory.EnumerateDirectories("/sys/class/input","event*")) {
                    string name;
                    try { name=File.ReadAllText(Path.Combine(p,"device/name")).Trim(); }catch(IOException) { continue; }
                    if(!name.Contains("ANBERNIC",StringComparison.OrdinalIgnoreCase) && name!="adc-keys")continue;
                    int fd=NativeOpen("/dev/input/"+Path.GetFileName(p),0x80800); // O_NONBLOCK | O_CLOEXEC, read-only
                    if(fd>=0) { inputs.Add(fd); Console.WriteLine("VOLUME input: "+Path.GetFileName(p)+" / "+name); }
                    if(inputs.Count>=32)break;
                }
            }catch(Exception ex) when(ex is IOException || ex is UnauthorizedAccessException) { Console.WriteLine("VOLUME input discovery: "+ex.Message); }
            running=true; worker=new Thread(Poll){IsBackground=true,Name="RGDS volume keys"};worker.Start();
        }
    }
    static bool TryRead(out int value) {
        value=0;
        try { return int.TryParse(File.ReadAllText(node).Trim(),out value) && value>=0 && value<=10; }
        catch(Exception ex) when(ex is IOException || ex is UnauthorizedAccessException) { return false; }
    }
    static void Set(int value,bool write) {
        level=Math.Clamp(value,0,10);Volatile.Write(ref gain,Curve(level));
        if(write && saved!=null) {
            try { File.WriteAllText(node,level+"\n"); }
            catch(Exception ex) when(ex is IOException || ex is UnauthorizedAccessException) { Console.WriteLine("VOLUME node write: "+ex.Message); }
        }
    }
    internal static void HandleEvent(int type,int code,int value,long milliseconds,int fd) {
        lock(Gate) {
            if(!initialized || type!=1 || (code!=114 && code!=115) || (value!=1 && value!=2))return;
            if(value==2 && milliseconds-lastRepeat<180)return;
            if(value==1 && fd!=lastFd && code==lastCode && milliseconds-lastPress<30)return;
            lastRepeat=milliseconds;lastPress=milliseconds;lastCode=code;lastFd=fd;
            Set(level+(code==115?1:-1),true);
            Console.WriteLine($"VOLUME: {level}/10, gain={Gain:F6}");
        }
    }
    static void Poll() {
        byte[] data=new byte[24*64];long lastRead=0;
        while(Volatile.Read(ref running)) {
            long now=(long)(Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency*1000);
            lock(Gate) {
                if(!running)break;
                if(now-lastRead>=200) { if(TryRead(out int n))Set(n,false);lastRead=now; }
            }
            foreach(int fd in inputs) {
                long size=NativeRead(fd,data,(nuint)data.Length);
                for(int p=0;p+24<=size;p+=24) {
                    int type=BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(p+16,2));
                    int code=BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(p+18,2));
                    int value=BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(p+20,4));
                    HandleEvent(type,code,value,now,fd);
                }
            }
            Thread.Sleep(10);
        }
    }
    public static void Shutdown() {
        Thread previous;
        lock(Gate) { if(!initialized)return;running=false;previous=worker; }
        previous?.Join();
        lock(Gate) {
            foreach(int fd in inputs)NativeClose(fd);inputs.Clear();worker=null;
            if(saved!=null) {
                try { File.WriteAllText(node,saved+"\n");Console.WriteLine("VOLUME: restored OS level="+saved); }
                catch(Exception ex) when(ex is IOException || ex is UnauthorizedAccessException) { Console.WriteLine("VOLUME restore: "+ex.Message); }
            }
            initialized=false;saved=null;Volatile.Write(ref gain,0);
        }
    }
    [DllImport("libc.so.6",EntryPoint="open",SetLastError=true)] static extern int NativeOpen(string path,int flags);
    [DllImport("libc.so.6",EntryPoint="read",SetLastError=true)] static extern long NativeRead(int fd,[Out]byte[] data,nuint count);
    [DllImport("libc.so.6",EntryPoint="close")] static extern int NativeClose(int fd);
}
