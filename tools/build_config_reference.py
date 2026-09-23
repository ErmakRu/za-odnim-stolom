"""Create the configuration reference from shipped JSON and verify field coverage."""
import json, re, hashlib
from pathlib import Path
from xml.sax.saxutils import escape
from reportlab.pdfgen import canvas
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, PageBreak, KeepTogether
from reportlab.lib import colors
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from pypdf import PdfReader
ROOT=Path(__file__).resolve().parents[1]
CFG=ROOT/'SummonersTable/Assets/StreamingAssets/Config'
OUT=ROOT/'output/pdf/config-reference-v0.7.pdf'
OUT.parent.mkdir(parents=True,exist_ok=True)
pdfmetrics.registerFont(TTFont('Arial',r'C:\Windows\Fonts\arial.ttf'))
pdfmetrics.registerFont(TTFont('ArialBold',r'C:\Windows\Fonts\arialbd.ttf'))
styles=getSampleStyleSheet()
styles.add(ParagraphStyle(name='BodyRU',fontName='Arial',fontSize=10,leading=14,spaceAfter=8,textColor=colors.HexColor('#223245')))
styles.add(ParagraphStyle(name='SmallRU',parent=styles['BodyRU'],fontSize=8,leading=11,spaceAfter=0,wordWrap='CJK'))
styles.add(ParagraphStyle(name='HeadRU',fontName='ArialBold',fontSize=24,leading=29,spaceAfter=14,textColor=colors.HexColor('#173743'),keepWithNext=True))
styles.add(ParagraphStyle(name='SubRU',fontName='ArialBold',fontSize=13,leading=18,spaceBefore=14,spaceAfter=8,textColor=colors.HexColor('#13887b'),keepWithNext=True))
styles.add(ParagraphStyle(name='CaptionRU',parent=styles['SmallRU'],textColor=colors.HexColor('#667788'),spaceAfter=7))
story=[]; coverage={}; sections=[]
def clean(s):return str(s).replace('\u2011','-').replace('\u2014','-').replace('\u2013','-')
def p(s,style='BodyRU'):return Paragraph(escape(clean(s)).replace('\n','<br/>'),styles[style])
def body(s):story.append(p(s))
def heading(s):story.append(p(s,'HeadRU'))
def sub(s):story.append(p(s,'SubRU'))
def table(rows,headers=('Параметр','Пример','Назначение / ограничения'),widths=(135,80,296)):
 data=[[p(s,'SmallRU') for s in headers]]+[[p(v,'SmallRU') for v in row] for row in rows]
 t=Table(data,colWidths=widths,repeatRows=1,hAlign='LEFT')
 t.setStyle(TableStyle([('BACKGROUND',(0,0),(-1,0),colors.HexColor('#dcefe9')),('TEXTCOLOR',(0,0),(-1,0),colors.HexColor('#123b38')),('VALIGN',(0,0),(-1,-1),'TOP'),('LEFTPADDING',(0,0),(-1,-1),7),('RIGHTPADDING',(0,0),(-1,-1),7),('TOPPADDING',(0,0),(-1,-1),4),('BOTTOMPADDING',(0,0),(-1,-1),4),('LINEBELOW',(0,0),(-1,0),.8,colors.HexColor('#188d7c')),('ROWBACKGROUNDS',(0,1),(-1,-1),[colors.white,colors.HexColor('#f4f7fa')]),('LINEBELOW',(0,1),(-1,-1),.3,colors.HexColor('#dde4eb'))]))
 story.append(t)
def display(v):
 if isinstance(v,str):return 'asset:…' if v.startswith('asset:') else (v if len(v)<65 else v[:61]+'...')
 if isinstance(v,float):return f'{v:.4g}'
 if isinstance(v,bool):return 'true' if v else 'false'
 if isinstance(v,dict):return ', '.join(k+'='+display(n) for k,n in v.items())
 return str(v)
