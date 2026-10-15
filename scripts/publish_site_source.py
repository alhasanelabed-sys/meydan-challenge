"""Fallback source synchronizer when the packaged Sites helper is unavailable.
Credentials arrive on hidden stdin; never persist or print the token.
"""
import sys,os,json,termios,subprocess,tarfile
from pathlib import Path
checkout=Path(sys.argv[1]).resolve();archive=Path(sys.argv[2]).resolve()
state=termios.tcgetattr(sys.stdin.fileno());hidden=state.copy();hidden[3]&=~termios.ECHO;termios.tcsetattr(sys.stdin.fileno(),termios.TCSANOW,hidden)
try:
 print('Ready for Site workflow JSON on stdin (input is hidden).',flush=True)
 data=json.loads(sys.stdin.readline())
finally:termios.tcsetattr(sys.stdin.fileno(),termios.TCSANOW,state)
c=data['credential'];manifest=json.loads((checkout/'.openai/hosting.json').read_text())
assert manifest['project_id']==data['project_id']
assert c['remote_url'].startswith('https://') and '@' not in c['remote_url']
assert c['auth_mode']=='http_extra_header'
def run(args,env=None):
 result=subprocess.run(args,cwd=checkout,env=env,text=True,capture_output=True,timeout=120)
 if result.returncode:
  raise RuntimeError(result.stderr[:2000])
 return result.stdout.strip()
if not (checkout/'.git').exists():run(['git','init','-b',c['branch']])
run(['git','config','user.name','Meydan Site Build']);run(['git','config','user.email','build@meydan.local'])
if 'origin' not in run(['git','remote']).splitlines():run(['git','remote','add','origin',c['remote_url']])
else:run(['git','remote','set-url','origin',c['remote_url']])
run(['git','add','.'])
changed=subprocess.run(['git','diff','--cached','--quiet'],cwd=checkout).returncode!=0
if changed:run(['git','commit','-m','Publish verified Meydan landing page and downloads'])
commit=run(['git','rev-parse','HEAD'])
env=os.environ.copy();env.update({'GIT_TERMINAL_PROMPT':'0','GIT_CONFIG_COUNT':'1','GIT_CONFIG_KEY_0':'http.'+c['remote_url']+'.extraHeader','GIT_CONFIG_VALUE_0':'Authorization: Bearer '+c['token']})
run(['git','push','origin','HEAD:'+c['branch']],env=env)
archive.parent.mkdir(parents=True,exist_ok=True)
with tarfile.open(archive,'w:gz') as tar:
 for name in ('.openai/hosting.json','dist'):tar.add(checkout/name,arcname=name)
print(json.dumps({'project_id':manifest['project_id'],'commit_sha':commit,'archive':str(archive)}),flush=True)
