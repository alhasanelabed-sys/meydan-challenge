from pathlib import Path
import json
p=Path(__file__).resolve().parents[1]/'windows/obj/project.assets.json'
d=json.loads(p.read_text());matches=[]
for target,libs in d['targets'].items():
 if '/win-x64' not in target:continue
 for name,info in libs.items():
  if name.startswith('InTheHand.Net.Bluetooth/'):
   matches.extend(x for x in info.get('runtime',{}) if x.endswith('InTheHand.Net.Bluetooth.dll'))
assert matches and all('net8.0-windows' in path for path in matches),f'Windows Bluetooth provider not selected: {matches}'
print('PASS: Windows-specific Bluetooth implementation selected:',*matches)