def params(obj,path=''):
 if isinstance(obj,dict):
  if set(obj) in ({'x','y'},{'x','y','z'},{'r','g','b','a'}):yield path,obj;return
  for k,v in obj.items():yield from params(v,path+'.'+k if path else k)
 elif isinstance(obj,list):
  seen=set()
  for v in obj:
   for key,val in params(v,path+'[]'):
    if key not in seen:seen.add(key);yield key,val
 else:yield path,obj
def section(file,title,intro,descriptions,notes=()):
 story.append(PageBreak());heading(title);body(file);body(intro)
 data=json.loads((CFG/file).read_text(encoding='utf-8'))
 rows=[]
 for path,value in params(data):
  if path=='schemaVersion':description='Версия структуры файла. В этой версии оставлять 1.'
  else:
   assert path in descriptions,(file,path)
   description=descriptions[path]
  rows.append((path,display(value),description))
 coverage[file]=[key for key,_ in params(data)]
 table(rows)
 for title,text in notes:
  if file=="cards.json":story.append(Spacer(1,5));story.append(p(title+". "+text,"SmallRU"))
  else:sub(title);body(text)
 sections.append((file,title))
heading('Настройки игры\nчерез JSON')
body('ЗА ОДНИМ СТОЛОМ · ВЕРСИЯ 0.7.0 · 23 СЕНТЯБРЯ 2026')
story.append(Spacer(1,20))
body('Справочник для редактирования карт, колод, правил, звуков, эффектов, сцены и параметров префабов. Значения и список полей сверены с Config, включённой в эту версию проекта.')
sub('Где находятся файлы')
table([('Unity','Assets/StreamingAssets/Config','Выберите JSON в Project. Содержимое появится в Inspector. Измените текст и нажмите «Сохранить».'),('Windows','Config рядом с EXE','Распакуйте весь билд. Редактируйте файлы в этой папке; пересборка для изменения параметров не нужна.'),('Встроенная копия','Resources/ConfigDefaults','Служебный снимок для показа ошибки при повреждении Config. Не редактировать как основной источник.')],('Среда','Папка','Порядок работы'),(65,165,281))
sub('Один источник значений')
body('JSON задаёт параметры. Префабы задают структуру объекта, модели, материалы и связи между компонентами. Игра не заменяет значения JSON данными карточных префабов. Загрузка проверяет весь набор до применения, поэтому ошибка одного файла не приводит к частичному обновлению.')
body('В проекте нет editor-test.json: для теста достаточно Play. Игроки, колоды и правила выбираются в обычном лобби.')
sub('Состав Config')
table([(f,'Настройки' if f!='assets.json' else 'Справочник') for f in ['cards.json','decks.json','rules.json','audio.json','vfx.json','world.json','presentation.json','prefabs.json','assets.json']],('Файл','Роль'),(180,331))
story.append(PageBreak());heading('Сохранение и применение')
for title,text in [
('1. Редактирование в Inspector','Откройте Assets/StreamingAssets/Config. Один или двойной щелчок по JSON показывает редактор в Inspector Unity. «Сохранить» проверяет синтаксис, значения и ссылки всех файлов Config. «Отменить изменения» перечитывает сохранённый файл. Внешняя программа для редактирования в Unity не используется.'),
('2. Play в Unity','Откройте MainMenu и нажмите Play. Выберите «За одним ПК» и число игроков. Запускается та же игровая логика, что и в Windows-билде. Steam не обязателен для локального теста.'),
('3. Перезагрузка без перезапуска','После сохранения нажмите «Перезагрузить Config» в Inspector во время Play или в главном меню/настройках ESC игры. Звук, следующие VFX, камера, мир и параметры отображения обновятся. Уже проигрываемый звук или эффект завершится с прежними параметрами.'),
('4. Правила со следующего матча','Карты, колоды, HP и лимиты не меняются посреди текущего матча. Новый баланс применяется перед следующим матчем. В Steam изменение баланса снимает готовность: участникам нужен одинаковый набор cards/decks/rules. Визуальные и звуковые настройки могут различаться.'),
('5. Новые звуки','Сначала добавьте WAV/OGG/другой поддерживаемый AudioClip в Assets Unity. Выполните Summoners Table > Config > Refresh asset IDs. В Inspector audio.json откройте «ID ассетов из Unity», найдите звук, скопируйте ID и укажите его в clip. Для добавления нового ассета в готовый билд нужна пересборка; переключение уже включённых звуков не требует пересборки.')]:
 sub(title);body(text)
