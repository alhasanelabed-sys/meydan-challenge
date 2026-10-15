using System.Text.Json.Nodes;
namespace Meydan;
public sealed class StateStore {
 readonly object sync=new();readonly string file;readonly JsonObject data;
 public StateStore(){var directory=Environment.GetEnvironmentVariable("MEYDAN_DATA_DIR")??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MeydanChallenge");file=Path.Combine(directory,"state.json");try{data=File.Exists(file)?JsonNode.Parse(File.ReadAllText(file))!.AsObject():new JsonObject();}catch{data=new JsonObject();}}
 public string Snapshot(){lock(sync)return data.ToJsonString();}
 public void Set(string key,string value){if(!key.StartsWith("meydan-",StringComparison.Ordinal)||key.Length>100||value.Length>8*1024*1024)throw new InvalidDataException("Invalid storage entry");lock(sync){var updated=(JsonObject)data.DeepClone();updated[key]=value;string json=updated.ToJsonString();if(json.Length>24*1024*1024)throw new InvalidDataException("Storage full");Directory.CreateDirectory(Path.GetDirectoryName(file)!);var temp=file+"."+Guid.NewGuid()+".tmp";File.WriteAllText(temp,json);File.Move(temp,file,true);data[key]=value;}}
}
