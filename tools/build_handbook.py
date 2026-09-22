"""Generate the handbook from the same catalogue the game reads."""
import json, hashlib, io
from pathlib import Path
from xml.sax.saxutils import escape
from reportlab.pdfgen import canvas
from reportlab.lib.colors import HexColor, Color, white
from reportlab.lib.pagesizes import A4
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import Paragraph
from reportlab.lib.styles import ParagraphStyle
from pypdf import PdfReader
from PIL import Image
from reportlab.lib.utils import ImageReader

ROOT=Path(__file__).resolve().parents[1]
DATA=ROOT/'SummonersTable/Assets/Resources/Data/catalog.json'
ART=ROOT/'SummonersTable/Assets/Resources/Art'
OUT=ROOT/'output/pdf/arena-test-decks-v0.5.pdf'
D=json.loads(DATA.read_text(encoding='utf-8'))
CARDS={c['id']:c for c in D['cards']}
TYPES={c['id']:c for c in D['typeColors']}
ROLES={c['name']:c['hex'] for c in D['roleColors']}
pdfmetrics.registerFont(TTFont('Body',r'C:\Windows\Fonts\arial.ttf'))
pdfmetrics.registerFont(TTFont('Bold',r'C:\Windows\Fonts\arialbd.ttf'))
OUT.parent.mkdir(parents=True,exist_ok=True)
C=canvas.Canvas(str(OUT),pagesize=A4)
C.setTitle('Арена призыва - тестовые колоды 0.5.0')
W,H=A4
INK=HexColor('#243144'); MUTED=HexColor('#657285'); TEAL=HexColor('#168A79')
page=0
def embedded(path,limit=(500,500)):
 im=Image.open(path).convert('RGB');im.thumbnail(limit,Image.Resampling.LANCZOS)
 stream=io.BytesIO();im.save(stream,format='JPEG',quality=88);stream.seek(0)
 return ImageReader(stream)
def clean(s): return s.replace('—','-').replace('–','-').replace('−','-').replace('\u2011','-')
def para(text,x,y,width,size=11,color=INK,bold=False):
 p=Paragraph(escape(clean(text)).replace('\n','<br/>'),ParagraphStyle('p',fontName='Bold' if bold else 'Body',fontSize=size,leading=size*1.4,textColor=color))
 _,height=p.wrap(width,1000)
 if y-height<38:raise ValueError(f'Page {page} overflow: {text[:40]}')
 p.drawOn(C,x,y-height)
 return y-height
def begin(title,subtitle=''):
 global page
 if page:C.showPage()
 page+=1
 C.setFillColor(HexColor('#F5F6F8'));C.rect(0,0,W,H,fill=1,stroke=0)
 C.setFillColor(TEAL);C.rect(0,H-9,W,9,fill=1,stroke=0)
 C.setFont('Bold',9);C.setFillColor(MUTED);C.drawString(38,H-36,'АРЕНА ПРИЗЫВА / ТЕСТ 0.5.0')
 C.setFont('Body',8);C.drawRightString(W-38,24,f'{page:02d} / За одним столом')
 C.setStrokeColor(HexColor('#DDE1E8'));C.line(38,39,W-38,39)
 y=para(title,38,H-58,W-76,25,bold=True)
 if subtitle:y=para(subtitle,38,y-10,W-76,10,MUTED)
 return y-24
def block(title,text,y):
 y=para(title,38,y,W-76,13,TEAL,True)-6
 return para(text,38,y,W-76,10.5)-18

begin('За одним столом','Правила и все карты тестовых колод')
C.drawImage(embedded(ART/'menu.png',(1400,900)),0,H-365,width=W,height=W*9/16,mask='auto')
C.setFillColor(HexColor('#132A30'));C.rect(0,H-366,W,100,fill=1,stroke=0)
para('АРЕНА ПРИЗЫВА',38,H-287,W-76,30,white,True)
para('Ритуалы, подколы и три готовые колоды',38,H-329,W-76,13,HexColor('#C5E7DE'))
y=H-400
y=block('2-4 игрока / 1 раунд / 30 уникальных карт','Каждый играет за себя. В каждой готовой колоде 30 экземпляров: 15 существ, 12 заклинаний и 3 реакции. Временная помощь соперникам разрешена.',y)
y=block('Главное правило','Существа и заклинания проходят QTE. Реакции играются без него. После QTE существо ждёт назначения цели. Кнопка «Закончить ход» запускает атаки; после первой атаки существо опускается на стол и включает постоянный эффект.',y)
y=block('Как пользоваться справочником','Далее: правила, цветовые метки, стратегии и составы колод, все 30 карт. ID C01-R04 позволяют обсуждать правки без путаницы с названиями.',y)
para('Версия баланса 0.5.0: исходный набор для тестов. Игра и PDF используют один каталог данных.',38,y,W-76,10,MUTED)

