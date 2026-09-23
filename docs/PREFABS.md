# Редактируемые префабы · 0.8.0

Для типизированных полей и предпросмотра без Play открывайте **Summoners Table → Authoring** и выбирайте Manager. [Инструкция](AUTHORING.md). Рабочие JSON находятся в `Assets/StreamingAssets/Config`. [Порядок работы](CONFIG.md). При Play эти значения имеют приоритет над полями префабов.

Открывайте префаб двойным щелчком в Project, изменяйте в Prefab Mode и сохраняйте. Для изменения только одной сцены можно сделать override экземпляра. Play Mode не сохраняет правки после остановки.

## Карты

```text
Assets/Prefabs/Editable/Cards/
  CardBase.prefab
    Types/CreatureCard.prefab
      Instances/C01.prefab … C18.prefab
    Types/SpellCard.prefab
      Instances/S01.prefab … S08.prefab
    Types/ReactionCard.prefab
      Instances/R01.prefab … R04.prefab
```

Это цепочки Prefab Variants, а не только папки с похожими именами. Общая структура наследуется от CardBase, цвет типа — от варианта типа, индивидуальные поля — от конкретной карты. Inspector → Overrides показывает, какие значения переопределены.

У CardView три формы: Full face (просмотр), Compact face (рука), Tabletop face (3D-стол). Меняйте размеры, шрифты, изображения, полосы и материалы в соответствующей форме. Компонент CardDisplaySlot масштабирует экземпляр в заданный контейнер. HandFan отвечает за веер, увеличение и расстояния. Library: `Assets/Resources/CardLibrary.asset` — 30 конкретных карт и рубашка.

Числа, описание и поддерживаемая механика карты находятся в `Config/cards.json`. Игра читает их из JSON. `CardView > Definition` хранит только предпросмотр. **Summoners Table > Config > Refresh card prefab previews from JSON** обновляет подписи префабов из JSON. Баланс меняется со следующего матча; игрокам Steam нужен одинаковый набор cards/decks/rules. Эффект определяется поддерживаемым строковым идентификатором; для принципиально новой механики нужен код правил.

## Где что менять

| Элемент | Префаб / ресурс |
|---|---|
| Главное меню | `Assets/Prefabs/MainMenuCanvas.prefab` |
| Лобби, нижний выбор брони, Ready, портреты | `Assets/Prefabs/LobbyCanvas.prefab` |
| Общая сборка игрового интерфейса | `Assets/Prefabs/Editable/UI/GameInterface.prefab` |
| Верхняя панель, кнопка конца хода, подсказки | `Editable/UI/MatchHUD.prefab` |
| Режимы и общие флажки лобби | `Editable/UI/MatchOptions.prefab` |
| Расход действий / QTE возле конца хода | `Editable/UI/TurnBudget.prefab` |
| Веер руки | `Editable/UI/HandFan.prefab` |
| Объёмное окно иллюстраций всех 30 карт | `CardDepthVisual` на `Editable/Cards/CardBase.prefab` |
| Глубина и перелив карт | `Assets/Resources/Styles/CardWindowUI.mat`, `CardWindowWorld.mat` |
| Центр / левый показ / правое наведение, реакции, QTE | `Assets/Prefabs/CardTableCanvas.prefab` |
| Контейнер экземпляра карты | `Editable/UI/CardSlot.prefab` |
| Ник и HP | `Editable/UI/PlayerStatus.prefab` |
| Атака/HP существа | `Editable/UI/UnitStats.prefab` |
| Журнал и его строки | `Editable/UI/HistoryPanel.prefab`, `HistoryRow.prefab` |
| Стрелка при перетаскивании | `Editable/UI/TargetArrow.prefab` |
| Главное меню / ESC: экран, звук, подсветка | `Editable/UI/SettingsPanel.prefab` |
| Выбор шейдера в лобби и ESC | `Editable/UI/ShaderChoice.prefab` |
| Toon / Painterly и их параметры | `Assets/Resources/ShaderStyles.asset`, `Styles/Toon.mat`, `Styles/Painterly.mat` |
| Итоги и три действия | `Editable/UI/MatchResults.prefab` |
| Передача управления, правила, подтверждение выхода | `Editable/UI/LocalHandoff.prefab`, `RulesPanel.prefab`, `ExitConfirmation.prefab` |
| Поиск Steam-лобби и строка результата | `Editable/UI/SteamSearch.prefab`, `RoomRow.prefab` |
| Каталог карт | `Editable/UI/CardBrowser.prefab` |
| Панель лаборатории | `Editable/UI/LabControls.prefab` |
| Общее место игрока, стул, аватар, цель, слот | `Editable/World/PlayerSeat.prefab`, `Chair.prefab`, `PlayerAvatar.prefab`, `PlayerTarget.prefab`, `CreatureSlot.prefab` |
| Общая панель игрока лобби / портретная сцена | `Editable/UI/LobbyPlayerPanel.prefab`, `Editable/World/LobbyPortraitStage.prefab` |
| Перезагрузка Config и статус | `Editable/UI/ConfigControls.prefab` |
| Интерьер, общая рассадка SeatingLayout, камера | `Assets/Prefabs/TableWorld.prefab` |
| Пропорции стола и посадки | `TableProportions` на корне `TableWorld.prefab` |
| Геометрия кресла с согласованными высотами сиденья и спинки | `Assets/Presentation/Meshes/Armchair proportions 0.asset` |
| Рубашки рядом с героями | `Editable/World/CardBack.prefab` |
| Размер и полукруг рубашек без поворота за камерой | `interface.json` и `Hand anchor` внутри `PlayerSeat` |
| Постоянные стрелки намерений | `Editable/World/AttackArrow.prefab` |
| Всплывающие числа HP | `Editable/World/HealthNumber.prefab` |
| Эффекты отдельных заклинаний | `Editable/Spells/S01.prefab` … `S08.prefab` |
| Частицы, применённые из stuff | `Assets/Presentation/Effects` |
| Камера, анимации, общие VFX и звуки | `Assets/Resources/PresentationSettings.asset`, `HeroLibrary.asset` |

