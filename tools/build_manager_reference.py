"""Build the 0.8 Manager/JSON reference; reuse unchanged field descriptions from 0.7."""
from pathlib import Path
old=Path(__file__).with_name('build_config_reference.py').read_text(encoding='utf-8')
setup=old[:old.index("heading('Настройки игры")]
setup=setup.replace("config-reference-v0.7.pdf","config-reference-v0.8.pdf").replace("ROOT=Path(__file__).resolve().parents[1]","ROOT=Path(__file__).resolve().parents[1]")
setup=setup.replace('story.append(t)', 'story.append(KeepTogether([t]) if len(rows)<=10 else t)').replace('if file=="cards.json":','if file in ("cards.json","world.json"):')
exec(setup)
heading('Управление игрой\nв Unity Editor')
body('ЗА ОДНИМ СТОЛОМ · 0.8.0 · 23 СЕНТЯБРЯ 2026')
body('Руководство для разработчика: сцены настройки, Manager, предпросмотр без Play, копии и все параметры JSON. Игровой матч по-прежнему рассчитан на 2-4 игроков; предпросмотр локации поддерживает 1-8 мест. В этой итерации Player не собирается.')
sub('Начните здесь')
table([
('Играть','Assets/Scenes/MainMenu.unity','Нажмите Play, выберите «За одним ПК», число игроков и начните матч.'),
('Локация','Authoring/LocationLab.unity','World Manager: интерьер, места, общий масштаб кресла и персонажа, таймеры, события.'),
('Эффекты и звук','Authoring/EffectsLab.unity','Vfx, Audio и Events Manager: выбор ассетов, параметры и отдельный запуск.'),
('Анимации','Authoring/AnimationLab.unity','PlayerAnimations Manager: состояния, последовательности, скорость и повтор.'),
('Карты / интерфейс','Authoring/CardsLab.unity','Cards, Decks, Interface, Presentation Manager: карта, веер, HP, стрелки, камера.')
],('Задача','Сцена','Действия'),(93,167,251))
body('Все Authoring-сцены находятся в Assets/Scenes. Открыть их можно через Summoners Table > Authoring. Для предпросмотра выбирайте объект Manager в Hierarchy. Панель Inspector показывает обычные поля Unity, списки и выбор импортированных ассетов.')
story.append(PageBreak());heading('Редактирование и сохранение')
for title,text in [
('1. Загрузите нужный раздел','Выберите Manager и нажмите «Загрузить сохранённые параметры», если JSON менялся в другой сцене. Поля Manager являются редактируемым черновиком.'),
('2. Проверьте изменение','Поменяйте поля. Локация пересоздаёт места сразу вне Play; «Количество мест» задаёт предпросмотр, а изменение максимума диапазона также обновляет число мест. Для эффектов, анимаций, карт, HP и стрелки нажмите «Показать / воспроизвести».'),
('3. Остановите длительное состояние','Кнопка «Остановить и очистить» удаляет временные объекты и останавливает звуки/анимацию выбранного предпросмотра. Новый запуск заменяет предыдущий. Закрытие сцены, Prefab Mode и переход в Play тоже очищают его.'),
('4. Сохраните','«Сохранить» проверяет весь набор Config и обновляет выбранный раздел main.json, а также одноимённый рабочий файл, например audio.json. Неверные ссылки или диапазоны не записываются. Геометрию и раскладку объектов сохраняйте обычным Save Scene / Save Prefab.'),
('5. Сделайте копию','«Создать копию» сохраняет текущий черновик выбранного Manager в Config/History/<раздел>/<раздел>_yyyyMMdd-HHmmss-fffffffZ.json. Время UTC. Другие разделы не копируются.'),
('6. Откатитесь','«Откатиться» загружает ближайшую предыдущую копию по времени из имени; будущие отметки пропускаются. Меняются только поля выбранного Manager. Для записи отката в рабочую Config нажмите «Сохранить».'),
('7. Проверьте игру','Откройте MainMenu и нажмите Play. Сохранённые данные читаются при запуске. Во время Play «Применить сохранённое к игре» или «Перезагрузить Config» обновляет оформление и звук. Правила, карты, колоды, механики событий и таймеры фиксируются до следующего матча.')
]:sub(title);body(text)
body('Предпросмотр рассчитан на Edit Mode. Authoring-сцены не запускают полный матч. Для игры используйте MainMenu. Нет отдельного editor-test.json.')
story.append(PageBreak());heading('Где хранятся данные')
table([
('Рабочие разделы','Assets/StreamingAssets/Config/*.json','Авторитетные данные при запуске в Editor. Можно редактировать и текст JSON прямо в Inspector.'),
('main.json','Config/main.json','Общий снимок: schemaVersion=1 и managers.<раздел>. Генерируется кнопкой сохранения. Не является вторым слоем переопределений.'),
('История','Config/History/<раздел>/','Самостоятельные JSON выбранного Manager. Название содержит время UTC.'),
('Префабы Manager','Assets/Prefabs/Managers/','Черновики типизированных параметров и точки предпросмотра.'),
('Резерв для ошибок','Resources/ConfigDefaults/','Встроенный рабочий снимок для экрана ошибки. Не основной источник для ручного редактирования.'),
('Настройки игрока','Application.persistentDataPath/user-settings.json','Экран, FPS, звук, подсветка и шейдер. Меняются из главного меню или ESC.'),
('Будущий Player','Config рядом с EXE','Числа и ссылки на включённые ассеты можно будет менять без пересборки. Новый ассет сначала импортируется в Unity и включается в будущий билд.')
],('Что','Путь / структура','Назначение'),(100,169,242))
coverage['main.json']=['schemaVersion','managers']
body('В main.json находятся разделы cards, decks, rules, audio, vfx, world, presentation, prefabs, playeranimations, events и interface. Поля каждого раздела полностью совпадают с соответствующим рабочим файлом. Общий снимок не нужно редактировать вручную.')
exec(old[old.index("section('cards.json'"):old.index("section('audio.json'")])
section('audio.json','Звуки по событиям','Audio Manager выбирает AudioClip через Object Picker Unity. На событие можно назначить от 0 до N вариантов. При каждом вызове выбирается один случайный вариант. Нулевой список означает тишину.',{
'cues[].action':'ID события. Обязательные существующие действия не удалять; новые ID можно выбрать в событиях, анимациях и стрелке.',
'cues[].bus':'Категория: 0 Effects, 1 Ambience, 2 Voices, 3 Music.',
'cues[].sounds[].clip':'ID импортированного AudioClip. В Manager назначается выбором ассета, а не копированием строки.',
'cues[].sounds[].volume':'Базовая громкость 0..1.',
'cues[].sounds[].randomPitch':'true выбирает случайную высоту из pitchRange при каждом вызове.',
'cues[].sounds[].pitch':'Постоянная скорость/высота, 0.1..3, когда randomPitch=false.',
'cues[].sounds[].pitchRange':'Диапазон x..y, от 0.1 до 3, x <= y.',
'cues[].sounds[].delay':'Задержка начала, 0..10 секунд.'
},[('Громкость','Итог = base volume варианта × ползунок его категории × общая громкость. Категории сохраняют соотношение при изменении общей громкости. В Resources/AudioRig.prefab находятся отдельные объекты Effects, Ambience, Voices и Music.'),('Действия','card.hover, action.invalid, creature.attack, damage.hit, qte.correct, qte.error, turn.start, ambience; S01.launch ... S08.launch и S01.impact ... S08.impact. ambience зациклен. Новый произвольный ID звучит, когда на него ссылается событие, анимация или стрелка.'),('Предпросмотр','Выберите событие в поле «Проверить». Учитываются задержка, случайный вариант, pitch и громкость. Для чтения сэмплов в Editor нужен импорт Load Type = Decompress On Load.')])
exec(old[old.index("section('vfx.json'"):old.index('world_desc=')])
section('world.json','Локация','World Manager управляет метаданными локации и правилами. Положение стола, окружения, точек имени/HP/руки и слотов задаётся в префабах Unity.',{
'id':'Стабильный числовой ID локации.',
'name':'Название локации.',
'timeChanges':'Включить переопределения времени. При false используются rules.json.',
'playersTurnTime':'Время решений одного игрока в секундах; -1 без изменения, иначе 0.1..86400.',
'qte':'Полное время QTE в секундах; -1 оставляет формулу rules.json. Положительное значение заменяет рассчитанную длительность, штраф ошибки сохраняется.',
'revealTime':'Длительность общего показа карты в секундах; -1 без изменения.',
'gameTimer':'Общее время матча, включая розыгрыши и бой. -1 отключает. При истечении среди живых сравниваются HP, лидеры получают очки победы раунда; затем используется обычный итог по очкам.',
'playerRange':'Минимум x и максимум y: 1..8. Предпросмотр может показать до 8; реальная игра допускает только пересечение этого диапазона с 2..4.',
'scale':'Общий масштаб Body в каждом месте: кресло, аватар, точки руки/имени/HP. 0.25..3. Слоты и стол не масштабируются.',
'scene':'ID префаба интерьера. Выберите GameObject из Unity. Интерьер должен содержать BoardTarget с kind=center.',
'events[].id':'Уникальный ID записи вероятности в данной локации.',
'events[].enabled':'Включить данное правило события.',
'events[].eventId':'Событие из events.json; в Inspector выбирается по ID и имени.',
'events[].chance':'Вероятность в процентах, 0..100, на каждый подходящий ход.',
'events[].everyTurns':'Проверять раз в N ходов игроков, 1..1000. Первая проверка на первом ходу.',
'events[].randomDuration':'Случайная длительность вместо duration.',
'events[].duration':'Число последовательных ходов любых игроков, 1..1000.',
'events[].durationRange':'Целочисленные x..y включительно, 1..1000.'
},[('Срок действия','Длительность 2, начавшаяся на ходу 1, действует на ходах 1 и 2 и заканчивается перед ходом 3. Активная запись не накладывается сама на себя и не продлевается новым броском. Разные записи могут существовать одновременно.'),('Рассадка','SeatingLayout создаёт экземпляры одного HeroSittingPlace, варианта PlayerSeat, по окружности. Радиус, начальный угол и направление редактируются в SeatingLayout на TableWorld. Сохранённые сцены не содержат отдельных наборов для 2, 3 и 4 игроков.')])
section('events.json','Механики событий','Events Manager: название, описание, поддерживаемая механика, сила, VFX и звуковое событие. В локации остаются только ссылка, шанс, частота и длительность.',{
'events[].id':'Уникальный числовой ID события; на него ссылается world.json.',
'events[].name':'Название в списках и журнале.',
'events[].description':'Текст правила в журнале. Обновляйте его вместе с параметрами.',
'events[].mechanic':'0 QteUnitDamage: урон случайному существу за верные символы. 1 TurnHeroHeal: лечение активного героя. 2 TurnHeroDamage: урон активному герою в начале хода.',
'events[].everySymbols':'Для QteUnitDamage: каждый N-й верный символ текущего QTE, 1..50. Ошибки не считаются. При пустом столе урон пропускается.',
'events[].value':'Величина урона/лечения, 0..100. Урон существу от локации не является атакой существа.',
'events[].vfx':'Префаб эффекта в точке получателя, можно оставить пустым.',
'events[].sound':'ID события audio.json, выбирается из списка, можно без звука.',
'events[].vfxScale':'Масштаб VFX, 0.001..100.',
'events[].lifetime':'Срок жизни VFX, 0.01..30 секунд.'
},[('Тестовое событие','«Да не бомбит у меня!»: каждый второй верный символ QTE наносит 1 урон случайному существу. Запись в world.json по умолчанию выключена, шанс 30%, длительность 2 хода. Чтобы проверить механику, включите её и начните новый матч; кнопка Events Manager показывает только VFX/SFX.'),('Добавление механик','Можно создавать события с комбинациями поддерживаемых трёх механик и параметров. Новая произвольная механика требует реализации в коде; текст описания сам по себе не выполняется.')])
section('playeranimations.json','Анимации персонажей','PlayerAnimations Manager задаёт последовательности AnimationClip для игровых состояний. Можно менять скорость и повтор, добавлять эффект и звуковое событие к началу каждого шага.',{
'states[].id':'Состояние: idle, turn, attack, hit, dead, revive, cameraForward, cameraBack, dizzy, reaction, victory. Обязательные: первые шесть.',
'states[].steps[].clip':'AnimationClip из проекта Unity. Выбирается через Object Picker; ID содержит GUID и локальный ID клипа.',
'states[].steps[].speed':'Скорость, 0.05..4.',
'states[].steps[].loop':'Повтор последнего шага. Зацикливание промежуточного шага запрещено, иначе следующие никогда не начнутся.',
'states[].steps[].vfx':'Необязательный префаб VFX в начале шага.',
'states[].steps[].sound':'Звуковое событие audio.json в начале шага.',
'states[].steps[].effectOffset':'Vector3: смещение эффекта от корня персонажа.',
'states[].steps[].effectScale':'Масштаб эффекта, 0.001..100.',
'states[].steps[].effectLifetime':'Срок эффекта, 0.01..30 секунд.'
},[('Порядок','Следующий шаг начинается после длины клипа / speed. Незавершённая разовая последовательность не перебивается обычным idle. Новое игровое действие может прервать её. VFX/SFX вызываются при входе в шаг, а не на каждом обороте loop.'),('Предпросмотр','Выберите состояние и героя, нажмите «Показать / воспроизвести». Редактор проигрывает клип вне Play. Для бесконечного повторения используйте loop последнего шага; «Остановить и очистить» остановит и анимацию, и её VFX/SFX.')])
section('interface.json','Веера, стрелки, HP','Interface Manager задаёт общий профиль для UI и пространственных представлений. Веера используют один радиус и раскрытие; UI показывает их спроецированными на экран.',{
'fan.radius':'Общий радиус дуги, 0.1..10.',
'fan.spread':'Максимальное раскрытие, 0..180 градусов.',
'fan.maximumStep':'Максимальный угол между соседними картами, 1..90 градусов.',
'fan.cardSize':'Размер карты в UI, x/y 1..1000.',
'fan.uiUnitsPerMetre':'Масштаб общей дуги в логических пикселях, 10..600.',
'fan.uiPerspective':'Сжатие вертикальной дуги и наклона UI, 0.05..1. Не поворачивает мировые карты к камере.',
'fan.worldCardSize':'Размер рубашек у аватара, 0.01..10 единиц сцены.',
'arrow.width':'Ширина экранной стрелки.',
'arrow.headLength':'Длина наконечника UI.',
'arrow.headWidth':'Полуширина наконечника UI.',
'arrow.worldWidth':'Базовая ширина пространственной стрелки.',
'arrow.worldHeadLength':'Длина её наконечника.',
'arrow.worldHeadWidth':'Ширина её наконечника.',
'arrow.uiMaterial':'Материал Graphic экранной стрелки; пусто сохраняет стандартный UI.',
'arrow.worldMaterial':'Материал LineRenderer пространственной стрелки; пусто сохраняет префаб.',
'arrow.beginEffect':'Необязательный VFX начала протягивания.',
'arrow.dragEffect':'Необязательный VFX, следующий за указателем.',
'arrow.selectEffect':'Необязательный VFX подтверждения цели.',
'arrow.beginSound':'Звук начала из audio.json.',
'arrow.dragSound':'Звук при начале протягивания, не на каждом кадре.',
'arrow.selectSound':'Звук подтверждения цели.',
'arrow.effectScale':'Масштаб эффектов стрелки.',
'arrow.effectLifetime':'Длительность разовых эффектов.',
'arrow.color':'Цвет предпросмотра общей стрелки. В игре личный цвет выбирается в настройках.',
'healthSize':'Ширина x и высота y полосы HP, 1..1000.',
'healthColor':'Цвет заполнения HP, RGBA.',
'healthTrailColor':'Цвет запаздывающей полосы урона, RGBA.',
'healthUsesPlayerColor':'Использовать цвет места игрока для HP вместо healthColor.',
'highlightMaterial':'Необязательный материал доступных карт и выбранного режима.',
'highlightColor':'Цвет нового личного конфига. Уже выбранный игроком цвет сохраняется в user-settings.json.'
},[('Стрелка','TargetArrowGraphic и WorldArrowView используют один профиль, но разные рендереры Unity: Graphic для Canvas и LineRenderer в мире. По умолчанию дополнительные эффекты и звуки стрелки выключены.'),('Привязки к месту','Hand anchor, Name anchor и Health anchor находятся внутри Body общего PlayerSeat. Перемещайте их в Prefab Mode. Имя и HP больше не имеют отдельных копий в GameInterface.')])
exec(old[old.index("section('presentation.json'"):old.index("story.append(PageBreak());heading('Параметры префабов')")])
sub('CardDepthVisual')
body('pointerInfluence редактируется непосредственно на карточном префабе, обычно 0..1. Значение удалено из prefabs.json, поэтому загрузка больше его не затирает. Общие cardDepth/cardFoil остаются в presentation.json.')
start=old.index("story.append(PageBreak());heading('Параметры префабов')")
end=old.index("section('assets.json'")
block=old[start:end]
block=block.replace("sub(typ)","sub(typ)\n if typ in ('HandFan','WorldHandFanSettings','PlayerStatusView','TargetArrowGraphic','WorldArrowView','MatchOptionsView'):body('Это совместимые поля старого профиля. Размеры/дуга веера, HP, материалы/размеры стрелок и цвет выбора теперь берутся из interface.json или личных настроек. Для новых правок используйте Interface Manager; расположение элементов - через префабы.')")
block=block.replace(' sub(typ)', ' start_group=len(story)\n sub(typ)').replace(' table(rows)\ncoverage', ' table(rows)\n story[start_group:]=[KeepTogether([part for item in story[start_group:] for part in (item._content if isinstance(item,KeepTogether) else [item])])]\ncoverage')
exec(block)
sub('Что означают компоненты')
body('TavernPanel рисует декоративную рамку панели: кайму, отступ и форму углов. TurnBudgetView показывает доступные маршруты расхода хода: 3 заклинания либо существо + заклинание, или остаток QTE в режиме Полководцев.')
section('assets.json','Справочник ассетов','Генерируемый реестр импортированных ассетов. В Manager выбирайте AudioClip, AnimationClip, Material или GameObject через обычное поле Unity; регистрация ID выполняется автоматически.',{
'assets[].id':'Устойчивый ID, используемый рабочими JSON.',
'assets[].kind':'audio / texture / prefab / interior / animation / material.',
'assets[].path':'Путь исходного ассета Unity. Информационный; ручная правка не импортирует внешний файл.'
},[('Новый звук','Сначала добавьте файл в Assets. Затем выберите AudioClip в Audio Manager. Игра не загружает внешние WAV по произвольным путям.'),('Реестр','Resources/ConfigAssets.asset разрешает ID в объекты. Summoners Table > Config > Refresh asset IDs обновляет текстовый справочник assets.json и сохраняет ранее назначенные анимации, материалы и интерьеры.')])
story.append(PageBreak());heading('Личные настройки игрока');body('user-settings.json')
body('Сохраняется автоматически в Application.persistentDataPath. В Windows обычно %USERPROFILE%/AppData/LocalLow/<CompanyName>/<ProductName>. Настройки экрана применяются к окну только в Player; в Editor разрешение просмотра задаёт Game View. Ограничение FPS и громкость действуют и в Editor.')
preferences={
'schemaVersion':('1','Версия структуры, оставлять 1.'),
'screenMode':('2','0 полный экран; 1 оконный без рамки; 2 оконный с рамкой.'),
'width / height':('1600 / 900','Разрешение. Ширина 640..7680, высота 480..4320.'),
'refreshRate':('60','Предпочтительная частота дисплея, 24..500 Гц; фактический режим зависит от монитора.'),
'frameLimit':('60','Ограничение FPS: 1..500 либо -1 без ограничения. В меню предложены стандартные значения.'),
'shader':('0','0 без шейдера; 1 шейдер 1; 2 шейдер 2.'),
'master':('0.8','Общая громкость 0..1, умножает все категории.'),
'effects':('0.75','Множитель эффектов 0..1.'),
'ambience':('0.65','Множитель окружения 0..1.'),
'voices':('0.8','Множитель голосов 0..1.'),
'music':('0.5','Множитель музыки 0..1.'),
'highlightColor':('RGBA','Личный цвет подсветки доступных карт и выбора режима. RGB настраивается тремя ползунками.')}
table([(k,*v) for k,v in preferences.items()])
coverage['user-settings.json']=list(preferences)
sub('Сохранение')
body('Громкость, шейдер и цвет сохраняются при изменении. Для параметров экрана есть кнопка «Применить экран». Включите желаемое разрешение и частоту, затем подтвердите этой кнопкой. Повреждённый личный файл заменяется безопасными значениями в памяти; игровые JSON при этом не меняются.')
story.append(PageBreak());heading('Практические примеры')
for title,text in [
('Пять мест без запуска игры','Откройте LocationLab. Выберите World Manager. В поле «Количество мест» выставьте 5, либо увеличьте максимальное число в диапазоне до 5. Пять экземпляров общего HeroSittingPlace появятся вокруг стола. Для реального матча по-прежнему доступны 2-4 участника.'),
('Случайный звук наведения','Audio Manager > cues > card.hover > sounds: добавьте 2-3 импортированных AudioClip. Задайте volume, включите randomPitch и диапазон, например 0.92..1.08. Нажмите «Показать / воспроизвести» несколько раз.'),
('Событие на два хода','Events Manager определяет механику; World Manager > events включает ссылку. chance=100 удобно для проверки, everyTurns=1, duration=2. Сохраните оба Manager и начните новый матч в Play.'),
('Анимация из двух шагов','PlayerAnimations Manager > нужное состояние > steps: первый клип без loop, второй с loop. При необходимости назначьте звук и эффект каждому шагу. Воспроизведите, затем остановите и очистите.'),
('Крупнее шкала HP','Interface Manager > healthSize: увеличьте ширину x и высоту y, задайте healthColor. В CardsLab можно посмотреть полосу рядом с веером и стрелкой. Положение на теле меняется через Health anchor в PlayerSeat.'),
('Работа с копиями','Перед крупной правкой нажмите «Создать копию». «Откатиться» загрузит последнюю копию только выбранного раздела. Если она верна, нажмите «Сохранить». Соседние карты/аудио/локация не перезаписываются.')
]:sub(title);body(text)
sub('Ограничения проверки')
body('Проверки запускаются в Unity Editor. Новый Player не собран. Сериализация сетевых данных проверяется автоматически; совместный Steam-матч с нескольких ПК требует отдельного игрового теста. Протокол 9. Хеш игрового набора учитывает карты, колоды, правила, таймеры и механики локации; косметика может различаться.')
def footer(c,doc):
 c.setStrokeColor(colors.HexColor('#d9e5e9'));c.line(42,39,553,39)
 c.setFont('Arial',8);c.setFillColor(colors.HexColor('#687b8a'));c.drawString(42,26,'За одним столом · Manager / Config 0.8.0');c.drawRightString(553,26,str(doc.page))
 c.setFillColor(colors.HexColor('#148c7e'));c.rect(0,833,595.28,9,fill=1,stroke=0)
doc=SimpleDocTemplate(str(OUT),pagesize=(595.28,841.89),leftMargin=42,rightMargin=42,topMargin=45,bottomMargin=53,title='За одним столом - Manager и JSON 0.8',author='GameJams')
doc.build(story,onFirstPage=footer,onLaterPages=footer)
reader=PdfReader(OUT);text='\n'.join(page.extract_text() for page in reader.pages)
for name in coverage:assert name in text,name
assert len(coverage)==14,coverage.keys()
report={'pdf':str(OUT),'pages':len(reader.pages),'documentedFiles':len(coverage),'parameterPaths':sum(map(len,coverage.values())),'coverage':coverage,'sha256':hashlib.sha256(OUT.read_bytes()).hexdigest()}
(ROOT/'output/tests/manager-pdf-coverage.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in report.items() if k!='coverage'},ensure_ascii=False))
