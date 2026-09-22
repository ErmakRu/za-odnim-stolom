using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace SummonersTable.Editor
{
    public static partial class PrefabAuthoring
    {
        static void CreateInterface()
        {
            var root=Center("Game interface",null,new Vector2(1600,1000));var canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=8;
            var scaler=root.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,1000);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;root.gameObject.AddComponent<GraphicRaycaster>();
            root.gameObject.AddComponent<AudioSource>().playOnAwake=false;root.gameObject.AddComponent<EffectsVolume>();
            var ui=root.gameObject.AddComponent<PrefabInterface>();
            var s=new Screen("Match HUD");I("Header",s.Parent,new Rect(0,0,1600,110),panel,true);I("Hand backdrop",s.Parent,new Rect(0,760,1600,240),ink,true);
            s.Text("title",new Rect(28,19,310,33),"ЗА ОДНИМ СТОЛОМ",22);s.Text("turn",new Rect(28,61,320,27),"",18);
            s.Text("phase",new Rect(385,17,790,34),"",26,TextAnchor.MiddleCenter);s.Text("timer",new Rect(380,58,810,28),"",18,TextAnchor.MiddleCenter);
            s.Button("rules",new Rect(1290,27,135,44),"Правила");s.Button("settings",new Rect(1440,27,140,44),"Настройки");
            s.Button("journal",new Rect(24,120,264,42),"▼ Журнал");s.Text("feedback",new Rect(400,110,800,65),"",19,TextAnchor.MiddleCenter);
            s.Text("cast",new Rect(420,180,760,64),"",21,TextAnchor.MiddleCenter);s.Text("hint",new Rect(410,700,780,48),"",19,TextAnchor.MiddleCenter);
            s.Text("camera",new Rect(24,777,285,45),"Колесо — вид камеры\nПКМ — поворот взгляда",16);
            s.Text("name",new Rect(24,924,270,33),"",23);s.Text("personal",new Rect(24,960,310,29),"",17);
            s.Button("cancel",new Rect(24,827,240,48),"Отменить выбор");s.Button("pass",new Rect(1318,826,250,50),"Пропустить реакцию");
            s.Button("end",new Rect(1320,902,250,60),"Закончить ход",gold);s.Text("endhint",new Rect(1300,970,285,25),"",16,TextAnchor.MiddleCenter);
            s.Text("center",new Rect(0,0,180,48),"СЛУЧАЙНАЯ\nЦЕЛЬ",16,TextAnchor.MiddleCenter);
            ui.hud=Nest(s.Save("MatchHUD"),root);
            ui.worldOverlay=Center("World labels",ui.hud.transform,new Vector2(1600,1000));
            ui.playerStatus=new PlayerStatusView[4];var status=CreatePlayerStatus();for(int i=0;i<4;i++){ui.playerStatus[i]=Nest(status,ui.worldOverlay);ui.playerStatus[i].name="Player "+i+" name and HP";}
            ui.unitBadgePrefab=CreateUnitBadge();
            var hand=Center("Hand fan",ui.hud.transform,new Vector2(1000,250));hand.anchoredPosition=new Vector2(0,-359);ui.hand=hand.gameObject.AddComponent<HandFan>();ui.hand.cardSlotPrefab=Load(Root+"UI/CardSlot.prefab").GetComponent<CardDisplaySlot>();
            var handAsset=Save(hand.gameObject,Root+"UI/HandFan.prefab").GetComponent<HandFan>();ui.hand=Nest(handAsset,ui.hud.transform);
            var aim=Center("Target arrow",ui.hud.transform,new Vector2(1600,1000));ui.arrow=aim.gameObject.AddComponent<TargetArrowGraphic>();ui.arrow.color=gold;ui.arrow.raycastTarget=false;
            var aimAsset=Save(aim.gameObject,Root+"UI/TargetArrow.prefab").GetComponent<TargetArrowGraphic>();ui.arrow=Nest(aimAsset,ui.hud.transform);
            ui.history=Nest(CreateHistory(),root);
            s=new Screen("ESC settings",true,40);s.Text("title",new Rect(420,220,760,70),"НАСТРОЙКИ",38,TextAnchor.MiddleCenter);
            s.Text("masterLabel",new Rect(470,350,660,42),"Общая громкость",24);s.Slider("master",new Rect(470,410,660,38));s.Text("effectsLabel",new Rect(470,485,660,42),"Звуковые эффекты",24);s.Slider("effects",new Rect(470,545,660,38));
            s.Text("note",new Rect(420,605,760,42),"Онлайн-матч продолжает идти, пока открыты настройки.",19,TextAnchor.MiddleCenter);
            s.Button("resume",new Rect(440,695,345,60),"Продолжить");s.Button("leave",new Rect(815,695,345,60),"Выйти в меню");ui.settings=Nest(s.Save("SettingsPanel"),root);
            s=new Screen("Match results",true,30);s.Text("title",new Rect(300,145,1000,70),"ВОТ ЭТО ПОСИДЕЛИ!",42,TextAnchor.MiddleCenter);s.Text("result",new Rect(300,240,1000,90),"",27,TextAnchor.MiddleCenter);
            s.Text("scores",new Rect(385,355,830,270),"",26);s.Text("votes",new Rect(280,625,1040,70),"",21,TextAnchor.MiddleCenter);
            s.Button("again",new Rect(230,765,355,65),"Сыграть ещё раз");s.Button("deck",new Rect(615,765,390,65),"Выбрать другую колоду",gold);s.Button("menu",new Rect(1035,765,335,65),"Выйти в меню");ui.results=Nest(s.Save("MatchResults"),root);
            s=new Screen("Local handoff",true,30);s.Text("title",new Rect(300,200,1000,80),"Передайте клавиатуру",42,TextAnchor.MiddleCenter);s.Text("name",new Rect(250,320,1100,90),"",54,TextAnchor.MiddleCenter);s.Text("hint",new Rect(320,457,960,135),"",25,TextAnchor.MiddleCenter);s.Button("continue",new Rect(530,640,540,70),"Я за столом — показать карты");s.Button("menu",new Rect(650,751,300,48),"В главное меню");ui.handoff=Nest(s.Save("LocalHandoff"),root);
            s=new Screen("Rules",true,40);s.Text("title",new Rect(240,100,1100,70),"Как устроена потасовка",38);s.Text("body",new Rect(240,220,1100,550),"Один раунд. Победа: +3 очка; устранение противника: +1.\n\nЗа ход: до трёх заклинаний ИЛИ одно заклинание и одно существо. Рука до 8 карт, стол — 5 слотов.\n\nСущество разыгрывается в свободный слот, заклинание — на выбранную цель. Затем QTE: клавиши A S D F G H J. Три ошибки или тайм-аут передают карту противнику.\n\nРеакции играются на чужой розыгрыш до завершения QTE. На атаки существ реакций нет.\n\nЗажмите ЛКМ на своём существе и вытяните стрелку на цель. Центр стола — случайный вражеский герой. Все атаки происходят по кнопке «Закончить ход».\n\nКамера: колесо и ПКМ. ESC — громкость и выход. После матча все участники могут подтвердить повторную игру или изменить выбор в лобби.",24);s.Button("close",new Rect(550,840,500,60),"Понятно");ui.rules=Nest(s.Save("RulesPanel"),root);
            s=new Screen("Confirm exit",true,45);s.Text("title",new Rect(400,320,800,65),"Покинуть матч?",36,TextAnchor.MiddleCenter);s.Text("note",new Rect(450,420,700,110),"",24,TextAnchor.MiddleCenter);s.Button("stay",new Rect(465,570,320,60),"Остаться");s.Button("leave",new Rect(815,570,320,60),"Выйти");ui.quit=Nest(s.Save("ExitConfirmation"),root);
            ui.search=Nest(CreateSearch(),root);ui.browser=Nest(CreateBrowser(),root);ui.roomRowPrefab=Load(Root+"UI/RoomRow.prefab").GetComponent<WidgetScreen>();ui.browserCardPrefab=Load(Root+"UI/CardSlot.prefab").GetComponent<CardDisplaySlot>();
            foreach(var screen in new[]{ui.hud,ui.settings,ui.results,ui.handoff,ui.rules,ui.quit,ui.search,ui.browser})screen.Show(false);ui.history.gameObject.SetActive(false);
            Save(root.gameObject,Root+"UI/GameInterface.prefab");
        }
        static PlayerStatusView CreatePlayerStatus()
        {
            var root=Center("PlayerStatus",null,new Vector2(1600,1000));var v=root.gameObject.AddComponent<PlayerStatusView>();
            v.nameAnchor=Center("Nickname above head",root,new Vector2(230,32));var bg=I("Backdrop",v.nameAnchor,new Rect(0,0,230,32),new Color(.02f,.05f,.07f,.72f));v.nickname=T("Nickname",v.nameAnchor,new Rect(4,0,222,32),"Имя игрока",20,TextAnchor.MiddleCenter);
            v.healthAnchor=Center("HP between hero and cards",root,new Vector2(160,25));I("Empty health",v.healthAnchor,new Rect(0,0,160,25),ink);
            v.healthTrail=I("Recent damage",v.healthAnchor,new Rect(2,2,156,21),gold);v.healthFill=I("Health",v.healthAnchor,new Rect(2,2,156,21),teal);
            foreach(var image in new[]{v.healthFill,v.healthTrail}){image.type=Image.Type.Filled;image.fillMethod=Image.FillMethod.Horizontal;image.fillOrigin=0;}
            v.healthNumber=T("Numeric HP",v.healthAnchor,new Rect(0,0,160,25),"30 / 30",17,TextAnchor.MiddleCenter);v.healthNumber.fontStyle=FontStyle.Bold;
            return Save(root.gameObject,Root+"UI/PlayerStatus.prefab").GetComponent<PlayerStatusView>();
        }
        static WidgetScreen CreateUnitBadge()
        {
            var s=new Screen("Unit stats");((RectTransform)s.Parent).sizeDelta=new Vector2(48,24);I("Background",s.Parent,new Rect(0,0,48,24),ink);s.Text("stats",new Rect(0,0,48,21),"",14,TextAnchor.MiddleCenter);s.Bind("group",I("Group",s.Parent,new Rect(0,21,48,3),teal));return s.Save("UnitStats");
        }
        static HistoryListView CreateHistory()
        {
            var row=R("History row",null,new Rect(0,0,908,84));var view=row.gameObject.AddComponent<HistoryRowView>();
            view.header=R("Turn header",row,new Rect(0,0,908,40)).gameObject;I("Header background",view.header.transform,new Rect(0,0,908,37),new Color(.11f,.20f,.22f));view.heading=T("Whose turn",view.header.transform,new Rect(12,6,875,28),"",19);
            view.body=R("Entry",row,new Rect(0,0,908,84)).gameObject;
            var source=R("Source hover",view.body.transform,new Rect(0,0,480,84));I("Background",source,new Rect(0,0,480,82),panel);view.sourceHover=source;view.sourceCard=Slot(source,"Source card",new Rect(10,7,49,70));view.actor=T("Owner",source,new Rect(70,5,390,24),"",17);view.sourceName=T("Name",source,new Rect(70,29,390,27),"",19);view.verb=T("Action",source,new Rect(70,56,390,24),"",16);
            view.targetGroups=new GameObject[4];view.targetOwners=new Text[4];view.targetNames=new Text[4];view.amounts=new Text[4];view.targetCards=new CardDisplaySlot[4];
            for(int i=0;i<4;i++){var target=R("Target "+i,view.body.transform,new Rect(490,i*84,418,84));view.targetGroups[i]=target.gameObject;I("Background",target,new Rect(0,0,418,82),panel);view.targetCards[i]=Slot(target,"Target card",new Rect(22,7,49,70));view.targetOwners[i]=T("Owner",target,new Rect(83,8,315,25),"",17);view.targetNames[i]=T("Name",target,new Rect(83,32,315,28),"",18);view.amounts[i]=T("Damage or heal",target,new Rect(83,60,315,23),"",17);view.amounts[i].color=new Color(1,.43f,.43f);}
            var rowAsset=Save(row.gameObject,Root+"UI/HistoryRow.prefab").GetComponent<HistoryRowView>();
            var s=new Screen("History dropdown",false,22);I("Panel",s.Parent,new Rect(309,116,956,615),ink,true);s.Text("title",new Rect(324,128,810,32),"История стола · наведите на карту",22);var close=s.Button("close",new Rect(1180,125,65,34),"×");var scroll=Scroll(s.Parent,"Event list",new Rect(321,173,932,546),new Vector2(908,546));
            var h=s.view.gameObject.AddComponent<HistoryListView>();h.window=s.Parent.Find("Panel") as RectTransform;h.scroll=scroll;h.rowPrefab=rowAsset;h.close=close;return s.Save("HistoryPanel").GetComponent<HistoryListView>();
        }
        static WidgetScreen CreateSearch()
        {
            var row=new Screen("Room row");((RectTransform)row.Parent).sizeDelta=new Vector2(790,68);I("Background",row.Parent,new Rect(0,0,790,64),panel);row.Text("name",new Rect(14,14,535,38),"",22);row.Button("join",new Rect(585,10,190,44),"Присоединиться");row.Save("RoomRow");
            var s=new Screen("Steam search",true);s.Button("back",new Rect(42,32,150,44),"← В меню");s.Text("title",new Rect(240,30,1250,50),"Игра через Steam",34);s.Text("status",new Rect(240,90,1250,55),"",20);
            s.Text("unavailable",new Rect(110,190,1280,110),"Запустите Steam и войдите в аккаунт. Всем участникам нужен одинаковый билд; тестовый App ID 480.",26);s.Button("retry",new Rect(110,320,390,60),"Повторить подключение");
            s.Button("quick",new Rect(100,230,540,65),"Быстрый поиск соперника");s.Button("refresh",new Rect(100,335,540,55),"Обновить список",gold);s.Text("joinlabel",new Rect(100,470,540,42),"Войти по коду лобби",23);s.Input("code",new Rect(100,535,350,50),"",24);s.Button("join",new Rect(467,535,172,50),"Войти");
            s.Text("createLabel",new Rect(730,180,760,45),"Открыть свой стол",30);s.Input("room",new Rect(730,250,470,50),"Весёлый стол",36);for(int n=2;n<=4;n++)s.Button("capacity"+n,new Rect(1220+(n-2)*90,250,78,50),n.ToString());s.Button("create",new Rect(730,335,760,60),"Создать публичное лобби",gold);
            s.Text("roomsLabel",new Rect(700,465,830,40),"Открытые столы",27);s.Bind("rooms",Scroll(s.Parent,"Rooms",new Rect(700,530,830,325),new Vector2(800,325)));s.Text("empty",new Rect(715,550,770,130),"",22);s.Text("error",new Rect(430,900,1080,70),"",21);s.Button("cancel",new Rect(70,900,310,48),"Отменить поиск");return s.Save("SteamSearch");
        }
        static WidgetScreen CreateBrowser()
        {
            var s=new Screen("Card browser",true);s.Button("back",new Rect(42,32,150,44),"← Назад");s.Text("title",new Rect(240,30,1250,50),"Колоды и карты",34);s.Text("guide",new Rect(70,88,1460,50),"",19);
            for(int n=-1;n<3;n++)s.Button("deck"+n,new Rect(60+(n+1)*385,158,365,52),n<0?"Все 30 карт":"Колода "+(n+1));s.Bind("cards",Scroll(s.Parent,"Cards",new Rect(60,235,1480,740),new Vector2(1450,3200)));return s.Save("CardBrowser");
        }
    }
}
