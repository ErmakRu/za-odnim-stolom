"""Read-only source-art QA and thumbnail contact sheets; never modifies card PNGs."""
import json
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'SummonersTable/Assets'
OUT = ROOT / 'output/layered-cards-all'
LAYERS = ASSETS / 'LayeredCards/Resources/LayeredCards/Layers'
cards = json.loads((ASSETS / 'StreamingAssets/Config/cards.json').read_text(encoding='utf-8-sig'))['cards']
cards = [c for c in cards if c['id'] not in ('C02', 'C08', 'S01')]
OUT.mkdir(parents=True, exist_ok=True)
report = []
for page in range(3):
    sheet = Image.new('RGB', (1440, 570), '#19252d')
    draw = ImageDraw.Draw(sheet)
    for i, card in enumerate(cards[page*9:(page+1)*9]):
        card_id=card['id'];x=(i%3)*480;y=(i//3)*190
        draw.text((x+5,y+5),card_id+'   original / subject / background',fill='white')
        for j,part in enumerate(('original','subject','background')):
            path=ASSETS/'Resources/Art'/f'{card_id}.png' if part=='original' else LAYERS/card_id/f'{part}.png'
            assert path.is_file(), str(path)
            with Image.open(path) as source:
                image=source.convert('RGBA')
                if part!='original':
                    hist=image.getchannel('A').histogram();area=image.width*image.height
                    if part=='subject':
                        assert hist[0]/area>.02 and sum(hist[64:])/area>.05, f'Invalid alpha {card_id}'
                    else:
                        assert hist[255]==area, f'Nonopaque background {card_id}'
                    report.append(dict(card=card_id,layer=part,width=image.width,height=image.height,
                        transparent=round(hist[0]/area,4),translucent=round(sum(hist[1:255])/area,4)))
                image.thumbnail((156,156))
                backdrop=Image.new('RGBA',(156,156),'#33404e')
                grid=ImageDraw.Draw(backdrop)
                for cy in range(0,156,12):
                    for cx in range(0,156,12):
                        if (cx//12+cy//12)%2:grid.rectangle((cx,cy,cx+11,cy+11),fill='#566577')
                backdrop.alpha_composite(image,((156-image.width)//2,(156-image.height)//2))
                sheet.paste(backdrop.convert('RGB'),(x+j*160,y+27))
    sheet.save(OUT/f'plates-{page+1}.jpg',quality=94)
(OUT/'plate-verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(f'PASS: {len(report)} new PNG plates; 27 transparent subjects and 27 opaque backgrounds.')