sub('Общие обозначения')
table([('schemaVersion','Целое число 1. Версия структуры, не баланс.'),('Vector2','Объект с x и y. Для интерфейса - логические пиксели, для мира - единицы сцены.'),('Vector3','Объект с x, y, z. position - локальная позиция; rotation - углы в градусах; scale - множители масштаба.'),('Color','Объект r, g, b, a. Обычный диапазон 0..1; a - прозрачность.'),('[]','Элемент массива. Строка в справочнике описывает каждый элемент, а пример взят из первого встретившегося.'),('ID','Стабильный идентификатор. Ссылка вида asset:GUID или art:C02 выбирает включённый в проект ассет.')],('Обозначение','Значение'),(100,411))
section('cards.json','Карты','30 определений существ, заклинаний и реакций. Числа эффекта и текст описания редактируются отдельно: при изменении баланса обновляйте оба.',{
'cards[].id':'Фиксированный ID существующего варианта: C01-C18, S01-S08, R01-R04. Не переименовывать.',
'cards[].name':'Отображаемое название. Непустая строка.',
'cards[].kind':'Тип: creature, spell, reaction. Должен совпадать с типом префаба ID.',
'cards[].faction':'Название фракции. Для существ сохраняйте не менее трёх различных ролей в каждой фракции.',
'cards[].role':'Название роли существа. Должно присутствовать в roleColors[].name.',
'cards[].effect':'Идентификатор поддерживаемой механики. Новая механика требует кода, а не только новой строки.',
'cards[].target':'Тип цели; допустимая комбинация проверяется вместе с kind и effect.',
'cards[].rules':'Описание правил. Меняйте вручную вместе с числами; не оставляйте устаревшие максимумы или силу эффекта.',
'cards[].flavor':'Художественная подпись в справочнике.',
'cards[].art':'ID текстуры из assets.json, например art:C02. Не путь к файлу и не промпт генерации.',
'cards[].attack':'Базовая атака, 0..100. Используется для существ.',
'cards[].health':'HP существа, 1..100; для остальных типов допускается 0.',
'cards[].qte':'Напечатанная стоимость QTE: 2..20 для существа/заклинания; 0 для реакции. В «Полководцах» это цена розыгрыша.',
'cards[].value':'Числовая сила effect, 0..100. Смысл зависит от эффекта: урон, защита, добор и т. п.',
'typeColors[].id':'Ключ типа: creature / spell / reaction.',
'typeColors[].name':'Подпись типа на карте.',
'typeColors[].hex':'HTML-цвет, например #45C3AD. Должен корректно распознаваться Unity.',
'roleColors[].id':'Идентификатор цвета роли.',
'roleColors[].name':'Название роли, совпадающее с cards[].role.',
'roleColors[].hex':'HTML-цвет дополнительной полосы существа.'
},[('Границы текущей версии','Параметры существующих 30 карт можно менять. Добавление новых ID, типов целей или механик требует расширения префабов/кода. Иерархия CardBase -> тип -> конкретная карта сохраняется. JSON изменяет игровые экземпляры, не структуру их префабов.')])
section('decks.json','Колоды','Три готовые колоды. Сумма count каждой колоды должна равняться rules.deckSize.',{
'decks[].id':'Уникальный стабильный ID колоды. Сохраняйте noise / cozy / tricks, поскольку на них ссылается существующий выбор.',
'decks[].name':'Название в лобби и каталоге.',
'decks[].subtitle':'Краткая характеристика колоды.',
'decks[].guide':'Подсказка по игре колодой.',
'decks[].entries[].cardId':'ID карты из cards.json.',
'decks[].entries[].count':'Число копий; целое не меньше 1. Общая сумма проверяется.'
})
section('rules.json','Правила и лимиты','Содержит общие правила, параметры QTE и значения флажков по умолчанию. Применяется к следующему матчу.',{
'version':'Версия набора баланса. Для выпуска согласованных правок меняйте у всех участников.',
'title':'Название набора игры.',
'rules.heroHp':'Стартовые и максимальные HP героя, 1..999.',
'rules.deckSize':'Размер каждой колоды, 1..120; согласуйте с decks.json.',
'rules.startingHand':'Начальная рука, 0..handLimit.',
'rules.handLimit':'Максимальная рука, 1..20. Большие значения могут потребовать настройки веера.',
'rules.boardSlots':'В этой версии строго 5: сцена, раскладки и цели рассчитаны на пять слотов.',
'rules.rounds':'Число раундов, 1..10. По умолчанию 1.',
'rules.roundWinPoints':'Очки за победу в раунде, 0..100.',
'rules.eliminationPoints':'Очки за устранение противника, 0..100.',
'rules.turnSeconds':'Время решений хода, 5..600 секунд.',
'rules.revealSeconds':'Длительность общего показа карты, 0..30 секунд.',
'rules.qteMistakes':'Ошибок до срыва QTE, 1..10.',
'rules.commandersQte':'Бюджет QTE за ход «Полководцев», 2..100.',
'rules.wizardSpells':'Лимит маршрута только заклинаний, 1..20.',
'rules.wizardMixedSpells':'Заклинания в маршруте с существами, 0..wizardSpells.',
'rules.wizardCreatures':'Существа в смешанном маршруте, 1..5.',
'rules.qteMaxLength':'Предел длины QTE при включённом ограничении усилений, 2..100.',
'rules.qteBaseSeconds':'Базовое время QTE из двух символов, 1..120 секунд.',
'rules.qteSecondsPerSymbol':'Дополнительное время за символ сверх двух, 0..30 секунд.',
'rules.qteMinimumSeconds':'Минимальное итоговое время QTE, 1..120 секунд.',
'rules.qteMistakePenalty':'Вычитаемые за ошибку секунды, 0..30.',
'rules.effectLimits[].effect':'ID суммируемого эффекта из таблицы ниже. Дубликаты запрещены.',
'rules.effectLimits[].maximum':'Максимум суммы при включённом флажке, 0..100. Без флажка предел не применяется.',
'defaults.mode':'Режим нового лобби: wizards / commanders.',
'defaults.limitPower':'true сохраняет пределы; false отключает пределы сложения эффектов.',
'defaults.cards3D':'true включает параллакс иллюстраций по умолчанию.',
'defaults.revision':'Служебная ревизия готовности. В исходном JSON оставлять 0.'
},[('Формула времени QTE','max(qteMinimumSeconds, qteBaseSeconds + (число символов - 2) * qteSecondsPerSymbol + бонус времени - штраф времени). Расход бюджета равен напечатанному QTE, даже если фактическая длина ритуала изменилась.'),('Текст карт','Изменение effectLimits или максимума HP не переписывает вручную сформулированные descriptions/rules. Обновляйте соответствующие cards[].rules и справочник карт при выпуске изменённого баланса.')])
sub('Идентификаторы суммируемых эффектов')
effect_names={'guard':'Защита героя','attackAura':'Прибавка атаки другим существам','openingPower':'Прибавка первой атаки','spellPower':'Сила заклинаний урона','thorns':'Ответный урон нападающему герою','lifesteal':'Лечение владельца при атаке','spellDraw':'Добор за первое успешное заклинание','turnDraw':'Добор в начале хода','failDraw':'Добор за первый срыв QTE','turnHeal':'Лечение в начале хода','forgive':'Прощённые промахи QTE','qteExtra':'Дополнительные символы чужого QTE','timeBonus':'Дополнительные секунды своего QTE','timeTax':'Штраф времени чужого QTE','spellTax':'Урон герою за успешное заклинание'}
table([(x['effect'],x['maximum'],effect_names[x['effect']]) for x in json.loads((CFG/'rules.json').read_text())['rules']['effectLimits']])
section('audio.json','Звуки и игровые действия','Каждая запись назначает звук событию. ID звука выбирается из встроенного справочника Inspector; сам файл звука хранится в Unity.',{
'cues[].action':'Фиксированный идентификатор события. Все обязательные события должны оставаться, дубликаты запрещены.',
'cues[].clip':'ID AudioClip из assets.json. Пустая строка отключает звук события.',
'cues[].volume':'Громкость 0..1. Эффекты дополнительно умножаются на ползунок «Эффекты», все звуки - на «Общая громкость».',
'cues[].pitch':'Высота/скорость воспроизведения, 0.1..3; 1 без изменения.',
'cues[].delay':'Задержка разового события, 0..10 секунд. Для непрерывного ambience оставляйте 0.'
})
sub('События')
table([('card.hover','Наведение на карту'),('action.invalid','Недопустимое действие'),('creature.attack','Начало атаки существа'),('damage.hit','Попадание/урон'),('qte.correct','Правильный символ QTE'),('qte.error','Ошибка QTE'),('turn.start','Начало нового хода'),('ambience','Зацикленный фон; регулируется общей громкостью'),('S01.launch ... S08.launch','Начало эффекта соответствующего заклинания'),('S01.impact ... S08.impact','Завершение/попадание эффекта соответствующего заклинания')],('action','Когда звучит'),(190,321))
section('vfx.json','Визуальные эффекты','Связи с включёнными в сборку префабами частиц. Изменения видны при следующем создании эффекта.',{
'effects[].id':'ID одного из восьми заклинаний S01-S08.',
'effects[].travel':'ID префаба летящего эффекта. Пусто - отключён.',
'effects[].impact':'ID префаба попадания. Пусто - отключён.',
'effects[].persistent':'ID длительного эффекта, например оглушения. Пусто - отключён.',
'effects[].motion':'Траектория: 0 Projectile, 1 Area, 2 Exchange, 3 Draw, 4 Return, 5 Boost.',
'effects[].cardCount':'Количество визуальных карт добора, 1..8. Не меняет игровой добор.',
'effects[].travelScale':'Масштаб полёта, 0.001..100.',
'effects[].impactScale':'Масштаб попадания, 0.001..100.',
'effects[].persistentScale':'Масштаб постоянного эффекта, 0.001..100.',
'effects[].travelSeconds':'Время движения, 0.05..10 секунд.',
'effects[].lifetime':'Дополнительное время жизни эффекта, 0.05..15 секунд.',
'effects[].arc':'Высота дуги в единицах сцены, 0..20.',
'effects[].cardSize':'Vector2: размеры визуальных карт полёта; x/y 0.001..100.',
'attack':'Общий префаб атаки существа.',
'hit':'Общий префаб попадания.',
'death':'Эффект смерти героя.',
'qteFire':'Активный огонёк QTE.',
'qteSmoke':'Неактивный огонёк QTE.',
'qteAttempt':'Индикатор попытки QTE.',
'motionTitle':'ID префаба титров с компонентом StyleManager. Применяется при перезагрузке Config.',
'attackScale':'Общий масштаб атаки, 0.001..100.',
'impactScale':'Общий масштаб попадания/смерти, 0.001..100.',
'qteScale':'Общий масштаб огоньков, 0.001..100.'
})
world_desc={
'table':'Стол относительно родителя окружения.',
'chair':'Общее кресло относительно PlayerSeat.',
'avatar':'Корень персонажа относительно PlayerSeat.',
'heroTarget':'Область выбора героя относительно PlayerSeat.',
'slots[]':'Каждый из пяти слотов относительно PlayerSeat.',
'layouts[].seats[].root':'Корень места игрока относительно раскладки.'
}
wd={'layouts[].players':'Число мест: один профиль для 2, один для 3, один для 4.',
'unitsPerMetre':'Единиц сцены на метр, 0.01..100. Исходное значение 8.',
'tableHeight':'Опорная высота столешницы в метрах, 0.1..10. Согласуйте с table и y слотов: влияет на точки эффектов и центр стола.',
'tableDiameter':'Опорный диаметр стола в метрах, 0.1..30. Геометрию меняет table.scale.',
'avatarHipHeight':'Высота таза сидящего персонажа в единицах сцены, 0..100.'}
for key,desc in world_desc.items():
 for field,explain in {'position':'Vector3, локальные координаты, каждый компонент -10000..10000.','rotation':'Vector3, локальные углы XYZ в градусах.','scale':'Vector3, масштаб XYZ; каждый компонент 0.00001..10000.'}.items():wd[key+'.'+field]=desc+' '+explain
