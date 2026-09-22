# Редактируемые префабы · 0.5.0

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

Числа, описание и логика карты находятся в `CardView > Definition`. Игра читает их из префаба. После изменения Definition выполните **Summoners Table > Export prefab card definitions and refresh labels**: подписи обновятся, данные экспортируются в JSON для PDF. Перед новым общим тестом пересоберите одинаковый билд для всех; при изменении баланса обновите версию каталога. Эффект определяется поддерживаемым строковым идентификатором; для принципиально новой механики нужен код правил.

## Где что менять

| Элемент | Префаб / ресурс |
|---|---|
| Главное меню | `Assets/Prefabs/MainMenuCanvas.prefab` |
| Лобби, нижний выбор брони, Ready, портреты | `Assets/Prefabs/LobbyCanvas.prefab` |
| Общая сборка игрового интерфейса | `Assets/Prefabs/Editable/UI/GameInterface.prefab` |
| Верхняя панель, кнопка конца хода, подсказки | `Editable/UI/MatchHUD.prefab` |
| Веер руки | `Editable/UI/HandFan.prefab` |
| Центр / левый показ / правое наведение, реакции, QTE | `Assets/Prefabs/CardTableCanvas.prefab` |
| Контейнер экземпляра карты | `Editable/UI/CardSlot.prefab` |
| Ник и HP | `Editable/UI/PlayerStatus.prefab` |
| Атака/HP существа | `Editable/UI/UnitStats.prefab` |
| Журнал и его строки | `Editable/UI/HistoryPanel.prefab`, `HistoryRow.prefab` |
| Стрелка при перетаскивании | `Editable/UI/TargetArrow.prefab` |
| ESC и два ползунка | `Editable/UI/SettingsPanel.prefab` |
| Итоги и три действия | `Editable/UI/MatchResults.prefab` |
| Передача управления, правила, подтверждение выхода | `Editable/UI/LocalHandoff.prefab`, `RulesPanel.prefab`, `ExitConfirmation.prefab` |
| Поиск Steam-лобби и строка результата | `Editable/UI/SteamSearch.prefab`, `RoomRow.prefab` |
| Каталог карт | `Editable/UI/CardBrowser.prefab` |
| Панель лаборатории | `Editable/UI/LabControls.prefab` |
| Стол, места 2/3/4 игроков, слоты, камера | `Assets/Prefabs/TableWorld.prefab` |
| Рубашки рядом с героями | `Editable/World/CardBack.prefab` |
| Постоянные стрелки намерений | `Editable/World/AttackArrow.prefab` |
| Всплывающие числа HP | `Editable/World/HealthNumber.prefab` |
| Эффекты отдельных заклинаний | `Editable/Spells/S01.prefab` … `S08.prefab` |
| Частицы, применённые из stuff | `Assets/Presentation/Effects` |
| Камера, анимации, общие VFX и звуки | `Assets/Resources/PresentationSettings.asset`, `HeroLibrary.asset` |

Пути, начинающиеся с `Editable`, находятся внутри `Assets/Prefabs/`. `GameInterface` содержит вложенные экземпляры экранов, руки и четырёх PlayerStatus. В `WidgetScreen > Bindings` имена связывают элементы с логикой: сохраняйте их и ссылки при перестройке иерархии. Динамические тексты (ник, HP, таймер, счёт) задаёт игра; их шрифт, размер и положение задаёт префаб.

PlayerStatus: `Name Offset` и `Minimum Name Gap` задают положение ника; `Health Between Hero And Slots` и `Health Height` — положение шкалы. Дочерние RectTransform задают её размер. В первом лице собственная модель скрыта; собственные HP остаются в панели руки.

SpellEffect: ссылки на travel/impact/persistent prefab, звуки, масштаб, длительность и траекторию находятся в Inspector. В поле Spell Effect у каждого S-варианта можно назначить другой префаб. Для новых звуковых источников эффектов добавляйте EffectsVolume; общая громкость действует через AudioListener. Музыка/фон подчиняются общей громкости.

## Сцены

В `Assets/Scenes/Match.unity` уже стоят TableWorld, CardTableCanvas, GameInterface и MotionAnnouncements. Новые объекты окружения добавляйте в Match или в TableWorld. Для общих правок между Match и PresentationLab меняйте исходный префаб. В сцене PresentationLab есть отдельный LabControls.

При запуске MainMenu недостающие сцены подгружаются аддитивно. Невидимые контроллеры и хитбоксы могут создаваться кодом; отображаемые игровые элементы берутся из сохранённых префабов. Production OnGUI/IMGUI удалён.

Обычные команды Build Windows и Create missing editable prefabs не пересоздают существующие карточки и экраны. Старые команды начальной сборки таверны и `PolishAndBuild` предназначены для миграции, не для ежедневного редактирования: они могут сбросить оформление своих целевых префабов.
