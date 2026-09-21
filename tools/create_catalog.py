"""Single editable catalogue for the Unity prototype and its printed handbook."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'SummonersTable/Assets/Resources/Data/catalog.json'
cards = []

def creature(id, name, faction, role, atk, hp, qte, effect, value, rules, flavor, subject):
    cards.append(dict(id=id, name=name, kind='creature', faction=faction, role=role,
        attack=atk, health=hp, qte=qte, effect=effect, value=value, target='enemy',
        rules=rules, flavor=flavor, art=subject))

def special(id, name, kind, qte, effect, value, target, rules, flavor, subject):
    cards.append(dict(id=id, name=name, kind=kind, faction='', role='',
        attack=0, health=0, qte=qte, effect=effect, value=value, target=target,
        rules=rules, flavor=flavor, art=subject))

creature('C01','Енот-карманник','Звери','Снабженец',2,3,3,'spellDraw',1,
 'Первое успешное заклинание в ваш ход: доберите 1 карту. Несколько таких эффектов дают не более 2 карт.',
 'Он просто проверял, хорошо ли лежит.',
 'A cheeky raccoon in an oversized wizard hood secretly pulling a glowing blank playing card from its sleeve, playful expression')
creature('C02','Медведь-обниматель','Звери','Защитник',3,6,4,'guard',1,
 'Урон вашему герою от атак и заклинаний уменьшен на 1. Общая защита существ не выше 3; усталость не уменьшается.',
 'Обнимет крепко. Особенно огненный шар.',
 'A huge gentle brown bear in a knitted scarf hugging a tiny glowing fireball safely against its belly')
creature('C03','Боевой гусь','Звери','Усилитель',4,3,4,'attackAura',1,
 'Другие ваши существа получают +1 к атаке, пока гусь на столе. Сумма таких усилений не выше +2.',
 'Мир был вариантом. Но не сегодня.',
 'An indignant white goose wearing a tiny crooked battle helmet, raising a wooden spoon like a commanding general')
creature('C04','Суккуб техподдержки','Демоны','Ритуалист',2,4,3,'forgive',1,
 'Первый промах в каждом вашем QTE не считается ошибкой и не отнимает время. Эффект не складывается.',
 'Вы пробовали перепризвать?',
 'A friendly small purple horned demon wearing a headset, carefully repairing floating magical letter-shaped abstract symbols with a wrench, no readable letters')
creature('C05','Бес-бюрократ','Демоны','Вредитель',2,4,3,'qteExtra',1,
 'QTE противников длиннее на 1 символ. Все подобные помехи вместе добавляют не более 2 символов.',
 'Ритуал принят. Приложите ещё один ритуал.',
 'A tiny red imp behind a mountain of blank parchment paperwork, stamping a glowing scroll with a comically enormous rubber stamp')
creature('C06','Демон-пожарник','Демоны','Провокатор',4,4,4,'thorns',1,
 'Когда атака или заклинание противника наносит урон вашему герою, его герой получает 1 ответного урона. Максимум 2; ответный урон не вызывает новый ответ.',
 'Огонь на себя! А потом обратно.',
 'A round orange horned demon in a firefighter helmet holding a hose that sprays small harmless glowing sparks backwards')
creature('C07','Скелет-бухгалтер','Нежить','Снабженец',2,4,3,'turnDraw',1,
 'В начале вашего хода доберите 1 дополнительную карту. Дополнительный добор от существ ограничен 2 картами.',
 'У него всё сходится. Кроме челюсти.',
 'A cheerful skeleton with round spectacles counting luminous playing cards on a wooden abacus, jaw slightly askew')
creature('C08','Вампир на диете','Нежить','Лекарь',3,4,3,'lifesteal',1,
 'Когда ваше существо наносит урон вражескому герою, восстановите своему герою 1 HP. Не более 2 HP за одну атаку от всех таких эффектов.',
 'После шести только группа крови ноль.',
 'A bashful aristocratic cartoon vampire sipping red berry juice from a tiny cup through a curly straw, elegant cape')
creature('C09','Рыцарь последней смены','Нежить','Защитник',3,6,4,'guard',1,
 'Урон вашему герою от атак и заклинаний уменьшен на 1. Общая защита существ не выше 3; усталость не уменьшается.',
 'Мёртвый, уставший, всё ещё на посту.',
 'A sleepy skeletal knight leaning on a broad dented shield with a cushion tied to it, warm friendly fantasy illustration')
creature('C10','Голем-подушка','Механоиды','Защитник',1,7,4,'guard',2,
 'Урон вашему герою от атак и заклинаний уменьшен на 2. Общая защита существ не выше 3; усталость не уменьшается.',
 'Принимает удар. И вашу усталость.',
 'A stout magical stone golem entirely wrapped in colorful soft pillows, standing protectively with open arms')
creature('C11','Турель-комплиментатор','Механоиды','Усилитель',4,3,4,'spellPower',1,
 'Ваши заклинания прямого и массового урона наносят на 1 больше. Общая прибавка не выше +2.',
 'Отличная осанка! Пли!',
 'A charming brass clockwork turret with an expressive lens eye and a little bow tie, firing bright confetti sparks')
creature('C12','Принтер заклинаний','Механоиды','Снабженец',2,3,3,'failDraw',1,
 'После первого сорванного QTE в ваш ход доберите 1 карту. Несколько принтеров дают не более 2 карт.',
 'Замятие маны. Откройте крышку.',
 'A whimsical brass and wooden magical printer coughing out a tangled ribbon of blank glowing spell scrolls')
creature('C13','Дракон-стажёр','Драконы','Вредитель',4,4,4,'spellTax',1,
 'После успешного заклинания противника его герой получает 1 урон. Общий штраф от таких существ не выше 2. Этот урон не вызывает ответных эффектов.',
 'Я только посмотреть. Ой, загорелось.',
 'A small clumsy teal dragon apprentice sneezing a tiny flame onto its own blank notebook, embarrassed cheerful expression')
creature('C14','Дракон-грелка','Драконы','Лекарь',3,5,4,'turnHeal',2,
 'В начале вашего хода восстановите герою 2 HP. Все такие существа вместе лечат не более 4 HP за ход.',
 'Тридцать градусов заботы.',
 'A plump friendly coral dragon curled around a warm teapot and knitted blanket, comforting golden glow')
creature('C15','Дракон-фанат','Драконы','Усилитель',3,4,3,'openingPower',1,
 'Первая атака ваших разыгрываемых существ наносит на 1 больше урона. Общая прибавка не выше +2.',
 'Лучший призыв! Ещё! Ещё!',
 'An enthusiastic little emerald dragon waving two oversized foam cheering hands beside a magical summoning circle')
creature('C16','Дух дедлайна','Духи','Вредитель',3,3,3,'timeTax',2,
 'У противников на QTE на 2 секунды меньше. Общий штраф не выше 4 секунд; итоговый таймер не короче 6 секунд.',
 'Надо было призвать вчера.',
 'A mischievous translucent violet ghost emerging from an oversized hourglass, pointing impatiently at a tiny pocket watch with no numerals')
creature('C17','Дух аптечки','Духи','Лекарь',2,5,3,'turnHeal',2,
 'В начале вашего хода восстановите герою 2 HP. Все такие существа вместе лечат не более 4 HP за ход.',
 'На рану подуем, само пройдёт.',
 'A gentle mint-green floating spirit carrying a small satchel of bandages and glowing medicinal leaves, friendly smile, no medical cross logo')
creature('C18','Призрак подсказки','Духи','Ритуалист',2,4,3,'timeBonus',2,
 'Ваши QTE получают +2 секунды. Несколько таких эффектов дают не более +4 секунд.',
 'Я бы нажал вот эту. Но рук нет.',
 'A curious pale blue friendly ghost peeking over a floating enchanted keyboard with blank keys and a softly glowing helpful fingertip')

special('S01','Огненный чих','spell',3,'damage',4,'enemy',
 'Нанесите 4 урона выбранному вражескому герою или существу.', 'Будьте здоровы. Остальные - как получится.',
 'A comic burst of orange magical flame shaped like an enormous sneeze, a tiny crumpled handkerchief flying away')
special('S02','Обнимашки по расписанию','spell',3,'heal',5,'hero',
 'Восстановите выбранному живому герою 5 HP, не выше 30. Можно помочь противнику.', 'Пять единиц заботы, без очереди.',
 'Two oversized glowing friendly magical hands gently embracing a small heart-shaped amber light')
special('S03','Всем по крошке','spell',4,'areaDamage',2,'none',
 'Нанесите каждому живому вражескому герою 2 урона. Защита и реакции применяются отдельно к каждому.', 'Делить надо поровну. Даже неприятности.',
 'A mischievous enchanted pie bursting into several little glowing crumbs flying in different directions, playful magical explosion')
special('S04','Технический перерыв','spell',3,'stun',1,'enemyUnit',
 'Выбранное вражеское существо пропускает следующую атаку в конце хода. Его постоянный эффект продолжает работать.', 'Существо отошло на пять минуточек.',
 'An enchanted battle helmet resting on a tiny deckchair beside a steaming mug, an abstract magical pause symbol formed from two glowing bars')
special('S05','Карманная перестановка','spell',4,'swap',1,'enemyHero',
 'Обменяйте случайную карту своей руки на случайную карту выбранного противника. Для розыгрыша у обоих должна оставаться карта для обмена.', 'У тебя было лучше. Теперь спорный вопрос.',
 'Two mischievous magical gloved hands exchanging colorful face-down cards inside a swirling circular portal')
special('S06','Чужой конспект','spell',3,'draw',2,'self',
 'Доберите 2 карты. Всё сверх лимита руки в 8 карт сгорает.', 'Почерк непонятный. Магия рабочая.',
 'A battered open spell notebook with abstract glowing doodles and two blank playing cards rising out of its pages, no readable writing')
special('S07','Слабо ещё две?','spell',2,'riskBoost',3,'self',
 'Ваше следующее существо в этот ход получает +2 символа к QTE и +3 урона к атаке при розыгрыше. Усиление одноразовое, действует только в этот ход и не складывается.', 'Последние слова уверенного мага.',
 'Two extra luminous blank keyboard keys balanced daringly on top of a wobbling magical tower, energetic amber sparks')
special('S08','Вы ошиблись столиком','spell',4,'bounce',1,'unit',
 'Верните выбранное существо в руку его владельца. Его постоянный эффект сразу исчезает. При полной руке карта сгорает.', 'Ваш ритуал в соседнем зале.',
 'A tiny bewildered friendly summoned creature sliding backwards through a round purple return portal while holding a dinner reservation card without text')

special('R01','Я вообще мимо','reaction',0,'reduce',3,'pending',
 'Во время розыгрыша чужого существа или заклинания: уменьшите урон этой карты вам либо вашему существу на 3. Не действует на обычные атаки со стола и усталость.', 'Меня здесь даже нарисовали случайно.',
 'A comical wizard-shaped empty coat sidestepping a glowing orange magic bolt, the coat flapping with exaggerated movement')
special('R02','Держись, дурень!','reaction',0,'rescue',2,'pending',
 'Во время розыгрыша чужой карты: уменьшите её урон защищаемой цели на 2. Можно спасать себя или другого игрока. Не реагирует на обычные атаки со стола.', 'Сначала спасу. Потом посмеюсь.',
 'A warm golden magical hand catching another tiny hand just before a harmless sparkling impact, heroic yet funny composition')
special('R03','Сам такой','reaction',0,'reflect',2,'pending',
 'Во время розыгрыша чужой карты, которая угрожает вам либо вашему существу: если она нанесёт урон, её владелец получит 2 ответного урона. Исходный урон не отменяется. Не действует на атаки со стола.', 'Аргумент принят и отправлен обратно.',
 'A cheeky enchanted hand mirror reflecting a small magical fireball back along a curved trail of pink sparks')
special('R04','Руки прочь','reaction',0,'deny',1,'pending',
 'Во время розыгрыша вражеского заклинания: отмените его эффект для вас или вашего существа. Не отменяет призыв, атаки существ, добор и усиления самого заклинателя.', 'Это моё. Даже если мне не нравится.',
 'A small assertive purple magical stop-hand shielding a glowing playing card from sneaky fingers, no letters or text')

def deck(id,name,subtitle,creatures,spells,reactions,guide):
    ids = creatures.split() + spells.split() + reactions.split()
    return dict(id=id,name=name,subtitle=subtitle,guide=guide,
                entries=[dict(cardId=i,count=1 if i.startswith('R') else 3 if i in creatures.split()[:3] else 2) for i in ids])

decks = [
 deck('noise','Шумная компания','Давление, усиления, ответный урон',
 'C03 C06 C09 C11 C13 C15','S01 S03 S04 S06 S07 S08','R01 R03 R04',
 'Начните с существа и не отдавайте стол без борьбы. Усилители увеличивают урон следующих карт. Слабо ещё две? разыгрывайте до существа, когда уверены в QTE. Выбирайте между атакой героя и устранением опасного постоянного эффекта.'),
 deck('cozy','Живём дальше','Защита, лечение, спокойные ритуалы',
 'C02 C08 C10 C14 C17 C18','S01 S02 S03 S04 S06 S08','R01 R02 R04',
 'Поставьте защитника, затем источник лечения. Сохраняйте реакции для большого урона и защищайте полезных существ. Лечение можно направлять сопернику ради временного союза. Вы ошиблись столиком убирает опасные эффекты или спасает вашего бойца.'),
 deck('tricks','Мелкие пакости','Добор, обмены, помехи QTE',
 'C01 C04 C05 C07 C12 C16','S01 S02 S04 S05 S06 S08','R01 R02 R03',
 'Развивайте добор, но следите за лимитом руки. Помехи усложняют ритуалы всех противников. Техподдержка страхует ваши ошибки. Обмен и возврат существ меняют планы соперников; реакции позволяют торговаться и спасать нужного вам участника.')
]

catalog = dict(version='0.2.0',title='Арена призыва: За одним столом',
 rules=dict(heroHp=30,deckSize=30,startingHand=5,handLimit=8,boardSlots=5,rounds=3,
            roundWinPoints=3,eliminationPoints=1,turnSeconds=45,revealSeconds=3,qteMistakes=3),
 typeColors=[dict(id='creature',name='Существо',hex='#3FC5AD'),dict(id='spell',name='Заклинание',hex='#F0B354'),dict(id='reaction',name='Реакция',hex='#AF9AF6')],
 roleColors=[dict(name='Защитник',hex='#5D9CEB'),dict(name='Лекарь',hex='#78C66B'),dict(name='Снабженец',hex='#E5C85B'),dict(name='Усилитель',hex='#F49454'),dict(name='Ритуалист',hex='#6BD7D8'),dict(name='Вредитель',hex='#CF7CBF'),dict(name='Провокатор',hex='#EF7983')],
 cards=cards,decks=decks)
assert len(cards)==30 and len({c['id'] for c in cards})==30
for d in decks:
    assert sum(e['count'] for e in d['entries'])==30
    assert all(e['cardId'] in {c['id'] for c in cards} for e in d['entries'])
    assert sum(e['count'] for e in d['entries'] if e['cardId'].startswith('R'))==3
for faction in {c['faction'] for c in cards if c['faction']}:
    assert len({c['role'] for c in cards if c['faction']==faction})==3
OUT.parent.mkdir(parents=True,exist_ok=True)
OUT.write_text(json.dumps(catalog,ensure_ascii=False,indent=2),encoding='utf-8')
print(f'{OUT}: {len(cards)} unique cards; 3 decks x 30; 6 factions x 3 roles')