section('world.json','Стол и места игроков','Общие параметры кресла, персонажа, области выбора и пяти слотов задаются один раз. Профили раскладки определяют положение корней мест для 2, 3 и 4 игроков.',wd,[('Как читать координаты','Внутри PlayerSeat персонаж и кресло находятся на отрицательной оси Z, перед ними - слоты. Сам PlayerSeat поворачивается вокруг центра стола. Поэтому одно изменение chair или slots применяется ко всем местам.'),('Предпросмотр в сцене','Откройте Match, выберите world.json и нажмите «Применить world.json к открытой сцене». Изменения поддерживают Undo; сохранение сцены выполняется отдельно. При Play значения всё равно загружаются из JSON.'),('Сохранение пропорций','Исходная сцена соответствует столу диаметром 1.5 м с высотой 0.75 м. При ручном изменении table.scale одновременно согласуйте tableHeight, положения слотов, героя и области выбора. JSON не является автоматическим решателем посадки.')])
section('presentation.json','Камера и объём карт','Значения применяются после перезагрузки Config. Текущий выбранный ракурс сохраняется; initialCameraMode используется при инициализации камеры.',{
'camera.lookSensitivity':'Чувствительность ПКМ; больше 0, не больше 20.',
'camera.cameraSmoothing':'Скорость сглаживания камеры, положительное число.',
'camera.hoverScale':'Увеличение карты при наведении, 1..1.5.',
'camera.hoverTilt':'Наклон от курсора в градусах, -30..30.',
'camera.cardFlightSeconds':'Время полёта разыгрываемой карты, 0..1.5 секунды.',
'camera.initialCameraMode':'Начальный ракурс 0, 1 или 2.',
'camera.cameraModes[].name':'Название ракурса. Требуется ровно три записи.',
'camera.cameraModes[].distance':'Расстояние камеры от центра по плоскости стола, не меньше 0.',
'camera.cameraModes[].height':'Высота камеры в единицах сцены, не меньше 1.1.',
'camera.cameraModes[].focusHeight':'Высота точки, куда направлена камера.',
'camera.cameraModes[].fieldOfView':'Вертикальный угол обзора, 20..110 градусов.',
'camera.cameraModes[].yawLimit':'Предел поворота влево/вправо, 0..180 градусов.',
'camera.cameraModes[].downLimit':'Предел взгляда вниз, 0..90 градусов.',
'camera.cameraModes[].upLimit':'Предел взгляда вверх, 0..90 градусов.',
'cardDepth':'Глубина параллакса иллюстрации, 0..0.35; действует при включённых 3D-карточках.',
'cardFoil':'Сила перелива иллюстрации, 0..1.'
})
story.append(PageBreak());heading('Параметры префабов');body('prefabs.json')
body('Каждый игровой префаб имеет PrefabConfigBinding с configId. Профиль выбирается по этому ID. Вложенный префаб получает свой профиль; карточные варианты наследуют настройки общей основы. Пустой components допустим: такой префаб получает предметные данные из cards/world/vfx либо служит структурным контейнером.')
table([('schemaVersion','1','Версия структуры.'),('prefabs[].id','HandFan','ID профиля, совпадающий с PrefabConfigBinding.configId.'),('prefabs[].components[].path','пусто или путь','Путь к объекту внутри префаба. Пусто означает корень. Не переименовывайте без соответствующего изменения префаба.'),('prefabs[].components[].type','SummonersTable.HandFan','Полное имя поддерживаемого компонента. Это выбор из существующих типов, не загрузка произвольного кода.'),('prefabs[].components[].values','объект','Параметры компонента; ниже перечислены все поля текущего набора.')])
pref=json.loads((CFG/'prefabs.json').read_text())
typed={}
for entry in pref['prefabs']:
 for component in entry['components']:
  typed.setdefault(component['type'].split('.')[-1],{}).update(component['values'])
