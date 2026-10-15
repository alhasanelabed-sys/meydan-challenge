from pathlib import Path
import zipfile,shutil,hashlib,json
root=Path(__file__).resolve().parents[1];out=root/'artifacts';out.mkdir(exist_ok=True)
windows=out/'windows'
shutil.copy2(root/'docs/guide-ar.md',windows/'README-ar.md')
shutil.copy2(root/'shared/questions.json',windows/'demo-questions.json')
with zipfile.ZipFile(out/'meydan-windows-portable.zip','w',zipfile.ZIP_DEFLATED,compresslevel=9) as z:
 for p in sorted(windows.rglob('*')):
  if p.is_file():z.write(p,Path('Meydan')/p.relative_to(windows))
shutil.copy2(root/'shared/questions.json',out/'meydan-demo-questions.json')
files=['meydan-android.apk','meydan-windows-portable.zip','meydan-demo-questions.json']
checks=[]
for name in files:
 p=out/name
 if p.exists():checks.append(hashlib.sha256(p.read_bytes()).hexdigest()+'  '+name)
(out/'SHA256SUMS.txt').write_text('\n'.join(checks)+'\n')
print('\n'.join(f'{name}: {(out/name).stat().st_size:,} bytes' for name in files if (out/name).exists()))
