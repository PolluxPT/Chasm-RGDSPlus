using ChasmRGDS;
if(Environment.GetEnvironmentVariable("CHASM_STARTUP_VOLUME")=="1"){
 SDL2.SDL.SDL_InitSubSystem(0x10);var startupNode=System.IO.Path.GetTempFileName();
 try{foreach(int level in Enumerable.Range(0,11)){System.IO.File.WriteAllText(startupNode,level+"\n");RGDSVolume.InitializeCore(startupNode,false);Device.InitializeAudio();float actual=Microsoft.Xna.Framework.Audio.SoundEffect.MasterVolume;if(Math.Abs(actual-RGDSVolume.Curve(level))>0.000001)throw new Exception("Startup master volume mismatch at "+level);RGDSVolume.Shutdown();if(System.IO.File.ReadAllText(startupNode).Trim()!=level.ToString())throw new Exception("Restore failed");}Console.WriteLine("PASS: all 11 native startup master levels match the volume curve before any game frame, including mute, with restoration");return;}finally{RGDSVolume.Shutdown();System.IO.File.Delete(startupNode);}
}
if(Environment.GetEnvironmentVariable("CHASM_TRACE_TEST")=="1"){
 var path=System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory,"..","logs","Startup_Trace.txt"));System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));System.IO.File.WriteAllText(path,"unchanged");var stamp=System.IO.File.GetLastWriteTimeUtc(path);
 for(int i=0;i<100000;i++)Device.Trace("regression check");
 if(System.IO.File.ReadAllText(path)!="unchanged"||System.IO.File.GetLastWriteTimeUtc(path)!=stamp)throw new Exception("Trace performed disk writes");Console.WriteLine("PASS: 100000 disabled trace calls perform no file writes");return;
}
if(Environment.GetEnvironmentVariable("CHASM_AUDIO_FULL")=="1"){
 SDL2.SDL.SDL_InitSubSystem(0x10);
 if(Environment.GetEnvironmentVariable("CHASM_MUSIC_ONLY")!="1"){Microsoft.Xna.Framework.Audio.SoundEffect.MasterVolume=.1f;
 Microsoft.Xna.Framework.FrameworkDispatcher.Update();using(var sound=new Microsoft.Xna.Framework.Audio.SoundEffect(new byte[8820],44100,Microsoft.Xna.Framework.Audio.AudioChannels.Mono)){if(!sound.Play())throw new Exception("Sound play failed");System.Threading.Thread.Sleep(100);}
 Console.WriteLine("PASS: ARM64 SoundEffect initialization and PCM playback");return;}
 var content=Environment.GetEnvironmentVariable("CHASM_CONTENT");
 using var engine=new Microsoft.Xna.Framework.Audio.AudioEngine(System.IO.Path.Combine(content,"music/xact_test.xgs"));
 using var waves=new Microsoft.Xna.Framework.Audio.WaveBank(engine,System.IO.Path.Combine(content,"music/songs.xwb"),0,4);
 using var bank=new Microsoft.Xna.Framework.Audio.SoundBank(engine,System.IO.Path.Combine(content,"music/sound bank.xsb"));
 engine.GetCategory("Music").SetVolume(.1f);using var cue=bank.GetCue("menu");cue.Play();for(int i=0;i<10;i++){engine.Update();System.Threading.Thread.Sleep(10);}
 Console.WriteLine("PASS: ARM64 .NET SoundEffect device, PCM playback, XACT engine, actual Chasm streaming music bank and sound bank");return;
}
if(Environment.GetEnvironmentVariable("CHASM_AUDIO_DIRECT")=="1"){
 SDL2.SDL.SDL_InitSubSystem(0x10);var probe=new AudioFieldProbe();for(int i=0;i<1000;i++){var status=FAudio.FAudio_GetDeviceDetails(IntPtr.Zero,0,out probe.Details);if(status!=0||probe.Details.OutputFormat.Format.nChannels!=2)throw new Exception("Direct field query failed");}Console.WriteLine("PASS: 1000 original ARM64 native queries into managed field");return;
}
if(Environment.GetEnvironmentVariable("CHASM_AUDIO_STUB")=="1"){
 for(int i=0;i<1000;i++){var status=ChasmRGDS.AudioInterop.GetDeviceDetails(IntPtr.Zero,0,out var d);if(status!=0||d.OutputFormat.Format.nChannels!=2||d.OutputFormat.Format.nSamplesPerSec!=48000)throw new Exception("Audio copy failed");}
 Console.WriteLine("PASS: 1000 unmanaged device-detail transfers, including 16-byte native overwrite beyond nominal struct");return;
}
Console.WriteLine("SDL2 platform: "+SDL2.SDL.SDL_GetPlatform());
var node=Path.GetTempFileName();
void Check(bool c,string m){if(!c)throw new Exception(m);}
try{
 foreach(int initial in Enumerable.Range(0,11)){
  File.WriteAllText(node,initial+"\n");RGDSVolume.InitializeCore(node,false);
  Check(Math.Abs(RGDSVolume.Gain-0.35*Math.Pow(initial/10.0,2))<0.000001,"curve");
  RGDSVolume.HandleEvent(1,115,1,1000,5);Check(RGDSVolume.Level==Math.Min(initial+1,10),"up");
  RGDSVolume.HandleEvent(1,115,2,1100,5);Check(RGDSVolume.Level==Math.Min(initial+1,10),"repeat throttle");
  RGDSVolume.HandleEvent(1,115,2,1200,5);Check(RGDSVolume.Level==Math.Min(initial+2,10),"repeat");
  RGDSVolume.HandleEvent(1,114,1,1300,5);int before=RGDSVolume.Level;
  RGDSVolume.HandleEvent(3,114,1,1400,5);RGDSVolume.HandleEvent(1,114,0,1400,5);Check(RGDSVolume.Level==before,"ignore axis/release");
  RGDSVolume.Shutdown();Check(File.ReadAllText(node).Trim()==initial.ToString(),"restore");
 }
 File.WriteAllText(node,"invalid\n");RGDSVolume.InitializeCore(node,false);RGDSVolume.HandleEvent(1,115,1,1000,5);RGDSVolume.Shutdown();Check(File.ReadAllText(node)=="invalid\n","invalid node preservation");
 Console.WriteLine("PASS: 11 entry levels, exact curve, up/down, key repeat, axis ignored, volume restoration, invalid-node preservation.");
}finally{RGDSVolume.Shutdown();File.Delete(node);}

class AudioFieldProbe{public FAudio.FAudioDeviceDetails Details;}
