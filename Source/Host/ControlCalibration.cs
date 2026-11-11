// RGDS+ private port integration, MIT. SDL-only helper runs in a separate process before the game.
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace ChasmRGDS;
public static class ControlCalibration {
 const string SDL="libSDL2-2.0.so.0";
 [StructLayout(LayoutKind.Sequential)] struct Guid16 {public ulong a,b;}
 [StructLayout(LayoutKind.Sequential)] struct Rect {public int x,y,w,h;public Rect(int X,int Y,int W,int H){x=X;y=Y;w=W;h=H;}}
 [DllImport(SDL)] static extern int SDL_InitSubSystem(uint f);
 [DllImport(SDL)] static extern void SDL_Quit();
 [DllImport(SDL)] static extern int SDL_NumJoysticks();
 [DllImport(SDL)] static extern IntPtr SDL_JoystickNameForIndex(int i);
 [DllImport(SDL)] static extern IntPtr SDL_JoystickOpen(int i);
 [DllImport(SDL)] static extern Guid16 SDL_JoystickGetDeviceGUID(int i);
 [DllImport(SDL)] static extern void SDL_JoystickGetGUIDString(Guid16 g,byte[] s,int n);
 [DllImport(SDL)] static extern int SDL_JoystickNumButtons(IntPtr j);
 [DllImport(SDL)] static extern int SDL_JoystickNumAxes(IntPtr j);
 [DllImport(SDL)] static extern int SDL_JoystickNumHats(IntPtr j);
 [DllImport(SDL)] static extern byte SDL_JoystickGetButton(IntPtr j,int b);
 [DllImport(SDL)] static extern byte SDL_JoystickGetHat(IntPtr j,int h);
 [DllImport(SDL)] static extern void SDL_JoystickUpdate();
 [DllImport(SDL)] static extern int SDL_PollEvent(byte[] e);
 [DllImport(SDL)] static extern int SDL_PushEvent(byte[] e);
 [DllImport(SDL)] static extern int SDL_RenderReadPixels(IntPtr r,IntPtr rect,uint format,[Out] byte[] pixels,int pitch);
 [DllImport(SDL)] static extern IntPtr SDL_CreateWindow(string title,int x,int y,int w,int h,uint flags);
 [DllImport(SDL)] static extern IntPtr SDL_CreateRenderer(IntPtr window,int index,uint flags);
 [DllImport(SDL)] static extern int SDL_RenderSetLogicalSize(IntPtr r,int w,int h);
 [DllImport(SDL)] static extern int SDL_SetRenderDrawColor(IntPtr r,byte R,byte G,byte B,byte A);
 [DllImport(SDL)] static extern int SDL_RenderClear(IntPtr r);
 [DllImport(SDL)] static extern int SDL_RenderFillRect(IntPtr r,ref Rect rect);
 [DllImport(SDL)] static extern void SDL_RenderPresent(IntPtr r);
 [DllImport(SDL)] static extern IntPtr SDL_GetError();
 [DllImport(SDL)] static extern int SDL_JoystickAttachVirtual(int type,int axes,int buttons,int hats);
 [DllImport(SDL)] static extern int SDL_JoystickSetVirtualButton(IntPtr j,int b,byte v);
 [DllImport(SDL)] static extern int SDL_JoystickSetVirtualHat(IntPtr j,int h,byte v);
 [DllImport("libc.so.6",EntryPoint="open")] static extern int Open(string p,int flags);
 [DllImport("libc.so.6",EntryPoint="ioctl")] static extern int Ioctl(int fd,ulong req,[Out] byte[] data);
 [DllImport("libc.so.6",EntryPoint="close")] static extern int Close(int fd);
 static readonly int[] TestButtons={12,4,9,1,15,2,7,3,10,6};
 public static int Run(bool force) {
  string testMode=Environment.GetEnvironmentVariable("CHASM_CALIBRATION_TEST");bool test=testMode=="1"||testMode=="cancel";
  IntPtr window=IntPtr.Zero;int exitFd=-1;
  try {
   if(SDL_InitSubSystem(0x2200)!=0)throw new Exception(Marshal.PtrToStringUTF8(SDL_GetError()));
   if(test)SDL_JoystickAttachVirtual(1,4,17,1);
   int index=-1;
   for(int i=0;i<SDL_NumJoysticks();i++){string name=Marshal.PtrToStringUTF8(SDL_JoystickNameForIndex(i))??"";if(name.Contains("ANBERNIC",StringComparison.OrdinalIgnoreCase)||(test&&name.Contains("Virtual"))){index=i;break;}}
   if(index<0)throw new Exception("No RGDS controller found. Reconnect the built-in controller and try again.");
   var joystick=SDL_JoystickOpen(index);if(joystick==IntPtr.Zero)throw new Exception("Could not open controller");
   byte[] guidBytes=new byte[33];SDL_JoystickGetGUIDString(SDL_JoystickGetDeviceGUID(index),guidBytes,33);string guid=System.Text.Encoding.ASCII.GetString(guidBytes,0,32);
   int buttons=SDL_JoystickNumButtons(joystick),axes=SDL_JoystickNumAxes(joystick),hats=SDL_JoystickNumHats(joystick);
   var saved=ControlProfile.Load(guid,buttons,axes,hats);
   if(!force&&saved!=null){Console.WriteLine("CONTROLS: saved calibration valid; "+saved.Mapping());return 0;}
   if(SDL_InitSubSystem(0x20)!=0)throw new Exception(Marshal.PtrToStringUTF8(SDL_GetError()));
   window=SDL_CreateWindow("Chasm controls",0,0,640,480,test?4u:0x1005u);
   if(window==IntPtr.Zero)throw new Exception(Marshal.PtrToStringUTF8(SDL_GetError()));
   var renderer=SDL_CreateRenderer(window,-1,2);if(renderer==IntPtr.Zero)renderer=SDL_CreateRenderer(window,-1,1);
   if(renderer==IntPtr.Zero)throw new Exception(Marshal.PtrToStringUTF8(SDL_GetError()));
   SDL_RenderSetLogicalSize(renderer,640,480);
   foreach(var p in Directory.EnumerateDirectories("/sys/class/input","event*"))try{if(File.ReadAllText(Path.Combine(p,"device/name")).Contains("ANBERNIC")){exitFd=Open("/dev/input/"+Path.GetFileName(p),0x80800);if(exitFd>=0)break;}}catch(IOException){}
   var profile=new ControlProfile{Guid=guid,ButtonCount=buttons,AxisCount=axes,HatCount=hats};
   bool captured=false;int step=0,autoFrames=0;bool armed=false;long neutralSince=0,comboSince=0;string status="RELEASE ALL BUTTONS";byte[] e=new byte[56],keys=new byte[96];var clock=Stopwatch.StartNew();
   Console.WriteLine($"CONTROLS: calibrating GUID={guid}, buttons={buttons}, axes={axes}, hats={hats}");
   while(step<ControlProfile.Steps.Length) {
    if(testMode=="cancel"&&step==3){Array.Clear(e);BitConverter.GetBytes(0x300u).CopyTo(e,0);BitConverter.GetBytes(27).CopyTo(e,20);SDL_PushEvent(e);}
    while(SDL_PollEvent(e)!=0){uint type=BitConverter.ToUInt32(e);if(type==0x100||(test&&type==0x300&&BitConverter.ToInt32(e,20)==27))return 2;}
    if(test){for(int b=0;b<buttons;b++)SDL_JoystickSetVirtualButton(joystick,b,0);SDL_JoystickSetVirtualHat(joystick,0,0);if(autoFrames++>=16){if(step<10)SDL_JoystickSetVirtualButton(joystick,TestButtons[step],1);else SDL_JoystickSetVirtualHat(joystick,0,new byte[]{1,4,8,2}[step-10]);}}
    SDL_JoystickUpdate();long now=clock.ElapsedMilliseconds;
    if(exitFd>=0&&Ioctl(exitFd,0x80604518,keys)>=0){bool combo=(keys[314/8]&(1<<(314%8)))!=0&&(keys[315/8]&(1<<(315%8)))!=0;if(combo){if(comboSince==0)comboSince=now;if(now-comboSince>=2000)return 2;}else comboSince=0;}
    var held=new List<string>();for(int b=0;b<buttons;b++)if(SDL_JoystickGetButton(joystick,b)!=0)held.Add("b"+b);
    for(int h=0;h<hats;h++){byte value=SDL_JoystickGetHat(joystick,h);if(value!=0)held.Add("h"+h+"."+value);}
    if(!armed){if(held.Count==0){if(neutralSince==0)neutralSince=now;if(now-neutralSince>=150){armed=true;status="PRESS ONE BUTTON";}}else neutralSince=0;}
    else if(held.Count>0){
     bool hatAllowed=step>=10;string binding=held[0];
     if(held.Count!=1||(!hatAllowed&&binding.StartsWith('h'))||(binding.StartsWith('h')&&!new[]{1,2,4,8}.Contains(int.Parse(binding.Split('.')[1])))){status="ONE BUTTON AT A TIME";}
     else if(profile.Bindings.ContainsValue(binding))status="ALREADY USED TRY AGAIN";
     else {profile.Bindings.Add(ControlProfile.Steps[step].Action,binding);Console.WriteLine("CONTROLS: "+ControlProfile.Steps[step].Label+" = "+binding);step++;autoFrames=0;status="RELEASE ALL BUTTONS";}
     armed=false;neutralSince=0;
    }
    SDL_SetRenderDrawColor(renderer,12,20,34,255);SDL_RenderClear(renderer);
    Text(renderer,"CHASM CONTROLS",3,35,100,210,255);
    Text(renderer,"MATCH THE LABEL ON THE HANDHELD",2,115,220,230,240);
    if(step<ControlProfile.Steps.Length){Text(renderer,"PRESS "+ControlProfile.Steps[step].Label,4,185,255,255,255);Text(renderer,(step+1)+" OF "+ControlProfile.Steps.Length,2,260,180,195,210);}
    Text(renderer,status,2,320,255,210,100);
    Text(renderer,"HOLD START + SELECT TO CANCEL",2,425,180,195,210);
    if(test&&!captured&&Environment.GetEnvironmentVariable("CHASM_CALIBRATION_SCREENSHOT") is string shot){captured=true;byte[] pixels=new byte[640*480*4];if(SDL_RenderReadPixels(renderer,IntPtr.Zero,0x16762004,pixels,640*4)==0){using var file=File.Create(shot);file.Write(System.Text.Encoding.ASCII.GetBytes("P6\n640 480\n255\n"));for(int px=0;px<640*480;px++)file.Write(pixels,px*4,3);}}
    SDL_RenderPresent(renderer);Thread.Sleep(16);
   }
   if(test){for(int b=0;b<buttons;b++)SDL_JoystickSetVirtualButton(joystick,b,0);SDL_JoystickSetVirtualHat(joystick,0,0);}
   SDL_SetRenderDrawColor(renderer,12,20,34,255);SDL_RenderClear(renderer);
   Text(renderer,"CALIBRATION COMPLETE",3,180,100,210,255);Text(renderer,"RELEASE BUTTONS TO START",2,270,255,255,255);SDL_RenderPresent(renderer);
   while(true){SDL_JoystickUpdate();bool held=false;for(int b=0;b<buttons;b++)held|=SDL_JoystickGetButton(joystick,b)!=0;for(int h=0;h<hats;h++)held|=SDL_JoystickGetHat(joystick,h)!=0;if(!held)break;Thread.Sleep(16);}
   // Save only the complete, validated set. A cancelled remap keeps the previous profile.
   profile.Save();Console.WriteLine("CONTROLS: saved "+profile.Mapping());return 0;
  } catch(Exception ex){Console.Error.WriteLine("CONTROL SETUP ERROR: "+ex.Message);return 3;}
  finally{if(exitFd>=0)Close(exitFd);SDL_Quit();}
 }
 static void Text(IntPtr r,string text,int scale,int y,byte red,byte green,byte blue){SDL_SetRenderDrawColor(r,red,green,blue,255);int x=(640-text.Length*6*scale)/2;foreach(char c in text){if(Font.TryGetValue(c,out string pattern)){var rows=pattern.Split('/');for(int row=0;row<7;row++)for(int col=0;col<5;col++)if(rows[row][col]=='1'){var rect=new Rect(x+col*scale,y+row*scale,scale,scale);SDL_RenderFillRect(r,ref rect);}}x+=6*scale;}}
 static readonly Dictionary<char,string> Font=new(){
 ['A']="01110/10001/10001/11111/10001/10001/10001",['B']="11110/10001/10001/11110/10001/10001/11110",['C']="01111/10000/10000/10000/10000/10000/01111",['D']="11110/10001/10001/10001/10001/10001/11110",['E']="11111/10000/10000/11110/10000/10000/11111",['F']="11111/10000/10000/11110/10000/10000/10000",['G']="01111/10000/10000/10111/10001/10001/01111",['H']="10001/10001/10001/11111/10001/10001/10001",['I']="11111/00100/00100/00100/00100/00100/11111",['J']="00111/00010/00010/00010/10010/10010/01100",['K']="10001/10010/10100/11000/10100/10010/10001",['L']="10000/10000/10000/10000/10000/10000/11111",['M']="10001/11011/10101/10101/10001/10001/10001",['N']="10001/11001/10101/10011/10001/10001/10001",['O']="01110/10001/10001/10001/10001/10001/01110",['P']="11110/10001/10001/11110/10000/10000/10000",['Q']="01110/10001/10001/10001/10101/10010/01101",['R']="11110/10001/10001/11110/10100/10010/10001",['S']="01111/10000/10000/01110/00001/00001/11110",['T']="11111/00100/00100/00100/00100/00100/00100",['U']="10001/10001/10001/10001/10001/10001/01110",['V']="10001/10001/10001/10001/10001/01010/00100",['W']="10001/10001/10001/10101/10101/10101/01010",['X']="10001/10001/01010/00100/01010/10001/10001",['Y']="10001/10001/01010/00100/00100/00100/00100",['Z']="11111/00001/00010/00100/01000/10000/11111",['0']="01110/10001/10011/10101/11001/10001/01110",['1']="00100/01100/00100/00100/00100/00100/01110",['2']="01110/10001/00001/00010/00100/01000/11111",['3']="11110/00001/00001/01110/00001/00001/11110",['4']="00010/00110/01010/10010/11111/00010/00010",['5']="11111/10000/10000/11110/00001/00001/11110",['6']="01110/10000/10000/11110/10001/10001/01110",['7']="11111/00001/00010/00100/01000/01000/01000",['8']="01110/10001/10001/01110/10001/10001/01110",['9']="01110/10001/10001/01111/00001/00001/01110",['+']="00000/00100/00100/11111/00100/00100/00000"};
}
