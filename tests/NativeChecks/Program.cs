using System.Collections.Concurrent;using Meydan;
var peers=new ConcurrentDictionary<string,Peer>();var oldStream=new MemoryStream();var newStream=new MemoryStream();var oldPeer=new Peer(oldStream,oldStream);var replacement=new Peer(newStream,newStream);peers["host"]=replacement;
if(NativeEngine.RemoveExact(peers,"host",oldPeer))throw new Exception("Stale cleanup removed replacement");
if(!ReferenceEquals(peers["host"],replacement))throw new Exception("Replacement lost");
if(!NativeEngine.RemoveExact(peers,"host",replacement)||peers.ContainsKey("host"))throw new Exception("Current cleanup failed");
Console.WriteLine("PASS: stale peer cleanup cannot remove resumed connection");
