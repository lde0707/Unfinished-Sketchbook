"""Remove the rejected row from project source sheets, preserving other rows verbatim."""
from pathlib import Path
from PIL import Image
P=Path(__file__).resolve().parent
for path in (P/'sources').glob('*.png'):
 im=Image.open(path).convert('RGBA');w,h=im.size
 if w/h>1.2:continue
 rows=[im.crop((0,round(r*h/4),w,round((r+1)*h/4))) for r in (0,1,3)]
 out=Image.new('RGBA',(w,sum(r.height for r in rows)));y=0
 for row in rows:out.paste(row,(0,y));y+=row.height
 out.save(path)
print('Project source sheets retain only Move / Attack / Death rows.')
