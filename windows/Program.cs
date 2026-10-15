using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using System.Net.WebSockets;
using InTheHand.Net;
using InTheHand.Net.Sockets;
using InTheHand.Net.Bluetooth;

namespace Meydan;
public static class Program {
 public static async Task Main(string[] args) {
  var engine=new NativeEngine();var ui=new LocalUi(engine);await ui.Start();
  if(args.Contains("--headless"))Console.WriteLine(new JsonObject{["url"]=ui.Url,["key"]=ui.Key}.ToJsonString());
  else {Console.WriteLine("ميدان التحدي — أبقِ هذه النافذة مفتوحة أثناء المسابقة.");Console.WriteLine(ui.Url);try{Process.Start(new ProcessStartInfo(ui.Url){UseShellExecute=true});}catch{Console.WriteLine("افتح الرابط أعلاه في المتصفح.");}}
  Console.CancelKeyPress+=(_,e)=>{e.Cancel=true;ui.Dispose();engine.Dispose();Environment.Exit(0);};
  await Task.Delay(Timeout.Infinite);
 }
}
public sealed class EventHub {
 readonly ConcurrentQueue<JsonObject> queue=new();readonly SemaphoreSlim signal=new(0);
 public void Emit(JsonObject item){queue.Enqueue(item);signal.Release();while(queue.Count>1024)queue.TryDequeue(out _);}
 public async Task<JsonArray> Poll(){await signal.WaitAsync(1000);var result=new JsonArray();while(queue.TryDequeue(out var item)){result.Add((JsonNode)item);signal.Wait(0);}return result;}
}
public sealed class LocalUi:IDisposable {
 readonly HttpListener http=new();readonly NativeEngine engine;readonly StateStore storage=new();public string Key{get;}=Convert.ToHexString(RandomNumberGenerator.GetBytes(24));public string Url{get;private set;}="";
 public LocalUi(NativeEngine engine){this.engine=engine;}
 public Task Start(){var probe=new TcpListener(IPAddress.Loopback,0);probe.Start();int port=((IPEndPoint)probe.LocalEndpoint).Port;probe.Stop();Url=$"http://127.0.0.1:{port}";http.Prefixes.Add(Url+"/");http.Start();_ = Task.Run(Loop);return Task.CompletedTask;}
 async Task Loop(){while(http.IsListening){try{var ctx=await http.GetContextAsync();_ = Handle(ctx);}catch(HttpListenerException){break;}}}
 async Task Handle(HttpListenerContext ctx){try{var request=ctx.Request;var response=ctx.Response;response.Headers["X-Content-Type-Options"]="nosniff";response.Headers["Cache-Control"]="no-store";response.Headers["Content-Security-Policy"]="default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self'; connect-src 'self'; object-src 'none'; frame-ancestors 'none'";
  if(request.Headers["Origin"] is string origin&&origin!=Url){response.StatusCode=403;return;}
  var path=request.Url?.AbsolutePath??"/";
  if(path=="/command"||path=="/events"){if(request.Headers["X-Meydan-Key"]!=Key){response.StatusCode=403;return;}if(path=="/events"){if(request.HttpMethod!="GET"){response.StatusCode=405;return;}await Write(response,(await engine.Events.Poll()).ToJsonString(),"application/json");return;}
   if(request.HttpMethod!="POST"){response.StatusCode=405;return;}if(request.ContentLength64<0||request.ContentLength64>12*1024*1024){response.StatusCode=413;return;}using var reader=new StreamReader(request.InputStream,new UTF8Encoding(false,true));string body=await reader.ReadToEndAsync();var command=JsonNode.Parse(body)?.AsObject()??throw new InvalidDataException();if(command["action"]?.GetValue<string>()=="store")storage.Set(command["key"]!.GetValue<string>(),command["value"]!.GetValue<string>());else await engine.Command(command);await Write(response,"{}","application/json");return;}
  if(request.HttpMethod!="GET"){response.StatusCode=405;return;}string name=path=="/"?"index.html":path.TrimStart('/');if(name.Contains('/')||name.Contains('\\')||name.Contains("..")){response.StatusCode=404;return;}string asset=Path.Combine(AppContext.BaseDirectory,"assets",name);if(!File.Exists(asset)){response.StatusCode=404;return;}string text=await File.ReadAllTextAsync(asset);if(name=="index.html")text=text.Replace("//DESKTOP_BOOTSTRAP","window.DESKTOP_TOKEN="+JsonValue.Create(Key)!.ToJsonString()+";window.DESKTOP_STORAGE="+storage.Snapshot()+";");string mime=Path.GetExtension(name) switch{".html"=>"text/html",".css"=>"text/css",".js"=>"text/javascript",".json"=>"application/json",_=>"text/plain"};await Write(response,text,mime);
 }catch(Exception){ctx.Response.StatusCode=400;}finally{ctx.Response.Close();}}
 static async Task Write(HttpListenerResponse response,string text,string mime){var bytes=Encoding.UTF8.GetBytes(text);response.ContentType=mime+"; charset=utf-8";response.ContentLength64=bytes.Length;await response.OutputStream.WriteAsync(bytes);}
 public void Dispose(){http.Close();}
}
public sealed class Peer:IDisposable {
 public readonly Stream Stream;readonly IDisposable owner;readonly SemaphoreSlim sendLock=new(1);public Peer(Stream stream,IDisposable owner){Stream=stream;this.owner=owner;}
 public async Task Send(string line){if(Encoding.UTF8.GetByteCount(line)>FrameReader.Max||line.Contains('\n'))throw new InvalidDataException();await sendLock.WaitAsync();try{await Stream.WriteAsync(Encoding.UTF8.GetBytes(line+"\n"));await Stream.FlushAsync();}finally{sendLock.Release();}}
 public void Dispose(){owner.Dispose();}
}
public static class FrameReader {
 public const int Max=65536;
 public static async Task<string?> Read(Stream stream,CancellationToken token){using var buffer=new MemoryStream();byte[] one=new byte[1];while(await stream.ReadAsync(one,token)>0){if(one[0]==10)return new UTF8Encoding(false,true).GetString(buffer.ToArray());if(buffer.Length>=Max)throw new InvalidDataException("Frame too large");buffer.WriteByte(one[0]);}if(buffer.Length>0)throw new InvalidDataException("Truncated frame");return null;}
}
public sealed class NativeEngine:IDisposable {
 public readonly EventHub Events=new();readonly ConcurrentDictionary<string,Peer> peers=new();TcpListener? tcp;BluetoothListener? bt;ClientWebSocket? relay;CancellationTokenSource cancellation=new();readonly SemaphoreSlim relaySend=new(1);static readonly Guid Service=new("7591c0c6-2f53-4f19-a904-bef577040ba2");
 void Emit(string type,string? peerId=null,string? line=null,string? address=null){var e=new JsonObject{["type"]=type};if(peerId!=null)e["peerId"]=peerId;if(line!=null)e["line"]=line;if(address!=null)e["address"]=address;Events.Emit(e);}
 void Error(Exception e){Events.Emit(new JsonObject{["type"]="error",["message"]="تعذر الاتصال: "+e.Message});}
 public async Task Command(JsonObject c){try{string action=c["action"]?.GetValue<string>()??"";if(action=="send"){string id=c["peerId"]!.GetValue<string>(),line=c["line"]!.GetValue<string>();if(Encoding.UTF8.GetByteCount(line)>FrameReader.Max)throw new InvalidDataException();if(relay?.State==WebSocketState.Open){await SendRelay(new JsonObject{["type"]="send",["peerId"]=id,["line"]=line});return;}if(peers.TryGetValue(id,out var p))await p.Send(line);return;}if(action=="stop"){Stop();return;}if(action=="devices"){if(!OperatingSystem.IsWindows())throw new PlatformNotSupportedException("البلوتوث يحتاج ويندوز");await Task.Run(()=>{using var client=new BluetoothClient();var list=new JsonArray();foreach(var d in client.PairedDevices)list.Add((JsonNode)new JsonObject{["name"]=d.DeviceName,["address"]=d.DeviceAddress.ToString()});Events.Emit(new JsonObject{["type"]="devices",["devices"]=list});});return;}
  Stop();var ct=cancellation.Token;bool host=action=="host";string mode=c["mode"]?.GetValue<string>()??"lan";if(mode=="online"){_ = ConnectRelay(c,host,ct);return;}if(mode=="bt"){if(!OperatingSystem.IsWindows())throw new PlatformNotSupportedException("البلوتوث يحتاج ويندوز ومحولًا متوافقًا");if(host){bt=new BluetoothListener(Service);bt.ServiceName="Meydan Challenge";bt.Start();var listener=bt;Emit("hosting",address:"Bluetooth · Windows");_ = Task.Run(()=>{try{while(!ct.IsCancellationRequested){var client=listener.AcceptBluetoothClient();Attach(Guid.NewGuid().ToString(),client.GetStream(),client,ct,false);}}catch(Exception ex){if(!ct.IsCancellationRequested)Error(ex);}});}else{_ = Task.Run(()=>{try{var client=new BluetoothClient();client.Connect(BluetoothAddress.Parse(c["address"]!.GetValue<string>()),Service);Attach("host",client.GetStream(),client,ct,true);}catch(Exception ex){if(!ct.IsCancellationRequested)Error(ex);}});}return;}
  if(host){tcp=new TcpListener(IPAddress.Any,45871);tcp.Start();var listener=tcp;var addresses=Dns.GetHostAddresses(Dns.GetHostName()).Where(a=>a.AddressFamily==AddressFamily.InterNetwork&&!IPAddress.IsLoopback(a)).Select(a=>a+":45871");Emit("hosting",address:string.Join(" / ",addresses));_ = Task.Run(async()=>{try{while(!ct.IsCancellationRequested){var client=await listener.AcceptTcpClientAsync(ct);client.NoDelay=true;Attach(Guid.NewGuid().ToString(),client.GetStream(),client,ct,false);}}catch(Exception ex){if(!ct.IsCancellationRequested)Error(ex);}});}else{_ = Task.Run(async()=>{try{var parts=c["address"]!.GetValue<string>().Split(':');var client=new TcpClient();await client.ConnectAsync(parts[0],parts.Length>1?int.Parse(parts[1]):45871,ct);client.NoDelay=true;Attach("host",client.GetStream(),client,ct,true);}catch(Exception ex){if(!ct.IsCancellationRequested)Error(ex);}});}
 }catch(Exception ex){Error(ex);}}
 public static bool RemoveExact(ConcurrentDictionary<string,Peer> peers,string id,Peer expected)=>((ICollection<KeyValuePair<string,Peer>>)peers).Remove(new KeyValuePair<string,Peer>(id,expected));
 void Attach(string id,Stream stream,IDisposable owner,CancellationToken ct,bool client){if(ct.IsCancellationRequested){owner.Dispose();return;}var peer=new Peer(stream,owner);peers[id]=peer;Emit(client?"connected":"peer",id);_ = Task.Run(async()=>{try{long window=Environment.TickCount64;int count=0;while(!ct.IsCancellationRequested){using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(30000);var line=await FrameReader.Read(stream,timeout.Token);if(line==null||ct.IsCancellationRequested)break;if(Environment.TickCount64-window>2000){window=Environment.TickCount64;count=0;}if(++count>120)throw new InvalidDataException("Rate limit");Emit("line",id,line);}}catch(Exception){}finally{bool removed=RemoveExact(peers,id,peer);peer.Dispose();if(removed&&!ct.IsCancellationRequested)Emit("disconnected",id);}});}
 async Task ConnectRelay(JsonObject c,bool host,CancellationToken ct){try{var url=c[host?"relay":"address"]!.GetValue<string>();if(!url.StartsWith("wss://",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("يجب استخدام wss");var socket=new ClientWebSocket();relay=socket;socket.Options.KeepAliveInterval=TimeSpan.FromSeconds(10);await socket.ConnectAsync(new Uri(url),ct);await SendRelay(new JsonObject{["type"]="register",["role"]=host?"host":"player",["room"]=c["room"]?.DeepClone(),["key"]=host?c["key"]?.DeepClone():JsonValue.Create("")});var buffer=new byte[8192];while(socket.State==WebSocketState.Open&&!ct.IsCancellationRequested){using var data=new MemoryStream();WebSocketReceiveResult r;do{r=await socket.ReceiveAsync(buffer,ct);if(r.MessageType==WebSocketMessageType.Close)return;if(r.MessageType!=WebSocketMessageType.Text||data.Length+r.Count>150000)throw new InvalidDataException();data.Write(buffer,0,r.Count);}while(!r.EndOfMessage);var m=JsonNode.Parse(Encoding.UTF8.GetString(data.ToArray()))!.AsObject();string t=m["type"]?.GetValue<string>()??"";if(t=="ready")Emit(host?"hosting":"connected",address:"Internet");else if(t=="data")Emit("line",m["peerId"]?.GetValue<string>()??"host",m["line"]!.GetValue<string>());else if(t=="peer"||t=="disconnected")Events.Emit(m);else if(t=="error")Events.Emit(new JsonObject{["type"]="error",["message"]=m["reason"]?.DeepClone()});}}
 catch(Exception ex){if(!ct.IsCancellationRequested)Error(ex);}finally{if(!ct.IsCancellationRequested)Emit("disconnected");}}
 async Task SendRelay(JsonObject m){await relaySend.WaitAsync();try{var bytes=Encoding.UTF8.GetBytes(m.ToJsonString());await relay!.SendAsync(bytes,WebSocketMessageType.Text,true,cancellation.Token);}finally{relaySend.Release();}}
 void Stop(){cancellation.Cancel();try{tcp?.Stop();}catch{}tcp=null;try{bt?.Stop();}catch{}bt=null;relay?.Abort();relay?.Dispose();relay=null;foreach(var p in peers.Values)p.Dispose();peers.Clear();cancellation.Dispose();cancellation=new();}
 public void Dispose(){Stop();cancellation.Dispose();}
}