pd={
'HandFan':{'cardSize':'Vector2, размер карты руки в логических пикселях.','spacing':'Расстояние между соседними картами.','maxSpread':'Максимальная ширина раскладки.','arc':'Опускание краёв веера.','angle':'Угол поворота на каждый шаг от центра.','hoverLift':'Подъём наведённой/выбранной карты.'},
'WorldHandFanSettings':{'cardSize':'Vector2, размеры физических рубашек в единицах сцены.','radius':'Радиус дуги веера.','sweep':'Максимальный угол раскрытия в градусах.','maximumStep':'Предельный угол между соседними картами.','sideOffset':'Боковой сдвиг от героя.','inwardOffset':'Сдвиг к столу.','height':'Высота над столешницей.','tilt':'Наклон рубашек.','roll':'Доля дополнительного поворота вокруг Z.'},
'PlayerStatusView':{'nameOffset':'Vector3, сдвиг имени от героя.','healthBetweenHeroAndSlots':'Доля пути от героя к среднему слоту для HP.','healthHeight':'Высота шкалы HP в единицах сцены.','minimumNameGap':'Минимальный зазор имени от проекции головы.','nameScreenPadding':'Дополнительный экранный подъём имени.','nameCollisionPadding':'Отступ между ником и перекрывающими его экранными элементами.'},
'TargetArrowGraphic':{'width':'Ширина линии экранной стрелки.','headLength':'Длина наконечника.','headWidth':'Половина ширины наконечника.'},
'WorldArrowView':{'segments':'Число точек пространственной стрелки; не меньше 3.','headLength':'Длина наконечника в мире.','headWidth':'Ширина наконечника в мире.','widthMultiplier':'Множитель толщины.'},
'TurnBudgetView':{'active':'Color, доступный маршрут действий.','inactive':'Color, недоступный маршрут.'},
'MatchOptionsView':{'selected':'Color, выбранный режим.','unselected':'Color, невыбранный режим.'},
'CardDepthVisual':{'pointerInfluence':'Сила влияния курсора на параллакс, обычно 0..1.'},
'TavernPanel':{'border':'Color, цвет каймы панели.','corner':'Размер среза углов.','inset':'Внутренний отступ каймы.'},
'EffectsVolume':{'baseVolume':'Базовая громкость источника; умножается на ползунок эффектов.'}
}
for typ,values in sorted(typed.items()):
 sub(typ)
 rows=[]
 for field,value in values.items():
  assert field in pd.get(typ,{}),(typ,field)
  rows.append((field,display(value),pd[typ][field]))
 table(rows)
