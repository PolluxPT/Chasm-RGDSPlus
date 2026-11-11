// RGDS+ standalone port integration, MIT. Learned raw bindings, inspired by Yokoi's per-GUID calibration.
using System.Text.Json;
using System.Text.RegularExpressions;
namespace ChasmRGDS;
public sealed class ControlProfile {
 public int Version {get;set;}=1;
 public string Guid {get;set;}="";
 public int ButtonCount {get;set;}
 public int AxisCount {get;set;}
 public int HatCount {get;set;}
 public Dictionary<string,string> Bindings {get;set;}=new();
 public static readonly (string Action,string Label)[] Steps={
  ("a","PHYSICAL A"),("b","PHYSICAL B"),("x","PHYSICAL X"),("y","PHYSICAL Y"),
  ("leftshoulder","L1"),("rightshoulder","R1"),("back","SELECT"),("start","START"),
  ("lefttrigger","L2"),("righttrigger","R2"),("dpup","DPAD UP"),("dpdown","DPAD DOWN"),("dpleft","DPAD LEFT"),("dpright","DPAD RIGHT")};
 public static string ProfilePath=>Environment.GetEnvironmentVariable("CHASM_CONTROLLER_PROFILE")??Path.Combine(Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")??Path.Combine(AppContext.BaseDirectory,"..","savedata","config"),"chasm-controls-v1.json");
 public bool Valid(string guid,int buttons,int axes,int hats) {
  if(Version!=1 || Guid!=guid || ButtonCount!=buttons || AxisCount!=axes || HatCount!=hats || !Regex.IsMatch(Guid,"^[0-9a-f]{32}$") || Bindings==null || Bindings.Count!=Steps.Length)return false;
  var used=new HashSet<string>();
  foreach(var step in Steps) {
   if(!Bindings.TryGetValue(step.Action,out string value)||value==null||!used.Add(value))return false;
   if(Regex.IsMatch(value,@"^b\d+$")){if(!int.TryParse(value[1..],out int n)||n<0||n>=buttons)return false;}
   else {var m=Regex.Match(value,@"^h(\d+)\.([1248])$");if(!m.Success||!int.TryParse(m.Groups[1].Value,out int n)||n<0||n>=hats)return false;}
  }
  return true;
 }
 public static ControlProfile Load(string guid,int buttons,int axes,int hats) {
  try {var p=new FileInfo(ProfilePath);if(!p.Exists||p.Length>32768)return null;var v=JsonSerializer.Deserialize<ControlProfile>(File.ReadAllText(p.FullName));return v!=null&&v.Valid(guid,buttons,axes,hats)?v:null;}
  catch(Exception e) when(e is IOException||e is JsonException||e is UnauthorizedAccessException){Console.WriteLine("CONTROL PROFILE: "+e.Message);return null;}
 }
 public string Mapping() => Guid+",RGDS calibrated,"+string.Join("",Steps.Select(s=>s.Action+":"+Bindings[s.Action]+","))+(AxisCount>=2?"leftx:a0,lefty:a1,":"")+"platform:Linux,";
 public void Save() {
  if(!Valid(Guid,ButtonCount,AxisCount,HatCount))throw new InvalidDataException("Incomplete controller calibration");
  string path=Path.GetFullPath(ProfilePath);Directory.CreateDirectory(Path.GetDirectoryName(path));
  string temp=path+".new";File.WriteAllText(temp,JsonSerializer.Serialize(this,new JsonSerializerOptions{WriteIndented=true})+"\n");File.Move(temp,path,true);
 }
}
