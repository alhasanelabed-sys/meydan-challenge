(function(root,factory){const api=factory();if(typeof module==='object')module.exports=api;else root.MeydanCapacity=api;})(globalThis,function(){
class Capacity{
 constructor(kind){this.kind=kind;this.limit=kind==='bt'?4:32;this.calibrating=false;this.data=new Map();this.verified=0;}
 observe(id,rtt,now){if(!Number.isFinite(rtt)||rtt<0||rtt>60000)return;let p=this.data.get(id);if(!p){p={first:now,last:now,samples:[]};this.data.set(id,p);}p.last=now;p.samples.push({at:now,rtt});p.samples=p.samples.filter(x=>x.at>=now-30000);}
 remove(id){this.data.delete(id);}
 report(now){const peers=[],rtts=[];let expected=0,received=0,stable=0;for(const [id,p] of this.data){const samples=p.samples.filter(x=>x.at>=now-30000);const n=Math.floor((now-Math.max(p.first,now-30000))/2000)+1;expected+=Math.max(n,1);received+=Math.min(samples.length,n);samples.forEach(s=>rtts.push(s.rtt));const online=now-p.last<=10000;peers.push({id,online,rtt:samples.at(-1)?.rtt||0});if(online&&now-p.first>=30000)stable++;}rtts.sort((a,b)=>a-b);const p95=rtts.length?rtts[Math.max(0,Math.ceil(rtts.length*.95)-1)]:0;const loss=expected?Math.max(0,1-received/expected):0;const healthy=p95<=1000&&loss<=.1&&peers.every(p=>p.online);if(healthy&&peers.length&&stable===peers.length)this.verified=Math.max(this.verified,stable);return {limit:this.limit,verified:this.verified,physicalMaximum:null,healthy,p95,loss,peers,calibrating:this.calibrating};}
 canAdmit(now,count=0){const r=this.report(now);if(!r.healthy)return false;if(count<this.limit)return true;if(this.calibrating&&r.verified>=count&&count===this.limit&&r.peers.length===count){this.limit=count+1;return true;}return false;}
}
return {Capacity};});