Пути, начинающиеся с `Editable`, находятся внутри `Assets/Prefabs/`. `GameInterface` содержит вложенные экземпляры экранов, руки и четырёх PlayerStatus. В `WidgetScreen > Bindings` имена связывают элементы с логикой: сохраняйте их и ссылки при перестройке иерархии. Динамические тексты (ник, HP, таймер, счёт) задаёт игра; их шрифт, размер и положение задаёт префаб.

Настройки режимов и пределы эффектов 0.6.0 описаны в [LOBBY-RULES.md](LOBBY-RULES.md). Положение `HandFan` наследуется вложенным экземпляром `GameInterface`; меняйте исходный веер. Флажок «3D карточки» применяет материалы `CardDepthVisual` только к иллюстрациям. Depth и Foil задаются в `presentation.json`, влияние курсора — в `prefabs.json`. Существующие варианты карт наследуют компонент автоматически.

PlayerStatus: `Name Offset`, `Minimum Name Gap` и `Name Screen Padding` задают отступы ника; высота увеличенной модели учитывается автоматически. `Health Between Hero And Slots` и `Health Height` — положение шкалы. Дочерние RectTransform задают её размер. В первом лице собственная модель скрыта; собственные HP остаются в панели руки. Пропорции стола и персонажей описаны в [TABLE-PROPORTIONS.md](TABLE-PROPORTIONS.md), материалы — в [SHADERS.md](SHADERS.md).

SpellEffect: рабочие travel/impact/persistent, масштаб, длительность и траектория задаются в `vfx.json`; звуки — в `audio.json`. В поле Spell Effect у каждого S-варианта можно назначить другой префаб. Для новых звуковых источников эффектов добавляйте EffectsVolume; общая громкость действует через AudioListener. Громкость = громкость варианта × категория × master. Категории Effects, Ambience, Voices и Music задаются в audio.json.

## Сцены

В `Assets/Scenes/Match.unity` уже стоят TableWorld, CardTableCanvas, GameInterface и MotionAnnouncements. Новые объекты окружения добавляйте в Match или в Locations/TavernInterior. Места создаются вокруг стола из одного HeroSittingPlace → PlayerSeat. Его Body содержит кресло, аватар, точки руки/имени/HP и PlayerStatus. Для общих правок между Match и PresentationLab меняйте исходный префаб. В сцене PresentationLab есть отдельный LabControls.

При запуске MainMenu недостающие сцены подгружаются аддитивно. Невидимые контроллеры и хитбоксы могут создаваться кодом; отображаемые игровые элементы берутся из сохранённых префабов. Production OnGUI/IMGUI удалён.

Обычные команды Build Windows и Create missing editable prefabs не пересоздают существующие карточки и экраны. Старые команды начальной сборки таверны и `PolishAndBuild` предназначены для миграции, не для ежедневного редактирования: они могут сбросить оформление своих целевых префабов.