y=begin('Как проходит матч')
for title,txt in [
 ('Цель и очки','Если все оставшиеся герои погибают одновременно, им по 1 очку за ничью. Один раунд. Последний выживший получает 3 очка; устранение вражеского героя приносит 1 очко источнику урона. После матча можно сыграть ещё раз, выбрать другую колоду в лобби или выйти в меню. Повтор начинается, когда все подтвердят готовность; счёт сбрасывается. Побеждает максимальный общий счёт; при равенстве победа общая.'),
 ('Начало раунда','У каждого 30 HP, 30 карт в колоде, 5 стартовых карт и 5 ячеек поля. Рука вмещает 8 карт; лишние сгорают. В начале хода - добор одной карты и эффекты начала хода от ваших существ.'),
 ('Ваш ход','Нажмите своё существо и укажите стрелкой врага или центр стола. Стрелки сохраняются между ходами. Можно сыграть 1 существо и до 1 заклинания либо до 3 заклинаний без существа. После второго заклинания призыв недоступен. Реакции не расходуют эти лимиты. На решения отведено 45 секунд, при розыгрыше этот таймер приостанавливается.'),
 ('Боевая фаза','QTE не завершает ход. Нажмите «Закончить ход» (или дождитесь тайм-аута решений). Готовые существа, включая новый призыв, атакуют по порядку слотов. Каждое бьёт один раз. Кнопка светится, если цели назначены и доступных розыгрышей больше нет; нажать можно раньше. Без назначенной цели выбирается герой случайного живого противника. Если назначенное существо уже погибло, цель также заменяется случайным героем.'),
 ('Добор и усталость','Если за ход не запускался ни один QTE, в конце хода доберите ещё карту. Сорванная попытка считается QTE. Пустая колода наносит 1, затем 2, затем 3 HP и так далее за каждый несостоявшийся добор. Защита не отменяет усталость.')]:y=block(title,txt,y)

y=begin('Ритуал, атака и реакции')
for title,txt in [
 ('QTE','Нажимайте показанные клавиши A S D F G H J или экранные кнопки. Базовый таймер: 10 + (длина - 2) x 3,3 секунды. Промах отнимает 2 секунды; третий промах или конец таймера срывает карту. Постоянные эффекты могут менять длину, время или прощать ошибку.'),
 ('Успешный призыв','Выберите существо и свободный слот, либо перетащите карту на слот. Карта показывается всем 2 секунды. Владелец вводит QTE; соперники видят только огоньки прогресса и оставшихся попыток. После успеха карта опускается в слот, а показ слева исчезает. Игрок сам тянет стрелку от существа к цели. Оно атакует в конце хода, получает ответный урон от существа и, если выживает, включает свой постоянный эффект.'),
 ('Срыв','В начале QTE владелец видит случайного живого противника - получателя при срыве. Провал отдаёт ему карту в руку, без атаки и эффекта. При полной руке карта сгорает. В дуэли получатель всегда второй игрок.'),
 ('Реакции','По одной реакции от соперника во время показа карты или QTE, без своего QTE. На призыв: R02 копирует в такой же свободный слот, R03 возвращает исходную карту в руку. На заклинание: R01 уменьшает урон, R04 отменяет эффект для своей цели. Эффекты срабатывают после успешного QTE; при срыве реакция потрачена. На атаки существ реакций нет.'),
 ('Постоянные эффекты','Новый призыв включает эффект после первой атаки. Копия R02 - сразу, но атакует только с хода своего владельца. Возврат в руку снимает эффект. Пределы складывания указаны на картах. Смерть героя убирает его существ. В локальной игре таймер QTE приостанавливается, пока другие выбирают реакции (до 8 секунд на ответ).')]:y=block(title,txt,y)
para('Заклинание: цель до QTE, эффект сразу после успеха; центр выбирает случайную допустимую цель. Существо: цель после QTE, атака в конце хода; центр выбирает вражеского героя. Заклинание исчезает слева после QTE. Камера: колесо и ПКМ. ESC: общая громкость и эффекты.',38,y,W-76,10,MUTED)

y=begin('Цвета и роли','Цвет всегда продублирован подписью типа или роли.')
for t in D['typeColors']:
 C.setFillColor(HexColor(t['hex']));C.roundRect(38,y-24,22,22,5,fill=1,stroke=0)
 y=para(t['name']+(' - QTE обязателен' if t['id']!='reaction' else ' - без QTE'),72,y-2,W-110,12,bold=True)-15
