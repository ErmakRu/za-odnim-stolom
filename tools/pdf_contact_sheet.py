from pathlib import Path
from PIL import Image,ImageOps,ImageDraw
root=Path(__file__).resolve().parents[1]/'tmp/pdfs'
files=sorted(root.glob('handbook-*.png'))
for start in range(0,len(files),12):
 sheet=Image.new('RGB',(900,4*444),'#d9dfe5')
 for j,p in enumerate(files[start:start+12]):
  im=Image.open(p).convert('RGB');im.thumbnail((280,412))
  x=(j%3)*300+10;y=(j//3)*444+22
  sheet.paste(im,(x,y));ImageDraw.Draw(sheet).text((x,y-17),p.stem,fill='black')
 sheet.save(root/f'contact-{start//12+1}.jpg')
print(len(files),'pages rendered')
