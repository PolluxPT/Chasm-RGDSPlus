using System.Runtime.InteropServices;
namespace ChasmRGDS;
public static class AudioInterop {
 [DllImport("libFAudio.so.0",EntryPoint="FAudio_GetDeviceDetails",CallingConvention=CallingConvention.Cdecl)]
 static extern uint GetDetails(IntPtr audio,uint index,IntPtr result);
 public static uint GetDeviceDetails(IntPtr audio,uint index,out FAudio.FAudioDeviceDetails details){
  // Native writes go to an oversized unmanaged allocation, never into a managed object field.
  IntPtr buffer=Marshal.AllocHGlobal(4096);
  try{
   Marshal.Copy(new byte[4096],0,buffer,4096);
   Device.Trace("Audio details: native query index="+index+" managed size="+Marshal.SizeOf<FAudio.FAudioDeviceDetails>());
   uint result=GetDetails(audio,index,buffer);
   details=result==0?Marshal.PtrToStructure<FAudio.FAudioDeviceDetails>(buffer):default;
   Device.Trace("Audio details: result="+result+" channels="+details.OutputFormat.Format.nChannels+" rate="+details.OutputFormat.Format.nSamplesPerSec);
   return result;
  }finally{Marshal.FreeHGlobal(buffer);}
 }
}
