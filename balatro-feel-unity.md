# Balatro Feel — Unity: разбор реализации и настройки проекта

Разобран commit [`aa2c8acd0202126c788c0ff081692c9b69762961`](https://github.com/mixandjam/Balatro-Feel/tree/aa2c8acd0202126c788c0ff081692c9b69762961), дата разбора 24.09.2026. Числа ниже взяты из C#, префабов, основной сцены и assets. Значения из C# и сохранённые overrides обозначены отдельно. Это статический разбор: Unity Editor в ходе подготовки документа не запускался.

## 1. Что обеспечивает ощущение карты

Три независимых уровня:

1. **Слот** — место в горизонтальной раскладке, которое не уезжает за мышью.
2. **Card** — прозрачная область ввода 150 × 200; хранит выбор и позицию, перемещается при drag.
3. **CardVisual** — отдельный объект в общем VisualHandler. Его изображение догоняет Card, вращается, масштабируется и имеет независимую тень.

При перестановке меняется принадлежность Card к слоту. Визуал остаётся в VisualHandler и продолжает следовать за Card. Поэтому layout может мгновенно переставить логические позиции, а изображение плавно доезжает до них. Если вложить визуал внутрь движущейся Card без компенсации координат, это разделение исчезнет.

```text
EventSystem: StandaloneInputModule, Drag Threshold = 30
Main Camera: Orthographic, Size = 5, position = (0,0,-5)
Canvas: World Space, scale ≈ 1/108, size = 1900×1080
├─ Trippy-BG, Deck, декоративные элементы
├─ PlayingCardGroup: HorizontalLayoutGroup + HorizontalCardHolder
│  ├─ CardSlot [tag Slot]
│  │  └─ A (Card.prefab): Card + прозрачный Image
│  └─ ... 8 слотов создаются в Start
├─ JokerCardGroup: такая же логика, 4 слота
├─ ConsumableGroup: такая же логика, 2 слота
└─ VisualHandler: VisualCardsHandler
   └─ CardVisual: CardVisual + Canvas
      └─ ShakeParent: короткие punch-импульсы
         ├─ Shadow: Image + Canvas + RotationConstraint
         └─ TiltParent: ручной наклон, idle, угол веера
            └─ Sprite: Image + ShaderCode
```

`VisualCardsHandler` почти пуст: устанавливает `instance` в `Awake`; служит точкой размещения визуалов. В `Card.Start` объект всё равно ищется через `FindObjectOfType`, а не через этот singleton.

## 2. Версии, сцена и настройка с нуля

| Настройка | Значение в репозитории |
|---|---|
| Unity | 2022.3.16f1 |
| Universal RP | 14.0.9 |
| uGUI | 1.0.0 |
| Cinemachine | 2.9.7 |
| Tween library | DOTween в Assets/Plugins/Demigiant, не пакет manifest |
| Сцена в Build Settings | Assets/Scenes/Balatro-Feel.unity |
| Input | Старый `Input` и `StandaloneInputModule` |
| Drag threshold EventSystem | 30 экранных px |
| Camera projection | Orthographic, Size 5, Near 0.3, Far 1000 |
| Camera position | (0,0,-5) |
| Canvas mode | World Space (`m_RenderMode: 2`) |
| Canvas position / scale | (0,0,-4) / (0.009259259, 0.009259259, 0.009259259) |
| Canvas RectTransform | 1900 × 1080; pivot (0.5,0.5) |
| Canvas Event Camera | Main Camera |
| Canvas sorting | Default, order 0 |

CanvasScaler сериализован как Scale With Screen Size, reference 1920 × 1080, Match Height = 1, Reference PPU = 100, Dynamic PPU = 1. Но World Space Canvas не следует трактовать как обычный адаптивный экранный Canvas: размер и масштаб его RectTransform здесь принципиальны.

У Canvas есть GraphicRaycaster: Ignore Reversed Graphics = true; Blocking Objects = Two D; Blocking Mask = Everything. Для собственного чистого UI без 2D-коллайдеров Blocking Objects можно поставить None — это изменение настройки, не значение исходника.

`PlayingCardGroup`: anchors (0.5,0), position (-131.91,216), size (695.66,200), 8 карт. `JokerCardGroup`: anchors (0.5,1), position (-170.69,-213), size (618.09,200), 4 карты. `ConsumableGroup`: anchors (0.5,1), position (370.34,-213), size (346.62,200), 2 карты. У всех трёх HorizontalLayoutGroup: Middle Center; padding = 0; spacing = 0; Control Child Size Width = on, Height = off; Child Force Expand Width = on, Height = off; Use Child Scale = off. Нулевая исходная ширина Slot растягивается layout; размер Card остаётся 150 × 200, поэтому карты перекрываются.

Порядок сборки: импортировать зависимости → добавить tag Slot → собрать Card → вложить его в CardSlot → собрать отдельный CardVisual → назначить ссылки → поставить HorizontalCardHolder на контейнер → назначить slotPrefab. Использовать исходный `CurveParameters.asset`, если нужна точная форма дуги.

## 3. Слои, сортировка, raycast и тень

GameObject Layer, Sorting Layer, Canvas sortingOrder и UI raycast — разные механизмы.

| Объект/механизм | Точная настройка | Назначение |
|---|---|---|
| Card, Slot, CardVisual и его дети | GameObject Layer = UI (5) | Маска камеры/физических запросов; не порядок UI |
| CardSlot | Tag = Slot | Проверки `CompareTag` в Card |
| Sorting layers проекта | Default; EffectLayer (ID 3943087487) | EffectLayer объявлен; исследованные Canvas карт используют Default |
| Card.Image | Color alpha = 0; Raycast Target = true | Невидимая область ввода |
| CardVisual.Canvas | Default; Order = 1; Override Sorting = false | Обычно наследует родительский порядок; при drag override включается |
| Shadow.Canvas | Default; Order = -20; Override Sorting = true | В покое тени выделены в нижний порядок |
| Shadow.Image | Чёрный; alpha 0.53333336; Raycast Target = false | Тень не должна перехватывать ввод |
| Sprite.Image | Raycast Target = true в префабе | У CardVisual нет отдельного GraphicRaycaster; не считать этот флаг маршрутизацией в Card |
| CardVisual.CanvasGroup | Компонент disabled; alpha 0, blocksRaycasts true | Не является активным способом отключения ввода |
| Shadow.RotationConstraint | Active, Locked, Weight 1; только ось Z; source = TiltParent | Тень повторяет поворот в плоскости, не наклоны X/Y |

Shadow и TiltParent — соседние дети ShakeParent. Размер Sprite/Shadow — 142 × 190; у Shadow локальный offset (0,-6). При нажатии добавляется ещё (0,-20), получается (0,-26). `shadowCanvas.overrideSorting = false` возвращает тень в порядок родительского визуала; при отпускании offset восстанавливается до -6, override снова true. Это создаёт впечатление поднятой над столом карты.

На BeginDrag `Card` выключает GraphicRaycaster ближайшего родительского Canvas и свой raycastTarget. Это затрагивает весь этот Canvas. Визуальный BeginDrag включает overrideSorting, использующий уже сохранённый order = 1; сам код не устанавливает order = 1000. На EndDrag оба механизма возвращаются обратно.

Для отдельной реализации с независимыми UI-панелями лучше управлять только областью ввода карты и захваченным pointerId, а визуальные Image явно сделать Raycast Target = false. Это предлагаемый рефакторинг исходника.

## 4. Все основные параметры: C# против Inspector

| Поле | Default C# | Сохранённое значение | Единицы / смысл |
|---|---:|---:|---|
| Card.moveSpeedLimit | 50 | 50 | world units/s; движение логической карты |
| Card.selectionOffset | 50 | **25** | локальные UI units; постоянный подъём выбора |
| Card.instantiateVisual | true | нет override в prefab | Создание отдельного визуала |
| CardVisual.followSpeed | 30 | **25** | Коэффициент Lerp в секунду |
| rotationAmount | 20 | **70** | Градусы на world unit ошибки по X |
| rotationSpeed | 20 | **50** | Сглаживание rotationDelta |
| autoTiltAmount | 30 | **15** | Градусы синусоидального idle X/Y |
| manualTiltAmount | 20 | **30** | Градусы на world unit смещения курсора |
| tiltSpeed | 20 | **40** | Скорость X/Y; Z использует половину |
| scaleAnimations | true | true | Включение части scale-анимаций |
| scaleOnHover | 1.15 | **1.07** | Масштаб hover |
| scaleOnSelect | 1.25 | **1.2** | Масштаб press/drag, не постоянный selected scale |
| scaleTransition | 0.15 | 0.15 | Секунды |
| scaleEase | OutBack | OutBack (enum 27) | Перелёт и возврат |
| selectPunchAmount | 20 | 20 | UI units краткого толчка |
| hoverPunchAngle | 5 | 5 | Градусы |
| hoverTransition | 0.15 | 0.15 | Секунды |
| swapAnimations | true | true | Включение swap punch |
| swapRotationAngle | 30 | **20** | Градусы |
| swapTransition | 0.15 | **0.2** | Секунды |
| swapVibrato | 5 | 5 | Параметр vibrato DOTween |
| CurveParameters.positioningInfluence | 0.1 | **0.02** | World units на интервал между картами |
| CurveParameters.rotationInfluence | 10 | **1.2** | Градусы на интервал |
| Holder.cardsToSpawn | 7 | **8 / 4 / 2** | PlayingCardGroup / JokerCardGroup / ConsumableGroup |
| Holder.tweenCardReturn | true | true | Drop длится 0.15 s, иначе 0 |

В коде также зашиты: click duration ≤ 0.2 s; shadowOffset 20; movementDelta smoothing 25; предел roll ±60°; hover уменьшает idle до 0.2; select punch position vibrato 10 / elasticity 1; hover/select punch rotation vibrato 20 / elasticity 1; startup delay индексов 0.1 s realtime.

## 5. Движение, веер и единицы измерения

### 5.1 Логическая карта

Алгоритм `Card.Update` во время drag эквивалентен ограничению длины шага:

```csharp
Vector2 target = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - offset;
Vector2 current = transform.position;
Vector2 step = Vector2.ClampMagnitude(target - current, moveSpeedLimit * Time.deltaTime);
transform.position = new Vector3(current.x + step.x, current.y + step.y, 0);
```

Это поясняющая запись формулы исходника. В самом исходнике используется normalised direction, `Min(limit, distance/dt)` и `Transform.Translate`. `ClampPosition()` вызывается до движения, поэтому координата после шага может выйти за границу до следующего кадра. Clamp учитывает центр, не размер и масштаб изображения.

`ScreenToWorldPoint` здесь опирается на конкретную ортографическую камеру. При переносе на Screen Space UI надо использовать `RectTransformUtility.ScreenPointToLocalPointInRectangle` с `eventData.pressEventCamera` (для Overlay — null), затем работать в координатах общего RectTransform.

### 5.2 Веер

В исходнике `SiblingAmount()` возвращает **N−1**, хотя название похоже на число соседей. Пусть `m=N−1`, `t=i/m`:

```text
curveY = positioning.Evaluate(t) × 0.02 × m
если m < 5: curveY = 0          # то есть для N ≤ 5!
curveZ = rotation.Evaluate(t) × 1.2 × m
targetVisual = Card.worldPosition + (0, isDragging ? 0 : curveY, 0)
```

Ключи positioning: (0,0), (0.5,1), (1,0); крайние tangents примерно +6.961761 и −6.8714356, weighted outer handles ≈0.20077813 / 0.20233536. Ключи rotation: (0,+1), (0.5,0), (1,−1), на внутренних сегментах slope −2. Для точного совпадения импортировать asset, а не подменять positioning параболой.

Для N=8 максимум высоты кривой при t=0.5 равен 0.14 world units; конкретного центрального слота при чётном N нет. При масштабе Canvas 1/108 это максимум 15.12 UI units. Крайние углы ±8.4°. Для N=4 вертикальная дуга отключена, но углы ±3.6° остаются.

При N=1 текущая `NormalizedPosition()` делит на ноль. Корректный вариант:

```csharp
float NormalizedIndex(int index, int count) => count <= 1 ? 0.5f : (float)index / (count - 1);
```

### 5.3 Четыре функции CardVisual.Update

Порядок строго такой: `HandPositioning → SmoothFollow → FollowRotation → CardTilt`.

```text
visual.position = Lerp(visual.position, targetVisual, 25×dt)
movement = visual.position − Card.position
movementDelta = Lerp(movementDelta, movement, 25×dt)
movementRotation = (dragging ? movementDelta : movement) × 70
rotationDelta = Lerp(rotationDelta, movementRotation, 50×dt)
visual.worldEulerZ = Clamp(rotationDelta.x, −60, +60)
```

Это наклон по ошибке положения, а не вычисленная физическая угловая скорость. Unity Lerp ограничивает alpha в [0,1]. Для другого движка можно сохранить это поведение либо использовать `alpha=1-exp(-speed*dt)` для более устойчивого поведения на разных FPS; последний вариант немного меняет отклик.

`CardTilt`: пока нет drag, сохраняет индекс; при drag фаза остаётся прежней, поэтому swap не перескакивает фазой idle. Затем:

```text
idleFactor = hovering ? 0.2 : 1
sine = sin(Time.time + savedIndex) × idleFactor
cosine = cos(Time.time + savedIndex) × idleFactor
offset = Visual.worldPosition − Mouse.worldPosition
Xtarget = (hovering ? −offset.y × 30 : 0) + sine × 15
Ytarget = (hovering ?  offset.x × 30 : 0) + cosine × 15
Ztarget = dragging ? текущее TiltParent.worldEulerZ : curveZ
X,Y сглаживаются LerpAngle с 40×dt; Z — с 20×dt
```

В исходнике используются **world eulerAngles** и несколько родителей; итог нельзя безоговорочно переписать как сумму всех локальных Euler-углов. В собственном коде удобнее отдельные локальные каналы roll, fan, tilt и punch с одним владельцем каждого свойства.

## 6. События и фактический отклик

| Событие | Состояние Card / Holder | Реакция CardVisual |
|---|---|---|
| PointerEnter | Invoke, затем isHovering=true | Scale 1.07 / 0.15 OutBack; punch Z 5° / 0.15 |
| PointerExit | Invoke, затем isHovering=false | Scale 1, если !wasDragged |
| PointerDown LMB | Invoke, записать Time.time | Scale 1.2; shadowY −=20; shadow override off |
| BeginDrag | Invoke, затем offset, isDragging=true, wasDragged=true, raycast off | Scale 1.2; visual override on |
| PointerUp LMB | Invoke(longPress), затем проверка времени и wasDragged | Scale longPress ? 1.07 : 1.2; shadow restore; visual override off |
| Select | Toggle selected; Invoke(state); изменить Card.localPosition | Punch Y 20 только при выборе; punch Z 2.5°; **Scale 1.07** |
| EndDrag | Holder запускает return; Card сбрасывает isDragging и raycast | Scale 1; visual override off |
| Swap | Переподчинить две Card слотам | У вытесненной карты punch ±20° / 0.2; vibrato 5 |

Выбор разрешён при duration ≤0.2 s и !wasDragged. `wasDragged` сбрасывается корутиной в конце кадра, чтобы отпускание drag не стало кликом. Это две независимые проверки: время нажатия и факт drag. Drag запускает EventSystem по порогу расстояния 30 px, а не по таймеру 0.2 s.

В исходнике некоторые события вызываются до обновления флагов. Слушателю лучше пользоваться аргументами события либо явно изменить порядок при рефакторинге. `Deselect()` сбрасывает позицию, но не вызывает SelectEvent; это тоже нужно учесть.

## 7. Функции контейнера и перестановка

`Start`: создаёт slots, собирает Card, подписывает hover/drag; спустя 0.1 s обновляет визуальные sibling indices. `BeginDrag` сохраняет `selectedCard` — это здесь перетаскиваемая карта, а не все выбранные. `EndDrag` tween-ит localPosition к (0,25,0) или zero за 0.15 s OutBack, затем сбрасывает selectedCard.

В `Update` Delete удаляет слот hoveredCard; ПКМ вызывает Deselect у всех карт. Во время drag для каждой карты проверяются одновременно её world X и индекс слота:

```text
если dragged.x > other.x И dragged.slotIndex < other.slotIndex: Swap(other); break
если dragged.x < other.x И dragged.slotIndex > other.slotIndex: Swap(other); break
```

`Swap(index)`:

1. Запомнить oldSlot перетаскиваемой и crossedSlot другой карты.
2. `other.SetParent(oldSlot)`; установить other.localPosition = selected ? (0,25,0) : zero.
3. `dragged.SetParent(crossedSlot)` с сохранением world position (default SetParent).
4. Определить направление из новых индексов; вызвать `other.cardVisual.Swap(±1)`.
5. Для всех visual вызвать UpdateIndex — sibling order берётся от индекса родительского Slot.

Список `cards` физически не пересортировывается; порядок определён parent indices. `isCrossing` устанавливается и сбрасывается внутри синхронного Swap. Ограничение «один swap за Update» обеспечено `break`, а не длительной блокировкой на время анимации.

## 8. Точная сборка материалов

На Sprite назначен материал CardShaderGraph; `ShaderCode.Start` создаёт **индивидуальный Material**, назначает его Image, сбрасывает keywords и случайно выбирает из массива `REGULAR, POLYCHROME, REGULAR, NEGATIVE`. Получаются вероятности 50%, 25%, 25%. В графе есть FOIL, но этот код его никогда не выбирает.

Каждый кадр ShaderCode берёт `transform.parent.localRotation` — TiltParent, нормализует X/Y до знаковых углов и ограничивает ±90°. Далее:

```text
_Rotation.x = Remap(angleX, −20, 20, −0.5, 0.5) = angleX / 40
_Rotation.y = Remap(angleY, −20, 20, −0.5, 0.5) = angleY / 40
```

Remap не делает clamp: при 40° параметр равен 1, не 0.5.

| Свойство материала | Значение | Применение |
|---|---:|---|
| _MainTex | sprite texture | RGB лица и отдельная alpha |
| _EDITION | Enum keyword | Regular / Polychrome / Foil / Negative |
| _Rotation | (0,0) до обновления | Вход наклона для Polychrome и Negative |
| _poly_frequency | 1 | Частота полихромного рисунка |
| _poly_power | 0.2 | Сила полихромного вклада |
| _poly_brightness | 0.76 | Яркость; graph default 0.7 переопределён материалом |

CardShaderGraph имеет Universal Sprite Lit target. BaseColor получает выход Edition, alpha берётся отдельно из Sample Texture 2D исходного изображения. Polychrome использует Twirl, Hue, Channel Mixer, Contrast, Sine и Blend; Rotation двигает рисунок через Twirl offset и hue-ветки. Negative сочетает инверсию, Contrast/Saturation, Twirl и Voronoi-маску. Foil использует Twirl → Split.R → Sine и окрашивание текстуры; **в главном графе его вход Rotation не подключён**, даже несмотря на существование этого входа в subgraph.

BG-Shader: Time двигает UV через Tiling And Offset и Rotate; шум добавляется к UV перед Twirl, другая ветка Simple Noise → Power модулирует выборку текстуры; ещё одна Simple Noise → Posterize смешивается через Blend. Это отдельный декоративный материал фона, не часть drag-системы.

При переносе не копировать «осиротевшие» свойства из material YAML как реально работающие controls: там встречаются старые _Power, _Frequency, _DisplacementIntensity и другие, но актуальные exposed properties нужно сверять с графом и связями.

## 9. Камера и постобработка

Main Camera включает URP post-processing; Volume Layer Mask = Default. Global Volume weight = 1. В profile сохранены:

| Эффект | Active | Параметры |
|---|---|---|
| Bloom | true | threshold 1; intensity 2; scatter 0.2; skipIterations 1 |
| Chromatic Aberration | true | intensity 0.1 |
| Vignette | true | intensity 0.403; smoothness 0.2; center (0.5,0.5); цвет (0,0.21698111,0.14509144) |
| Panini Projection | true | distance 0.2; cropToFit 1 |
| Lens Distortion | **false** | сохранён intensity −0.15, но override выключен целиком |

Наличие active override не гарантирует одинаковую видимую работу в любой камере/renderer: в частности учитывать ортографическую проекцию и возможности конкретной версии URP. Virtual Camera использует noise profile с amplitude gain 0.2 / frequency gain 0.3. Это настройки сцены, не код импульса камеры при выборе: такого вызова в исследованных карточных скриптах нет.

## 10. Что исправить при использовании исходника в своей игре

| Проблема | Конкретное исправление |
|---|---|
| N=1 даёт деление на ноль | NormalizedIndex guard, t=0.5 |
| Глобальные DOTween ID 2/3 затрагивают другие карты | Хранить Tween-ссылки на экземпляре; Kill только их |
| Swap убивает ID 2, но создаёт ID 3 | Управлять swap Tween отдельно и завершать предыдущий канал |
| Несколько DOScale конкурируют | Один scaleTween; Kill(false), новый tween из текущего scale |
| Выключается raycaster всего Canvas | Разделить ввод руки и остальных UI; pointer capture/локальный hit-test |
| Deselect без SelectEvent | Сделать единый SetSelected(bool) с обновлением состояния и события |
| localPosition складывается с world-space up | Использовать локальный Vector3.up либо преобразовать направление в parent-space |
| Задержка Start 0.1 s вместо готовности | Явный Initialize после Instantiate, затем RebuildLayout/UpdateIndex |
| Материал создаётся, но не уничтожается | Хранить originalMaterial, Destroy(instance) в OnDestroy |
| Clamp центра до движения | Clamp после шага, с half extents выбранного визуального состояния |

Пример адресного scale-канала внутри своего Visual-компонента (это улучшение, не цитата исходника):

```csharp
private Tween scaleTween;
private Tween punchTween;

void AnimateScale(float value)
{
    scaleTween?.Kill(false);
    scaleTween = transform.DOScale(value, 0.15f).SetEase(Ease.OutBack);
}

void PunchZ(float angle, float duration, int vibrato)
{
    punchTween?.Kill(false);
    shakeParent.localRotation = Quaternion.identity;
    punchTween = shakeParent.DOPunchRotation(
        Vector3.forward * angle, duration, vibrato, 1f);
}

void OnDestroy()
{
    scaleTween?.Kill(false);
    punchTween?.Kill(false);
    // Здесь также отписать слушатели, если Card переживает visual.
}
```

## 11. Контроль сборки в Editor

Это сценарии для проверки будущей реализации, а не отчёт о запущенных тестах:

- 8 карт: overlap, дуга, крайние углы ±8.4°; 4 карты: высота дуги 0.
- Hover не изменяет hitbox; масштаб 1.07; idle X/Y снижен до 3°.
- Press сдвигает тень с −6 к −26; release возвращает −6.
- Клик 0.1 s поднимает Card на 25; удержание 0.3 s не меняет selected.
- Drag дольше порога 30 px не вызывает selection при release даже при коротком жесте.
- При swap world position dragged остаётся непрерывной, сосед доезжает визуальным follower.
- Повторный press во время return останавливает старый return tween.
- Удаление, пустая рука, одна карта и смена разрешения не оставляют NaN, потерянный raycast или чужие tweens.

## 12. Источники по разделам

- [Card.cs: ввод, координаты, выбор](https://github.com/mixandjam/Balatro-Feel/blob/aa2c8acd0202126c788c0ff081692c9b69762961/Assets/Scripts/Card.cs)
- [CardVisual.cs: follow, tilt, scale, shadow, punch](https://github.com/mixandjam/Balatro-Feel/blob/aa2c8acd0202126c788c0ff081692c9b69762961/Assets/Scripts/CardVisual.cs)
- [HorizontalCardHolder.cs: spawn, swap, return](https://github.com/mixandjam/Balatro-Feel/blob/aa2c8acd0202126c788c0ff081692c9b69762961/Assets/Scripts/HorizontalCardHolder.cs)
- [Префабы: реальные Inspector overrides](https://github.com/mixandjam/Balatro-Feel/tree/aa2c8acd0202126c788c0ff081692c9b69762961/Assets/Prefabs)
- [Основная сцена](https://github.com/mixandjam/Balatro-Feel/blob/aa2c8acd0202126c788c0ff081692c9b69762961/Assets/Scenes/Balatro-Feel.unity)
- [CurveParameters.asset](https://github.com/mixandjam/Balatro-Feel/blob/aa2c8acd0202126c788c0ff081692c9b69762961/Assets/Scriptables/CurveParameters.asset)
- [ShaderCode.cs](https://github.com/mixandjam/Balatro-Feel/blob/aa2c8acd0202126c788c0ff081692c9b69762961/Assets/Scripts/ShaderCode.cs) и [Shader Graph assets](https://github.com/mixandjam/Balatro-Feel/tree/aa2c8acd0202126c788c0ff081692c9b69762961/Assets/Shaders)
- [TagManager: layers/tags](https://github.com/mixandjam/Balatro-Feel/blob/aa2c8acd0202126c788c0ff081692c9b69762961/ProjectSettings/TagManager.asset)

Локальная копия исследованного commit: `O:/Games/anton/project_collector/tmp/balatro-feel-reference`.

## 13. Ключевые C#-функции непосредственно из исходника

Ниже точные тела методов из исследованного commit, без исправлений ошибок, перечисленных выше. Они вставляются в соответствующие исходные классы и используют их поля/ссылки из раздела 4; это не самостоятельные компоненты. Полные классы лежат в локальной копии проекта и по ссылкам раздела 12.

### Card.cs

```csharp
public void OnBeginDrag(PointerEventData eventData)
{
    BeginDragEvent.Invoke(this);
    Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    offset = mousePosition - (Vector2)transform.position;
    isDragging = true;
    canvas.GetComponent<GraphicRaycaster>().enabled = false;
    imageComponent.raycastTarget = false;

    wasDragged = true;
}

public void OnPointerUp(PointerEventData eventData)
{
    if (eventData.button != PointerEventData.InputButton.Left)
        return;

    pointerUpTime = Time.time;

    PointerUpEvent.Invoke(this, pointerUpTime - pointerDownTime > .2f);

    if (pointerUpTime - pointerDownTime > .2f)
        return;

    if (wasDragged)
        return;

    selected = !selected;
    SelectEvent.Invoke(this, selected);

    if (selected)
        transform.localPosition += (cardVisual.transform.up * selectionOffset);
    else
        transform.localPosition = Vector3.zero;
}
```

### CardVisual.cs

```csharp
private void HandPositioning()
{
    curveYOffset = (curve.positioning.Evaluate(parentCard.NormalizedPosition()) * curve.positioningInfluence) * parentCard.SiblingAmount();
    curveYOffset = parentCard.SiblingAmount() < 5 ? 0 : curveYOffset;
    curveRotationOffset = curve.rotation.Evaluate(parentCard.NormalizedPosition());
}

private void SmoothFollow()
{
    Vector3 verticalOffset = (Vector3.up * (parentCard.isDragging ? 0 : curveYOffset));
    transform.position = Vector3.Lerp(transform.position, cardTransform.position + verticalOffset, followSpeed * Time.deltaTime);
}

private void FollowRotation()
{
    Vector3 movement = (transform.position - cardTransform.position);
    movementDelta = Vector3.Lerp(movementDelta, movement, 25 * Time.deltaTime);
    Vector3 movementRotation = (parentCard.isDragging ? movementDelta : movement) * rotationAmount;
    rotationDelta = Vector3.Lerp(rotationDelta, movementRotation, rotationSpeed * Time.deltaTime);
    transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, Mathf.Clamp(rotationDelta.x, -60, 60));
}

private void CardTilt()
{
    savedIndex = parentCard.isDragging ? savedIndex : parentCard.ParentIndex();
    float sine = Mathf.Sin(Time.time + savedIndex) * (parentCard.isHovering ? .2f : 1);
    float cosine = Mathf.Cos(Time.time + savedIndex) * (parentCard.isHovering ? .2f : 1);

    Vector3 offset = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
    float tiltX = parentCard.isHovering ? ((offset.y * -1) * manualTiltAmount) : 0;
    float tiltY = parentCard.isHovering ? ((offset.x) * manualTiltAmount) : 0;
    float tiltZ = parentCard.isDragging ? tiltParent.eulerAngles.z : (curveRotationOffset * (curve.rotationInfluence * parentCard.SiblingAmount()));

    float lerpX = Mathf.LerpAngle(tiltParent.eulerAngles.x, tiltX + (sine * autoTiltAmount), tiltSpeed * Time.deltaTime);
    float lerpY = Mathf.LerpAngle(tiltParent.eulerAngles.y, tiltY + (cosine * autoTiltAmount), tiltSpeed * Time.deltaTime);
    float lerpZ = Mathf.LerpAngle(tiltParent.eulerAngles.z, tiltZ, tiltSpeed / 2 * Time.deltaTime);

    tiltParent.eulerAngles = new Vector3(lerpX, lerpY, lerpZ);
}
```

### HorizontalCardHolder.cs

```csharp
void EndDrag(Card card)
{
    if (selectedCard == null)
        return;

    selectedCard.transform.DOLocalMove(selectedCard.selected ? new Vector3(0,selectedCard.selectionOffset,0) : Vector3.zero, tweenCardReturn ? .15f : 0).SetEase(Ease.OutBack);

    rect.sizeDelta += Vector2.right;
    rect.sizeDelta -= Vector2.right;

    selectedCard = null;

}

void Swap(int index)
{
    isCrossing = true;

    Transform focusedParent = selectedCard.transform.parent;
    Transform crossedParent = cards[index].transform.parent;

    cards[index].transform.SetParent(focusedParent);
    cards[index].transform.localPosition = cards[index].selected ? new Vector3(0, cards[index].selectionOffset, 0) : Vector3.zero;
    selectedCard.transform.SetParent(crossedParent);

    isCrossing = false;

    if (cards[index].cardVisual == null)
        return;

    bool swapIsRight = cards[index].ParentIndex() > selectedCard.ParentIndex();
    cards[index].cardVisual.Swap(swapIsRight ? -1 : 1);

    //Updated Visual Indexes
    foreach (Card card in cards)
    {
        card.cardVisual.UpdateIndex(transform.childCount);
    }
}
```


## 14. Собрать исходные Shader Graph вручную: порты, провода и константы

Этот раздел заменяет краткое перечисление узлов из раздела 8. Нумерация F/P/N/B введена для инструкции: переименуйте группы/добавьте Sticky Notes с этими ID, чтобы отличать одинаковые Multiply. `P03.Out → P04.X` означает провод от выхода Out конкретного узла P03 ко входу X узла P04. Запись `B = 0.45` означает неподключённый числовой вход, а не отдельное свойство Blackboard.

Рецепт рассчитан на Unity 2022.3 / Shader Graph 14 из исходного проекта. Все значения и связи сверены с сериализованными графами. Неподключённые служебные узлы опущены; где это важно, они перечислены отдельно. Вводите числа с точкой. Не добавляйте Saturate/Clamp к результатам цветовых узлов: исходник допускает выход за диапазон 0–1.

### 14.1 Подготовка ассетов и проверка базового изображения

1. В проекте с URP 14 создать папку `Assets/BalatroShaders`. Создать три `Shader Sub Graph`: `Edition-Foil`, `Edition-Polychrome`, `Edition-Negative`. Затем создать `URP Sprite Lit Shader Graph` с именем `CardShaderGraph` и второй такой граф `BG-Shader`.
2. Project Settings → Player → Other Settings → Color Space = **Linear**, как в репозитории (`m_ActiveColorSpace: 1`). Color-узлы далее вводятся в sRGB; Unity сама преобразует их RGB в Linear.
3. Текстура лица, реально используемая CardVisual.prefab: `Assets/Sprites/jammo-variation.psd`. Фон: `Assets/Sprites/Group 3.png`. Обе имеют sRGB on, mipmaps off, Bilinear, Wrap Clamp, Alpha Is Transparency on; тип Sprite (2D and UI). Для другого движка экспортировать лицо в RGBA PNG с сохранённой прозрачностью.
4. На время настройки отключить `ShaderCode` на Sprite: иначе Start случайно переключит edition и каждый Update перезапишет `_Rotation`.
5. В CardShaderGraph Blackboard добавить Texture2D с Display Name `Texture`, Reference **`_MainTex`**. Перетащить свойство на поле → Sample Texture 2D.Texture. UV оставить UV0. RGBA/RGB подключить к Fragment/Base Color, A — отдельно к Fragment/Alpha.
6. Save Asset → Create Material на этом графе → назначить материал в Sprite.Image.Material; Source Image = jammo-variation. Изображение должно оставаться узнаваемым и иметь прозрачный внешний контур. Если оно чёрное в изолированной 2D Sprite Lit сцене, проверить 2D lighting/Global Light 2D и renderer. Для оригинальной uGUI-сцены использовать её настройки renderer из репозитория.
7. После проверки Regular переходить к subgraphs. Не отлаживать одновременно tint, постобработку, tilt и неизвестный graph.

В Blackboard старые default texture GUID некоторых subgraphs указывают на отсутствующий asset. В инструкции все CardTexture подключаются явно; пропавший default использовать не нужно.

### 14.2 Общие правила портов

- `UV0`: вход UV оставить неподключённым у Sample/Twirl либо подключить узел UV с Channel UV0. Это стандартные UV меша, не константа (0,0).
- Scalar → Vector: число размножается по компонентам. Для ручной проверки `v + s` = `v + (s,s,...)`.
- Vector2 → Float: берётся X. В Polychrome это критично для Hue.Offset: после Multiply добавьте Split и используйте R/X явно.
- RGBA → Vector3: берётся RGB, alpha не участвует в покрытии. Alpha для всех editions берётся из исходной текстуры отдельно.
- У `Color` выбирать Mode Default; задавать RGBA через числа. Цвета в таблицах — sRGB input, не hex без указания пространства.
- У `Channel Mixer` нужны все 9 коэффициентов, указанные ниже. Неперечисленные режимы узлов — обычные default; настройки, меняющие математику, прописаны явно.

### 14.3 Edition-Foil: полный subgraph

Blackboard: Texture2D `CardTexture`; Vector2 `Rotation=(0,0)`; Float `Frequency=300`; Float `Birghtness=0.7`; Float `Power=0.2`. Опечатка **Birghtness** — имя в исходнике. Power в этом subgraph не подключён и ничего не меняет. В Output создать выход Vector3 `FinalResult`.

| ID | Создать узел | Подключить входы / установить значения |
|---|---|---|
| F01 | Twirl | UV=UV0; Center=(0.5,0.5); Strength=Frequency; Offset=Rotation |
| F02 | Split | In=F01.Out |
| F03 | Sine | In=F02.R |
| F04 | Multiply | A=F03.Out; B=1 |
| F05 | Color | RGBA=(0.655660391,0.731280088,1,0); Mode Default |
| F06 | Multiply | A=F04.Out; B=F05.Out |
| F07 | Sample Texture 2D | Texture=CardTexture; UV=UV0; Type Default; Sampler default |
| F08 | Multiply | A=F07.RGBA; B=Birghtness |
| F09 | Add | A=F08.Out; B=F06.Out |
| F10 | Multiply | A=F09.Out; B=F05.Out |
| Output | FinalResult Vector3 | F10.Out → FinalResult (RGB) |

На **главном графе** у экземпляра Foil поставить Birghtness=1, Frequency=300, Rotation=(0,0). В оригинале Rotation Foil не связан с `_Rotation`; если подключить, получится дополнительная реакция, которой в исходном главном графе нет. Промежуточный F04 множитель 1 сохранён для узнаваемости исходной цепочки.

Проверка: при Edition=Foil изображение получает голубоватую модуляцию. При неподключённом Rotation эффект не должен меняться от ручного `_Rotation`. Power не влияет — это ожидаемо.

### 14.4 Edition-Polychrome: полный subgraph

Blackboard: Texture2D CardTexture; Vector2 Rotation=(0,0); Float Power=0.3; Float Frequency=1; Float Birghtness=0.7. Главный граф передаст Power=0.2, Frequency=1, Birghtness=0.76 через свои exposed properties. Output: Vector3 FinalResult.

| ID | Узел | Входы / настройки |
|---|---|---|
| P01 | Split | In=Rotation |
| P02 | Multiply | A=P01.G; B=0.45 |
| P03 | Add | A=0.5; B=P02.Out |
| P04 | Vector 2 | X=P03.Out; Y=0.2 |
| P05 | Twirl | UV=UV0; Center=P04.Out; Strength=4; Offset=Rotation |
| P06 | Multiply | A=P05.Out; B=Frequency |
| P06X | Split | In=P06.Out; далее использовать R для float Hue.Offset |
| P07 | Color | RGBA=(1,0.075684220,0,0); Mode Default |
| P08 | Hue | In=P07.Out RGB; Offset=P06X.R; **Range=Normalized** |
| P09 | Channel Mixer | In=P08.Out; коэффициенты из таблицы ниже |
| P10 | Multiply | A=P09.Out; B=Power |
| P11 | Sample Texture 2D | Texture=CardTexture; UV=UV0 |
| P12 | Multiply | A=P11.RGBA; B=Birghtness |
| P13 | Hue | In=P12.Out RGB; Offset=P06X.R; **Range=Normalized** |
| P14 | Add | A=P13.Out; B=P10.Out |
| P15 | Contrast | In=P14.Out; Contrast=1 |
| P16 | Split | In=P12.Out |
| P17 | Multiply | A=P16.G; B=2 |
| P18 | One Minus | In=P17.Out |
| P19 | Multiply | A=P15.Out; B=P18.Out |
| P20 | Channel Mixer | In=P19.Out; те же коэффициенты |
| P21 | Blend | Base=P15.Out; Blend=P20.Out; **Mode=Difference; Opacity=0.2** |
| Output | FinalResult Vector3 | P21.Out → FinalResult |

Оба Channel Mixer, строки являются коэффициентами входных R/G/B:

| Выход | Input R | Input G | Input B |
|---|---:|---:|---:|
| Red | 0.81 | −0.25 | 0.27 |
| Green | −0.12 | 0.65 | 0 |
| Blue | 0 | 0 | 1 |

Если интерфейс версии показывает проценты, это 81%, −25%, 27% и т.д.; в asset хранятся коэффициенты 0.81 и т.д. Формула Red = dot(InputRGB,(0.81,−0.25,0.27)).

В исходнике есть Sine, подключённый к P06, но его выход не доходит до FinalResult: для результата его создавать не требуется. Redirect/reroute-узлы также можно заменить прямыми проводами.

Проверка: Edition=Polychrome; временно `_Rotation=(0,0)`, затем (0.5,0), затем (0,0.5). Поле оттенка должно изменяться в обоих направлениях. Frequency здесь умножает выход Twirl; не подключать её к Twirl.Strength — там фиксировано 4.

### 14.5 Edition-Negative: полный subgraph

Blackboard: Texture2D CardTexture, Vector2 Rotation. В исходном asset ещё объявлены Power=0.87, Frequency=0.35, Birghtness=0.7, но в вычисляющей выход цепочке они **не используются**. Можно оставить для совпадения интерфейса; константы ниже не заменять этими свойствами. Output: Vector3 FinalResult.

| ID | Узел | Входы / настройки |
|---|---|---|
| N01 | Sample Texture 2D | Texture=CardTexture; UV=UV0 |
| N02 | Invert Colors | In=N01.RGBA; Red/Green/Blue=true; Alpha=false |
| N03 | Contrast | In=N02.Out RGB; Contrast=0.7 |
| N04 | Saturation | In=N03.Out; Saturation=2 |
| N05 | Color | RGBA=(0.778301895,0.8308093548,1,1); Mode Default; значения из сериализованного Color node |
| N06 | Multiply | A=N04.Out; B=N05.Out RGB |
| N07 | Twirl | UV=UV0; Center=(0.5,0.5); Strength=0.68; Offset=Rotation |
| N08 | Tiling And Offset | UV=N07.Out; Tiling=(2.79,1); Offset=(2.29,0) |
| N09 | Voronoi | UV=N08.Out; Angle Offset=0; Cell Density=0.28; Hash=Legacy Sine для старого узла |
| N10 | Power | A=N09.Out; B=4 |
| N11 | Multiply | A=N10.Out; B=N01.R |
| N12 | Multiply | A=N11.Out; B=0.3 |
| N13 | Split | In=N02.Out |
| N14 | Power | A=N13.G; B=0.5 |
| N15 | Smoothstep | Edge1=0.04; Edge2=0.14; In=N10.Out |
| N16 | Multiply | A=N14.Out; B=N15.Out |
| N17 | Multiply | A=N16.Out; B=2 |
| N18 | Add | A=N12.Out; B=N17.Out |
| N19 | Add | A=N06.Out; B=N18.Out |
| N20 | Multiply | A=N06.Out; B=N18.Out |
| N21 | Saturation | In=N20.Out; Saturation=5 |
| N22 | Add | A=N19.Out; B=N21.Out |
| N23 | Contrast | In=N22.Out; Contrast=1.1 |
| Output | FinalResult Vector3 | N23.Out → FinalResult |

N01.R используется повторно вместо второго одинакового Sample Texture 2D из оригинала. Это та же текстура с тем же UV0. В оригинале ещё вычисляется Vector2(2.29+Rotation.y*2,0), но его выход не соединён с N08.Offset: Offset N08 остаётся константой (2.29,0). Не подключайте найденный рядом Vector2 «по смыслу» — получится другой graph.

При Angle Offset=0 внутренняя случайная точка Voronoi становится (0.5,1) в каждой ячейке. Значит, этот конкретный Negative не требует временного шума: Time в subgraph отсутствует. Движение рисунка связано с Rotation.

### 14.6 CardShaderGraph: главный граф, keyword и материал

Blackboard:

| Display Name | Type | Reference / default |
|---|---|---|
| Texture | Texture2D | `_MainTex` |
| Rotation | Vector2 | `_Rotation` = (0,0) |
| poly power | Float | `_poly_power` =0.2 |
| poly frequency | Float | `_poly_frequency` =1 |
| poly brightness | Float | `_poly_brightness` graph default=0.7, материал=0.76 |
| Edition | Keyword Enum | Reference `_EDITION`; Definition Shader Feature; Scope Global; entries ниже |

Keyword entries в этом порядке: Regular → REGULAR; Polychrome → POLYCHROME; Foil → FOIL; Negative → NEGATIVE. Перетащить keyword в graph, чтобы получить узел Edition с четырьмя входами.

| Откуда | Куда |
|---|---|
| Texture property | Sample Texture 2D.Texture; CardTexture всех трёх subgraphs |
| Sample Texture 2D.RGBA | Edition.Regular |
| Sample Texture 2D.A | Fragment.Alpha |
| Rotation | Polychrome.Rotation; Negative.Rotation |
| poly power / frequency / brightness | Одноимённые Power / Frequency / Birghtness экземпляра Polychrome |
| Polychrome.FinalResult | Edition.Polychrome |
| Foil.FinalResult | Edition.Foil |
| Negative.FinalResult | Edition.Negative |
| Edition.Out RGB | Fragment.Base Color |

Foil: Rotation=(0,0), Frequency=300, Birghtness=1. Остальные его значения не работают через выход. Fragment Sprite Mask=(1,1,1,1), Normal TS=(0,0,1) по умолчанию; Vertex Position/Normal/Tangent оставить стандартными bindings, не заменять нулевыми константами. Graph target Universal → Sprite Lit; Alpha Clipping off. Не переносить числовое `m_SurfaceType=0` как команду «сделать UI непрозрачным»: Sprite Lit SubTarget задаёт собственную сборку passes.

Создать Material, установить poly brightness=0.76. Если включён ShaderCode, runtime keyword выбирается случайно из REGULAR/POLYCHROME/REGULAR/NEGATIVE. Для проверки Foil временно выключить ShaderCode и выбрать Foil вручную. После проверки вернуть скрипт. При переключении из кода выключить предыдущие `_EDITION_*` keywords, затем включить один нужный. Для runtime-build включить используемые variants, чтобы Shader Feature stripping не удалил никогда не встречавшийся в материалах Foil.

### 14.7 BG-Shader: полный граф фона

Blackboard: Texture2D Texture с Reference `_MainTex`, назначить `Group 3.png`. URP Sprite Lit target; Alpha Clipping on; Fragment Alpha=1; Alpha Clip Threshold=1. Normal TS и Sprite Mask default. Исходная текстура — Bilinear/Clamp, mipmaps off.

| ID | Узел | Входы / настройки |
|---|---|---|
| B01 | Time | Используются выходы Time и Cosine Time |
| B02 | Multiply | A=B01.Time; B=0.01 |
| B03 | Tiling And Offset | UV=UV0; Tiling=(1.5,1); Offset=(B02.Out,B02.Out) |
| B04 | Simple Noise | UV=B03.Out; Scale=16.2; Hash=Deterministic |
| B05 | Rotate | UV=UV0; Center=(B01.Cosine Time,B01.Cosine Time); Rotation=B02.Out; **Unit=Degrees** |
| B06 | Add | A=B04.Out (scalar на оба канала); B=B05.Out |
| B07 | Twirl | UV=B06.Out; Center=(0.5,0.5); Strength=30; Offset=(0,0) |
| B08 | Simple Noise | UV=UV0; Scale=31.3; Hash=Deterministic |
| B09 | Power | A=B08.Out; B=2.14 |
| B10 | Multiply | A=B07.Out; B=B09.Out |
| B11 | Sample Texture 2D | Texture=Texture property; UV=B10.Out |
| B12 | Simple Noise | UV=UV0; Scale=52.8; Hash=Deterministic |
| B13 | Posterize | In=B12.Out; Steps=2.1 |
| B14 | Blend | Base=B11.RGBA; Blend=B13.Out; **Mode=Linear Burn; Opacity=0.05** |
| Fragment | Base Color | B14.Out RGB |

Center B05 действительно движется по `(cos(time),cos(time))`, это не постоянные (0.5,0.5). Steps B13=2.1, не округлять до 2. Linear Burn в этой версии Shader Graph: `lerp(Base, Base+Blend-1, 0.05)`, без дополнительного saturate. Граф не использует alpha sampled texture для выхода: Alpha остаётся 1.

### 14.8 Как диагностировать сборку по промежуточным точкам

Подключать к Base Color по очереди: Regular sample → выход одного subgraph → Edition → итоговый материал. Для subgraph-preview назначать CardTexture явно и использовать те же значения входов, что у экземпляра в основном графе.

| Проверка | Ожидаемое наблюдение |
|---|---|
| Regular | Исходное изображение и исходная alpha без процедурного покрытия |
| P06X.R в grayscale | Плавное поле, меняется от Rotation; не цветная текстура |
| P08.Out | Hue-поле исходного оранжевого Color; Range Normalized |
| P21 против P15 | Добавляется Difference-mix с opacity 0.2; это не Overlay |
| N10.Out в grayscale | Статическая маска при Rotation=(0,0); двигается от Rotation |
| Foil | Сильная пространственная модуляция при Frequency=300; внешняя `_Rotation` не влияет |
| Background | Анимация появляется только в Play/в preview с работающим Time |
| Снаружи силуэта карты | Прозрачно для всех editions; не брать alpha из Color nodes с alpha=0 |

Побитовое/визуальное сравнение в Editor не проводилось. Таблицы выше восстановлены по сохранённым connections и свойствам; проверки здесь предназначены для сборки пользователем.