coverage['prefabs.json']=[key for key,_ in params(pref)]
body('Форма объектов, шрифты, статическая вёрстка, модели и связи компонентов по-прежнему редактируются в Prefab Mode. JSON управляет перечисленными параметрами; он не заменяет иерархию Unity. Значения в этой таблице - пример существующего профиля, а не обязательные значения для всех объектов.')
section('assets.json','Справочник ассетов','Генерируется из Unity командой Refresh asset IDs. Это список для выбора ID, не внешняя загрузка ресурсов. Игра разрешает ссылки через ConfigAssets.asset, который включён в билд.',{
'assets[].id':'Устойчивый ID ассета. Копируется в clip, art или соответствующее поле vfx.',
'assets[].kind':'audio / texture / prefab.',
'assets[].path':'Путь исходного ассета в Unity. Информационное поле: изменение строки не добавляет файл в билд.'
},[('Что включено','Все импортированные AudioClip проекта; карточные иллюстрации Resources/Art; выбранные эффекты, уже подключённые к игровым префабам. Для нового VFX сначала назначьте его в проекте и обновите реестр. Для нового звука достаточно импорта и Refresh asset IDs, затем сборки.'),('Чего не делать','Не подставляйте вместо ID путь к WAV рядом с EXE. Внешние звуковые файлы не загружаются, согласно согласованному порядку работы.')])
story.append(PageBreak());heading('Примеры и проверка')
sub('Поменять звук атаки')
body('В audio.json найдите action = creature.attack. В Inspector раскройте список ID, найдите нужный AudioClip и скопируйте его ID в clip. Установите volume, например 0.2, и pitch = 1. Нажмите «Сохранить», затем во время Play - «Перезагрузить Config». Следующая атака использует новый звук.')
sub('Изменить баланс карты')
body('В cards.json найдите C02. Измените attack, например с 3 на 4, и при необходимости текст rules. Сохраните и перезагрузите Config. Текущий матч сохранит старую атаку; следующий использует 4. Для Steam передайте участникам одинаковые cards.json, decks.json и rules.json.')
sub('Сделать руку крупнее')
body('В prefabs.json найдите профиль HandFan и values.cardSize. Увеличьте x и y. При необходимости скорректируйте spacing, maxSpread, arc и angle. После перезагрузки проверьте руку с 8 картами и наведением, чтобы она не перекрывала кнопку конца хода.')
sub('Частые ошибки')
table([('Не сохраняется JSON','Проверьте кавычки, запятые, повторные ключи и название параметра. Inspector показывает файл и причину. Нельзя оставлять запятую после последнего элемента.'),('Неизвестный ID звука','Импортируйте AudioClip в Unity, обновите реестр и скопируйте ID из Inspector. Для нового ассета пересоберите Windows Player.'),('Не видно лобби друга','Сверьте версию игры, протокол 8 и одинаковый баланс cards/decks/rules. Разные наборы не допускаются к одному матчу.'),('Изменение не видно в текущем матче','Баланс применяется со следующего матча. Для оформления и звука нажмите перезагрузку; сохранения файла недостаточно.'),('Пропали изменения после запуска','Редактируйте исходную Config или Config рядом с EXE. Поля, управляемые JSON, при Play получают значения из JSON, даже если их вручную изменили в префабе.'),('Ошибочная Config при запуске','Игра показывает ошибку и блокирует начало матча до исправления. Исправьте файл и нажмите перезагрузку. При ошибке горячей перезагрузки ранее загруженный набор остаётся целым.')],('Ситуация','Решение'),(145,366))
sub('Границы проверки выпуска')
body('Проверены строгий JSON, ссылки ассетов, общий источник карт, наследование повторяющихся префабов, запуск и перезагрузка настроек в Unity Play. Текущая версия предназначена для проверки в редакторе; папка Config рядом с EXE предусмотрена для будущих сборок. Сетевой матч через Steam на нескольких отдельных ПК требует совместного теста. Файлы проекта и этот справочник относятся к версии 0.7.0.')
def footer(c,doc):
 c.setStrokeColor(colors.HexColor('#d9e5e9'));c.line(42,39,553,39)
 c.setFont('Arial',8);c.setFillColor(colors.HexColor('#687b8a'));c.drawString(42,26,'За одним столом · Config 0.7.0');c.drawRightString(553,26,str(doc.page))
 c.setFillColor(colors.HexColor('#148c7e'));c.rect(0,833,595.28,9,fill=1,stroke=0)
doc=SimpleDocTemplate(str(OUT),pagesize=(595.28,841.89),leftMargin=42,rightMargin=42,topMargin=45,bottomMargin=53,title='За одним столом - справочник JSON 0.7',author='GameJams')
doc.build(story,onFirstPage=footer,onLaterPages=footer)
reader=PdfReader(OUT)
text='\n'.join(page.extract_text() for page in reader.pages)
for name in coverage:assert name in text,name
assert len(coverage)==9
report={'pdf':str(OUT),'pages':len(reader.pages),'documentedFiles':9,'parameterPaths':sum(map(len,coverage.values())),'coverage':coverage,'sha256':hashlib.sha256(OUT.read_bytes()).hexdigest()}
(ROOT/'output/tests/config-pdf-coverage.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in report.items() if k!='coverage'},ensure_ascii=False))