y=block('Дополнительная метка существа','Рамка обозначает тип карты. Отдельная цветная полоска - роль существа. Фракция указана текстом и не ограничивает роль.',y-10)
desc={'Защитник':'Уменьшает урон.','Лекарь':'Восстанавливает HP.','Снабженец':'Помогает получать карты.','Усилитель':'Повышает урон.','Ритуалист':'Облегчает QTE.','Вредитель':'Мешает соперникам.','Провокатор':'Наказывает за нападение.'}
for r in D['roleColors']:
 C.setFillColor(HexColor(r['hex']));C.roundRect(38,y-20,16,16,4,fill=1,stroke=0)
 y=para(r['name']+' - '+desc[r['name']],66,y-3,W-104,10.5)-10
y=block('У каждой фракции - три роли','\n'.join(f+': '+', '.join(c['role'].lower() for c in D['cards'] if c['faction']==f) for f in ['Звери','Демоны','Нежить','Механоиды','Драконы','Духи']),y-12)

for d in D['decks']:
 y=begin(d['name'],d['subtitle']+' / 30 карт: 15 существ, 12 заклинаний, 3 реакции')
 y=para(d['guide'],38,y,W-76,11)-20
 for kind in ['creature','spell','reaction']:
  n=sum(e['count'] for e in d['entries'] if CARDS[e['cardId']]['kind']==kind)
  C.setFillColor(HexColor(TYPES[kind]['hex']));C.roundRect(38,y-14,10,10,2,fill=1,stroke=0)
  y=para(TYPES[kind]['name']+f' / {n} карт',55,y,W-93,12,INK,True)-10
  for e in d['entries']:
   c=CARDS[e['cardId']]
   if c['kind']!=kind:continue
   txt=f"{c['id']}  {c['name']}"+(f"  {c['attack']}/{c['health']}" if kind=='creature' else '')
   y=para(txt,45,y,W-120,10)-5
   C.setFont('Bold',10);C.setFillColor(MUTED);C.drawRightString(W-40,y+7,'x'+str(e['count']))
  y-=14
 para('Совет: устраняйте опасные постоянные эффекты. На троих и четверых помощь сопернику может сохранить полезного временного союзника.',38,100,W-76,10,MUTED)

for start in range(0,len(D['cards']),2):
 y=begin('Каталог карт',f'{start+1:02d}-{min(start+2,30):02d} из 30 / значения совпадают с каталогом игры')
 for c in D['cards'][start:start+2]:
  top=y;bottom=top-292;color=HexColor(TYPES[c['kind']]['hex'])
  C.setFillColor(white);C.setStrokeColor(HexColor('#DCE2E9'));C.roundRect(38,bottom,W-76,292,10,fill=1,stroke=1)
  C.setFillColor(color);C.roundRect(38,top-8,W-76,8,3,fill=1,stroke=0)
  q=para(c['id']+' / '+TYPES[c['kind']]['name'],52,top-20,330,9,MUTED,True)
  q=para(c['name'],52,q-8,325,17,bold=True)
  meta=f"{c['faction']} / {c['role']}" if c['kind']=='creature' else ('Разовый эффект' if c['kind']=='spell' else 'Ответ во время розыгрыша')
  if c['kind']=='creature':
   C.setFillColor(HexColor(ROLES[c['role']]));C.roundRect(52,q-17,8,8,2,fill=1,stroke=0)
  para(meta,66 if c['kind']=='creature' else 52,q-6,310,9,INK)
  art=ART/(c['id']+'.png')
  if art.exists():C.drawImage(embedded(art),W-150,top-136,98,98,preserveAspectRatio=True,anchor='c',mask='auto')
  else:
   C.setFillColor(Color(color.red,color.green,color.blue,alpha=.14));C.roundRect(W-150,top-136,98,98,8,fill=1,stroke=0)
   para(c['id'],W-138,top-74,74,20,color,True)
  stat=f"АТАКА {c['attack']} / HP {c['health']} / QTE {c['qte']}" if c['kind']=='creature' else (f"QTE {c['qte']}" if c['kind']=='spell' else 'БЕЗ QTE')
  para(stat,52,top-113,330,10,bold=True)
  q=para(c['rules'],52,top-153,W-104,10.5)
  if q<bottom+49:raise ValueError('Card rules overflow: '+c['id'])
  para(c['flavor'],52,bottom+39,W-104,9,MUTED)
  y=bottom-18
C.save()
reader=PdfReader(str(OUT));text='\n'.join(p.extract_text() for p in reader.pages)
for c in D['cards']:assert c['id'] in text and clean(c['name']) in text,c['id']
print(json.dumps(dict(path=str(OUT),pages=len(reader.pages),cards=30,catalogueSha256=hashlib.sha256(DATA.read_bytes()).hexdigest()),ensure_ascii=False))
