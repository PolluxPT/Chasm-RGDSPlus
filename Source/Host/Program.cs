using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
namespace ChasmRGDS;
internal static class Program {
 [STAThread] static int Main(string[] args) {
  AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization",true);
  AssemblyLoadContext.Default.Resolving+=(c,n)=>{string p=Path.Combine(AppContext.BaseDirectory,n.Name+".dll");return File.Exists(p)?c.LoadFromAssemblyPath(p):null;};
  try {
   Console.WriteLine("Chasm RGDS+ v1.0.0 / "+System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture);
   if(!args.Contains("--check-assembly")&&!args.Contains("--check-jit")) RGDSVolume.Initialize();
   if(args.Contains("--ensure-controls"))return ControlCalibration.Run(false);
   var a=Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory,"Chasm.exe"));
   if(args.Contains("--check-jit")){
    int n=0;foreach(var assembly in new[]{a,typeof(Game).Assembly})foreach(var t in assembly.GetTypes())foreach(var m in t.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance|BindingFlags.DeclaredOnly)){
     if((t.FullName=="Chasm.ChasmGame"&&m.Name=="LoadContent")||(t.FullName=="Chasm.UserInfo"&&new[]{"Load","Save","GetUserSaveData"}.Contains(m.Name))||(t.FullName=="Chasm.SoundHelper"&&m.Name=="LoadContent")||(t.FullName=="Chasm.Gameplay.Database"&&m.Name=="LoadItemAndStrings")||(t.FullName=="Microsoft.Xna.Framework.Audio.SoundEffect"&&m.Name=="Device")){Console.WriteLine("JIT "+t.FullName+"."+m);System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod(m.MethodHandle);n++;}
    }Console.WriteLine("Instrumented methods JIT checked: "+n);return 0;
   }
   if(args.Contains("--check-assembly")){Console.WriteLine("Types loaded: "+a.GetTypes().Length);return 0;}
   a.EntryPoint.Invoke(null,new object[]{Array.Empty<string>()});return 0;
  }catch(Exception e){Console.Error.WriteLine(e);if(e is ReflectionTypeLoadException r)foreach(var x in r.LoaderExceptions)Console.Error.WriteLine(x);return 1;}
  finally{RGDSVolume.Shutdown();}
 }
}
public static class Device {
 static readonly bool TraceEnabled=Environment.GetEnvironmentVariable("CHASM_TRACE")=="1";
 public static void Trace(string message){
  if(!TraceEnabled)return;
  try{var path=Path.Combine(AppContext.BaseDirectory,"..","logs","Startup_Trace.txt");File.AppendAllText(path,DateTime.UtcNow.ToString("O")+" T"+Environment.CurrentManagedThreadId+" "+message+"\n");}catch(IOException){}catch(UnauthorizedAccessException){}
 }
 public static bool HasCalibratedControls=>true;
 static long held,nextExitCheck;static readonly string exitRequest=Environment.GetEnvironmentVariable("CHASM_EXIT_REQUEST");static float previous=-1;static bool replay;static readonly Dictionary<AudioCategory,float> categories=new();
 public static float CategoryVolume(AudioCategory category,float value){if(!replay)categories[category]=value;return value*RGDSVolume.Gain;}
 public static void InitializeAudio(){
  float gain=RGDSVolume.Gain;SoundEffect.MasterVolume=gain;previous=gain;
 }
 public static void OnFrame(Game game){
  float gain=RGDSVolume.Gain;
  if(previous!=gain){SoundEffect.MasterVolume=gain;replay=true;try{foreach(var p in categories.ToArray())p.Key.SetVolume(p.Value);}finally{replay=false;}previous=gain;}
  bool combo=false;for(int i=0;i<4;i++){var s=GamePad.GetState((PlayerIndex)i);combo|=s.IsConnected&&s.Buttons.Start==ButtonState.Pressed&&s.Buttons.Back==ButtonState.Pressed;}
  long now=Environment.TickCount64;if(combo){if(held==0)held=now;if(now-held>=1500)game.Exit();}else held=0;
  if(now>=nextExitCheck){nextExitCheck=now+100;if(exitRequest!=null&&File.Exists(exitRequest))game.Exit();}
 }
}
