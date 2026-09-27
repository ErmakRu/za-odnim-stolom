# Обновление интерфейса — 27 сентября 2026

- Тело единой стрелки расширено втрое. Water02/Dark и размеры наконечника сохранены.
- Добавлен сохраняемый мультяшный отсчёт последних секунд собственного хода и QTE. Отсчёт учитывает блокировки и скрытые данные соперника; позиция QTE задаётся от нижнего края панели.
- HP соперника привязан к точке над макушкой после окончательной позы модели; рога/уши учтены настройками HeroLibrary. Строка таймера находится под названием фазы и не пересекает HP.
- Сборка AuthoredPlayerBuild использует сохранённые сцены без генераторов интерфейса и массовой смены импортов. Резервные копии сохранены в EditorSnapshots.

Проверки: Editor DreamPresentation PASS51, InterfaceRevision PASS44; проверки границ и видимости отсчёта; отдельный Windows Player завершился с кодом 0. Пять кадров Player проверены на пустое изображение и отсутствующие шейдеры; кадры лобби, отсчёта и QTE также просмотрены. Меню не создаёт TableWorld; при запуске боя создаётся один мир.

Билд Windows x64: Builds/Windows-20260927-134842/ZaOdnimStolom.exe, Unity 6000.3.6f1. Исходный коммит и хеш игровой сборки: SummonersTable/Captures/PlayerSmoke-20260927/build-source.txt. Бинарники не входят в историю Git согласно .gitignore.

Сборка успешна, 8 предупреждений (неиспользуемые поля/устаревшие API и предупреждения сторонних шейдеров). Сеть Steam между двумя компьютерами этой проверкой не покрыта. Лобби проверено по базовой композиции: таверна, четыре цветные восьмигранные панели, модели и веера карт; это не заявление о попиксельном совпадении с референсом.

## Standalone mode buttons and separate campaign scene

Fixed ConfigRuntime resolving only an external Config directory in Windows Player. Plain Unity builds now load StreamingAssets/Config when there is no external override; malformed explicit overrides still fail validation. SteamPlayerSetup writes a missing steam_appid.txt for Windows x64 builds using the existing test App ID 480, preserving any supplied ID.

Campaign reading and tutorial UI now live in Assets/Campaign/Scenes/Campaign.unity. Mode-card actions load that scene; leaving returns to MainMenu. CampaignComic prefab remains the editable visual source. MainMenu/Lobby/Match no longer contain that Canvas. Scene backups were saved locally before migration; user prefab edits were preserved.

Validation: Editor Play opened campaign through the actual new-game button at source 0.1 in the throne hall, advanced all 14 prologue lines to the dream and tutorial, returned to MainMenu and opened local lobby. Original campaign save was restored. Windows LZ4HC release 20260927-151601 passed standalone smoke without external Config: actual local/online onClick handlers, local match, countdown, Water02 arrow and QTE, five nonblank screenshots, no missing shaders or development/performance-test metadata. Online page transition was checked; a two-client Steam match was not tested.
