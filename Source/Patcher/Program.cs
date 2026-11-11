using Mono.Cecil;using Mono.Cecil.Cil;
string source=args[0],dest=args[1],host=args[2],fnaPath=args[3];Directory.CreateDirectory(dest);
var resolver=new DefaultAssemblyResolver();resolver.AddSearchDirectory(source);resolver.AddSearchDirectory(Path.GetDirectoryName(fnaPath));var rp=new ReaderParameters{AssemblyResolver=resolver};
using var hook=AssemblyDefinition.ReadAssembly(host);using var fna=AssemblyDefinition.ReadAssembly(fnaPath);
var device=hook.MainModule.GetType("ChasmRGDS.Device");
foreach(string name in new[]{"Chasm.exe","bitkid.common.dll"}){
 using var a=AssemblyDefinition.ReadAssembly(Path.Combine(source,name),rp);
 a.MainModule.Architecture=TargetArchitecture.I386;a.MainModule.Attributes=ModuleAttributes.ILOnly;
 foreach(var t in a.MainModule.Types.Where(t=>t.Namespace.StartsWith("Chasm.Platforms.GOG")||t.Namespace.StartsWith("Chasm.Platforms.Steam")).ToArray())a.MainModule.Types.Remove(t);
 foreach(var r in a.MainModule.AssemblyReferences.Where(r=>r.Name=="GalaxyCSharp"||r.Name=="Steamworks.NET").ToArray())a.MainModule.AssemblyReferences.Remove(r);
 foreach(var t in a.MainModule.Types.SelectMany(Flatten))foreach(var m in t.Methods.Where(m=>m.HasBody))foreach(var i in m.Body.Instructions){
 if(i.Operand is MethodReference mr&&mr.DeclaringType.FullName=="SDL3.SDL"){
  var target=fna.MainModule.GetType("SDL2.SDL").Methods.Single(x=>x.Name==mr.Name&&x.Parameters.Count==mr.Parameters.Count);i.Operand=a.MainModule.ImportReference(target);
 }
 }
 if(name=="Chasm.exe"){
 var soundInit=a.MainModule.GetType("Chasm.SoundHelper").Methods.Single(m=>m.Name=="LoadContent");var soundIL=soundInit.Body.GetILProcessor();soundIL.InsertBefore(soundInit.Body.Instructions[0],soundIL.Create(OpCodes.Call,a.MainModule.ImportReference(device.Methods.Single(m=>m.Name=="InitializeAudio"))));
 var update=a.MainModule.GetType("Chasm.ChasmGame").Methods.Single(m=>m.Name=="Update"&&m.Parameters.Count==1);var il=update.Body.GetILProcessor();var first=update.Body.Instructions[0];il.InsertBefore(first,il.Create(OpCodes.Ldarg_0));il.InsertBefore(first,il.Create(OpCodes.Call,a.MainModule.ImportReference(device.Methods.Single(m=>m.Name=="OnFrame"))));
 }
 if(name=="Chasm.exe")foreach(var t in a.MainModule.Types.SelectMany(Flatten))foreach(var m in t.Methods.Where(m=>m.HasBody)){
 if((t.FullName=="Chasm.ChasmGame"&&m.Name=="LoadContent")||(t.FullName=="Chasm.UserInfo"&&new[]{"Load","Save","GetUserSaveData"}.Contains(m.Name))||(t.FullName=="Chasm.SoundHelper"&&m.Name=="LoadContent")||(t.FullName=="Chasm.Gameplay.Database"&&m.Name=="LoadItemAndStrings")||(t.FullName=="Chasm.ButtonHelper"&&m.Name=="LoadContent")||(t.FullName=="Chasm.Gameplay.RumbleManager"&&m.Name=="Init")||(t.FullName=="Chasm.ChasmGame"&&m.Name=="LoadEffect"))Instrument(a.MainModule,m,device);
 }
 a.Write(Path.Combine(dest,name));Console.WriteLine("Patched "+name);
}
// Keep FNA upstream source unchanged; add the port's category gain hook in the private binary.
var category=fna.MainModule.GetType("Microsoft.Xna.Framework.Audio.AudioCategory");var set=category.Methods.Single(m=>m.Name=="SetVolume");var proc=set.Body.GetILProcessor();var start=set.Body.Instructions[0];
foreach(var i in new[]{proc.Create(OpCodes.Ldarg_0),proc.Create(OpCodes.Ldobj,category),proc.Create(OpCodes.Ldarg_1),proc.Create(OpCodes.Call,fna.MainModule.ImportReference(device.Methods.Single(m=>m.Name=="CategoryVolume"))),proc.Create(OpCodes.Starg_S,set.Parameters[0])})proc.InsertBefore(start,i);
foreach(var t in fna.MainModule.Types.SelectMany(Flatten))foreach(var m in t.Methods.Where(m=>m.HasBody))if((t.FullName=="Microsoft.Xna.Framework.Audio.SoundEffect"&&m.Name=="Device")||t.FullName.Contains("FAudioContext")&&(m.IsConstructor||m.Name=="Create"))Instrument(fna.MainModule,m,device);
foreach(var t in fna.MainModule.Types.SelectMany(Flatten))foreach(var m in t.Methods.Where(m=>m.HasBody))foreach(var i in m.Body.Instructions)if(i.Operand is MethodReference mr&&mr.DeclaringType.FullName=="FAudio"&&mr.Name=="FAudio_GetDeviceDetails")i.Operand=fna.MainModule.ImportReference(hook.MainModule.GetType("ChasmRGDS.AudioInterop").Methods.Single(x=>x.Name=="GetDeviceDetails"));
fna.Write(Path.Combine(dest,"FNA.dll"));Console.WriteLine("Patched XACT category gain");
static IEnumerable<TypeDefinition> Flatten(TypeDefinition t){yield return t;foreach(var n in t.NestedTypes.SelectMany(Flatten))yield return n;}

static void Instrument(ModuleDefinition module,MethodDefinition m,TypeDefinition hook){
 if(Environment.GetEnvironmentVariable("CHASM_BUILD_TRACE")!="1")return;
 var il=m.Body.GetILProcessor();var callback=module.ImportReference(hook.Methods.Single(m=>m.Name=="Trace"));
 var first=m.Body.Instructions[0];il.InsertBefore(first,il.Create(OpCodes.Ldstr,"ENTER "+m.FullName));il.InsertBefore(first,il.Create(OpCodes.Call,callback));
}
