using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace SummonersTable
{
    public sealed partial class GameApp : MonoBehaviour
    {
        const float W=1600,H=1000;
        readonly Color ink=new Color(.06f,.11f,.14f),panel=new Color(.055f,.105f,.13f,.96f),muted=new Color(.64f,.75f,.76f);
        readonly Color teal=new Color(.25f,.79f,.69f),gold=new Color(.97f,.74f,.36f),red=new Color(1,.43f,.43f);
        readonly Dictionary<string,Texture2D> art=new Dictionary<string,Texture2D>();
        readonly Dictionary<int,GUIStyle> labels=new Dictionary<int,GUIStyle>();
        Catalog catalog; SteamSession steam; GameEngine local; MatchState state;
        GUIStyle buttonStyle,fieldStyle;Font font;
        string page="menu",modal="",returnPage="menu",error="",selectedCard="",selectedUnit="",joinCode="",roomName="Весёлый стол";
        string[] localNames={"Игрок 1","Игрок 2","Игрок 3","Игрок 4"};
        string[] localHeroes={"badger","deer","owl","lion"};int[] localOutfits={0,1,2,3},localPalettes={0,1,2,3};
        double nextLookSend;Vector2 lastSentLook=new Vector2(999,999);int lastSentMode=-1;
        int[] localDecks={0,1,2,0};int localCount=2,capacity=4,seat,selectedSlot=-1,catalogDeck=-1;
        bool handoff,initialized,quitConfirm;double localTime;int[] seq=new int[4];
        float scale;Vector2 offset,scroll;bool online;
        string seenPhase="";int seenTurn=-1;
        AudioSource audioSource;AudioClip tickTone,failTone,successTone;string lastQte="";int lastProgress,lastMistakes;
        CardTableCanvas cardCanvas;
        MotionAnnouncements announcements;
        public bool IsReady {get{return steam!=null&&board!=null&&cardCanvas!=null;}}
        public Catalog Catalog {get {return catalog;}}

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(FindFirstObjectByType<GameApp>()==null)new GameObject("SummonersTable").AddComponent<GameApp>();}
        IEnumerator Start()
        {
            yield return LoadPresentationScenes();
            Application.runInBackground=true;Application.targetFrameRate=60;
            catalog=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("Data/catalog").text);catalog.Validate();
            font=Font.CreateDynamicFontFromOSFont("Arial",24);BindFrontEnd();
            foreach(var c in catalog.cards)art[c.id]=Resources.Load<Texture2D>("Art/"+c.id);
            foreach(var key in new[]{"menu","board","card_back"})art[key]=Resources.Load<Texture2D>("Art/"+key);
            board=FindFirstObjectByType<TableBoard>(FindObjectsInactive.Include);
            if(board==null)board=new GameObject("3D Table").AddComponent<TableBoard>();
            board.Initialize(catalog);
            cardCanvas=FindFirstObjectByType<CardTableCanvas>(FindObjectsInactive.Include);
            if(cardCanvas==null){cardCanvas=new GameObject("Card display Canvas").AddComponent<CardTableCanvas>();cardCanvas.Build();}
            cardCanvas.Initialize(font,key=>{if(state?.qte!=null)Send(new GameCommand{kind="key",phaseId=state.qte.id,key=key});});
            announcements=FindFirstObjectByType<MotionAnnouncements>(FindObjectsInactive.Include);
            if(FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>()==null)
            {var events=new GameObject("UI Event System");events.AddComponent<UnityEngine.EventSystems.EventSystem>();events.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();}
            var eventSystems=FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.InstanceID);
            for(int i=1;i<eventSystems.Length;i++)eventSystems[i].gameObject.SetActive(false);
            captureMode=Environment.GetCommandLineArgs().Contains("--capture-preview")||Environment.GetCommandLineArgs().Contains("--capture-lab");
            steam=new SteamSession(catalog);if(!captureMode&&FindFirstObjectByType<PresentationLab>()==null)steam.Initialize();
            audioSource=gameObject.AddComponent<AudioSource>();audioSource.volume=.15f;
            tickTone=Tone(680,.045f);failTone=Tone(160,.12f);successTone=Tone(980,.14f);
            var args=Environment.GetCommandLineArgs();
            if(args.Contains("--local-test"))StartLocal(4);
            if(args.Contains("--capture-lab"))StartCoroutine(CaptureLabPreview());
            else if(captureMode)StartCoroutine(CapturePreview());
        }
        AudioClip Tone(float frequency,float duration)
        {
            int length=(int)(44100*duration);float[] samples=new float[length];
            for(int i=0;i<length;i++)samples[i]=Mathf.Sin(2*Mathf.PI*frequency*i/44100)*Mathf.Sin(Mathf.PI*i/length)*.3f;
            var clip=AudioClip.Create("QTE",length,1,44100,false);clip.SetData(samples,0);return clip;
        }
        void OnDestroy(){steam?.Dispose();}
        void StartLocal(int count)
        {
            steam.Leave();var members=new List<LobbyMember>();
            for(int i=0;i<count;i++)members.Add(new LobbyMember{id="local-"+i,name=string.IsNullOrWhiteSpace(localNames[i])?"Игрок "+(i+1):localNames[i],deckId=catalog.decks[localDecks[i]].id,heroId=localHeroes[i],outfit=localOutfits[i],palette=localPalettes[i],ready=true});
            localTime=0;local=new GameEngine(catalog,members,Environment.TickCount);online=false;seq=new int[4];seat=0;
            page="game";modal="";handoff=true;ClearSelection();seenTurn=-1;state=local.View(seat,localTime);
        }
        void Update()
        {
            if(!IsReady)return;
            if(!captureMode)UpdateSession();SyncFrontEnd();
            if(!captureMode&&announcements!=null)announcements.Present(handoff?"handoff":page,state,page=="local"?localCount:steam.Members.Count);
            if(page=="game"&&state!=null&&!handoff)
            {
                board.inputEnabled=!captureMode&&modal==""&&!quitConfirm;
                board.selectedUnit=selectedUnit;board.choosingTarget=selectedUnit!=""||(selectedCard!=""&&catalog.Card(state.players[seat].hand.Find(h=>h.uid==selectedCard)?.cardId)?.kind=="spell");
                var selectedDefinition=catalog.Card(state.players[seat].hand.Find(h=>h.uid==selectedCard)?.cardId);
                board.placingCreature=selectedDefinition?.kind=="creature";board.targetMode=selectedUnit!=""?"enemy":selectedDefinition?.target??"none";
                board.Sync(state,seat,Clock,selectedSlot);
                board.AimTrail(Input.mousePosition,!captureMode&&board.inputEnabled&&board.choosingTarget&&(selectedUnit==""||unitDragMoved));
                if(!captureMode&&Time.unscaledTimeAsDouble>=nextLookSend&&(Vector2.Distance(lastSentLook,board.CameraRig.Look)>1||lastSentMode!=board.CameraRig.Mode))
                {
                    nextLookSend=Time.unscaledTimeAsDouble+.2;lastSentLook=board.CameraRig.Look;lastSentMode=board.CameraRig.Mode;
                    if(online)steam.SubmitLook(lastSentLook.x,lastSentLook.y,lastSentMode);
                    else if(local!=null)local.Submit(seat,new GameCommand{seq=++seq[seat],kind="look",lookYaw=lastSentLook.x,lookPitch=lastSentLook.y,cameraMode=lastSentMode},localTime);
                }
                if(!captureMode)ReadQteKeys();
            }
            else board.gameObject.SetActive(false);
            var inspected=InspectionAt(captureMode?previewPointer??new Vector2(-100,-100):(Vector2)Input.mousePosition);
            cardCanvas.Present(state,seat,state==null?0:Clock,inspected,catalog,board,page=="game"&&!handoff&&modal==""&&!quitConfirm&&state.phase!="roundEnd"&&state.phase!="matchEnd");
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                if(modal!="")modal="";
                else if(selectedCard!=""||selectedUnit!="")ClearSelection();
                else if(page=="game")quitConfirm=!quitConfirm;
                else if(steam.InRoom)steam.Leave();
                else{steam.CancelSearch();page="menu";}
            }
        }
        void Send(GameCommand command)
        {
            if(online){steam.Submit(command);error=steam.Error;state=steam.View;}
            else if(local!=null){command.seq=++seq[seat];var result=local.Submit(seat,command,localTime);error=result.ok?"":result.message;state=local.View(seat,localTime);}
        }
        double Clock {get {return online?state.serverTime+Time.realtimeSinceStartupAsDouble-steam.ReceivedAt:localTime;}}
        void ClearSelection(){selectedCard="";selectedUnit="";selectedSlot=-1;mouseHeld=false;draggingCard=false;unitPointerHeld=false;unitDragMoved=false;error="";}
        void ExitMatch(){steam.Leave();local=null;state=null;online=false;page="menu";quitConfirm=false;ClearSelection();}
        void SetupStyles()
        {
            if(initialized)return;initialized=true;
            buttonStyle=new GUIStyle(GUI.skin.button){font=font,fontSize=19,wordWrap=true,alignment=TextAnchor.MiddleCenter,richText=false};
            buttonStyle.normal.background=Texture2D.whiteTexture;buttonStyle.hover.background=Texture2D.whiteTexture;
            buttonStyle.active.background=Texture2D.whiteTexture;buttonStyle.normal.textColor=ink;buttonStyle.hover.textColor=ink;buttonStyle.active.textColor=ink;
            fieldStyle=new GUIStyle(GUI.skin.textField){font=font,fontSize=22,padding=new RectOffset(12,10,9,8)};
        }
        GUIStyle Style(int size,bool bold=false,TextAnchor align=TextAnchor.UpperLeft)
        {
            int key=size*100+(bold?20:0)+(int)align;
            if(!labels.TryGetValue(key,out var style))
            {
                style=new GUIStyle{font=font,fontSize=size,fontStyle=bold?FontStyle.Bold:FontStyle.Normal,wordWrap=true,richText=false,alignment=align};labels[key]=style;
            }
            return style;
        }
        void Text(Rect r,string text,int size=20,Color? color=null,bool bold=false,TextAnchor align=TextAnchor.UpperLeft)
        {var style=Style(size,bold,align);style.normal.textColor=color??Color.white;GUI.Label(r,text??"",style);}
        void Box(Rect r,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        void Frame(Rect r,Color color,float width=2){Box(new Rect(r.x,r.y,r.width,width),color);Box(new Rect(r.x,r.yMax-width,r.width,width),color);Box(new Rect(r.x,r.y,width,r.height),color);Box(new Rect(r.xMax-width,r.y,width,r.height),color);}
        bool Button(Rect r,string text,Color? color=null,bool enabled=true)
        {
            bool prev=GUI.enabled;GUI.enabled=prev&&enabled;var old=GUI.backgroundColor;GUI.backgroundColor=color??teal;
            bool clicked=GUI.Button(r,text,buttonStyle);GUI.backgroundColor=old;GUI.enabled=prev;return clicked;
        }
        void Image(string id,Rect r,ScaleMode mode=ScaleMode.ScaleAndCrop)
        {if(art.TryGetValue(id??"",out var texture)&&texture!=null)GUI.DrawTexture(r,texture,mode);}
        Color TypeColor(CardDef c){ColorUtility.TryParseHtmlString(catalog.typeColors.Find(t=>t.id==c.kind).hex,out var color);return color;}
        Color RoleColor(CardDef c){var role=catalog.roleColors.Find(r=>r.name==c.role);ColorUtility.TryParseHtmlString(role?.hex??"#FFFFFF",out var color);return color;}
        string TypeName(CardDef c){return catalog.typeColors.Find(t=>t.id==c.kind).name;}
        void OnGUI()
        {
            if(!IsReady)return;
            if(previewLobby)return;
            if(modal==""&&!quitConfirm&&((page=="menu"&&menuCanvas!=null)||((page=="local"||page=="steam"&&steam.InRoom)&&lobbyCanvas!=null)))return;
            SetupStyles();GUI.color=Color.white;
            if(page!="game"||handoff)Box(new Rect(0,0,Screen.width,Screen.height),Color.black);
            scale=Mathf.Min(Screen.width/W,Screen.height/H);offset=new Vector2((Screen.width-W*scale)/2,(Screen.height-H*scale)/2);
            if(page=="game")
            {
                // Reserve the lower screen for the hand, so every own creature remains targetable.
                board.ViewCamera.rect=new Rect(offset.x/Screen.width,(Screen.height-offset.y-760*scale)/Screen.height,W*scale/Screen.width,650*scale/Screen.height);
                Box(new Rect(0,0,offset.x,Screen.height),Color.black);Box(new Rect(Screen.width-offset.x,0,offset.x,Screen.height),Color.black);
                Box(new Rect(0,0,Screen.width,offset.y),Color.black);Box(new Rect(0,Screen.height-offset.y,Screen.width,offset.y),Color.black);
            }
            GUI.matrix=Matrix4x4.TRS(offset,Quaternion.identity,new Vector3(scale,scale,1));
            bool blocked=modal!=""||quitConfirm;GUI.enabled=!blocked;
            switch(page)
            {
                case "menu":Menu();break;case "local":LocalSetup();break;case "steam":SteamPage();break;
                case "cards":CardBrowser();break;case "game":Game();break;
            }
            GUI.enabled=true;
            if(modal=="rules")Rules();
            if(quitConfirm)
            {
                Dim();Box(new Rect(440,330,720,300),panel);Text(new Rect(475,360,650,60),"Покинуть матч?",32,null,true);
                Text(new Rect(475,435,650,80),online&&steam.IsHost?"Вы хост. Если вы уйдёте, матч закончится для всех.":"Вернёмся в главное меню.",23,muted);
                if(Button(new Rect(475,550,300,50),"Остаться"))quitConfirm=false;
                if(Button(new Rect(805,550,300,50),"Выйти",red))ExitMatch();
            }
            GUI.matrix=Matrix4x4.identity;
        }
        void Dim(){Box(new Rect(0,0,W,H),new Color(.015f,.025f,.035f,.91f));}
        void Back(string destination="menu") {if(Button(new Rect(42,32,150,44),"← Назад",new Color(.7f,.78f,.77f))){page=destination;scroll=Vector2.zero;}}
        void Menu()
        {
            if(menuCanvas!=null)return;
            Image("menu",new Rect(0,0,W,H));Box(new Rect(0,0,700,H),new Color(.02f,.06f,.08f,.85f));
            Text(new Rect(75,85,550,38),"АРЕНА ПРИЗЫВА",22,teal,true);
            Text(new Rect(70,160,590,200),"За одним\nстолом",76,null,true);
            Text(new Rect(77,384,515,92),"Призывай нелепых героев.\nПорти планы друзьям. Жми в ритм.",25,muted);
            if(Button(new Rect(78,515,480,66),"Играть через Steam")){page="steam";steam.Search();}
            if(Button(new Rect(78,596,480,60),"За одним ПК • 2–4 игрока",gold))page="local";
            if(Button(new Rect(78,676,232,54),"Колоды и карты",new Color(.77f,.71f,.95f))){returnPage="menu";page="cards";}
            if(Button(new Rect(326,676,232,54),"Как играть",new Color(.7f,.78f,.77f)))modal="rules";
            Text(new Rect(78,780,520,65),steam.Status,17,muted);
            Text(new Rect(78,870,510,50),"ТЕСТ "+Application.version+"  /  3 РАУНДА  /  30 КАРТ",16,teal,true);
            if(Button(new Rect(78,927,150,40),"Выход",new Color(.7f,.78f,.77f)))Application.Quit();
            Text(new Rect(1130,900,390,70),"Существа + заклинания + реакции\nНикаких серьёзных лиц",21,Color.white,false,TextAnchor.MiddleRight);
        }
        void Background(string title,string subtitle)
        {
            Image("board",new Rect(0,0,W,H));Box(new Rect(0,0,W,H),new Color(.025f,.065f,.08f,.80f));
            Text(new Rect(240,30,1290,48),title,34,null,true);Text(new Rect(240,85,1290,48),subtitle,20,muted);
        }
        void LocalSetup()
        {
            Background("Собираемся за одним ПК","Передавайте клавиатуру по подсказке. Чужие руки скрыты; бота в этой версии нет.");Back();
            Text(new Rect(90,164,500,35),"Сколько нас?",26,null,true);
            for(int n=2;n<=4;n++)if(Button(new Rect(90+(n-2)*150,218,125,55),n+" игрока",localCount==n?teal:muted))localCount=n;
            for(int i=0;i<localCount;i++)
            {
                float y=320+i*123;Box(new Rect(90,y,1420,105),panel);
                localNames[i]=GUI.TextField(new Rect(112,y+29,305,47),localNames[i],24,fieldStyle);
                for(int d=0;d<3;d++)if(Button(new Rect(450+d*335,y+22,315,62),catalog.decks[d].name,localDecks[i]==d?teal:muted))localDecks[i]=d;
            }
            if(Button(new Rect(90,868,435,62),"Начать дружескую потасовку"))StartLocal(localCount);
            if(Button(new Rect(555,868,330,62),"Изучить колоды",gold)){returnPage="local";page="cards";}
        }
        void SteamPage()
        {
            Background(steam.InRoom?"Лобби: "+steam.RoomName:"Игра через Steam",steam.Status);
            if(Button(new Rect(42,32,150,44),"← В меню",muted)){steam.Leave();page="menu";}
            if(!steam.Available)
            {
                Text(new Rect(110,240,1260,140),"Запустите Steam и войдите в свой аккаунт. Игра использует общий тестовый App ID 480. У всех участников должна быть запущена одинаковая версия билда.",28);
                if(Button(new Rect(110,440,390,60),"Повторить подключение"))steam.Initialize();return;
            }
            if(steam.InRoom){Lobby();return;}
            Box(new Rect(70,175,600,675),panel);Text(new Rect(100,200,540,50),"Найти компанию",30,null,true);
            if(Button(new Rect(100,275,540,65),"Быстрый поиск соперника",teal,!steam.Busy))steam.Search(true);
            Text(new Rect(100,355,540,76),"Присоединится к свободному столу. Если столов нет — создаст новый на 4 места.",20,muted);
            if(Button(new Rect(100,462,540,50),"Обновить список",gold,!steam.Busy))steam.Search();
            Text(new Rect(100,550,540,40),"Войти по коду лобби",23,null,true);
            joinCode=GUI.TextField(new Rect(100,602,350,50),joinCode,24,fieldStyle);
            if(Button(new Rect(467,602,172,50),"Войти",teal,!steam.Busy))
            {if(ulong.TryParse(joinCode.Trim(),out var id))steam.Join(id);else steam.Error="Код лобби должен состоять из цифр.";}
            Text(new Rect(100,700,540,92),"Для теста по сети нужны разные Steam-аккаунты. Хост должен оставаться в игре до конца матча.",19,muted);
            Box(new Rect(700,175,830,300),panel);Text(new Rect(730,200,760,40),"Открыть свой стол",30,null,true);
            roomName=GUI.TextField(new Rect(730,260,470,50),roomName,36,fieldStyle);
            for(int n=2;n<=4;n++)if(Button(new Rect(1230+(n-2)*88,260,72,50),n.ToString(),capacity==n?teal:muted))capacity=n;
            if(Button(new Rect(730,343,760,60),"Создать публичное лобби",gold,!steam.Busy))steam.Create(roomName,capacity,true);
            Text(new Rect(700,507,830,38),"Открытые столы",27,null,true);
            scroll=GUI.BeginScrollView(new Rect(700,562,830,290),scroll,new Rect(0,0,796,Math.Max(280,steam.Rooms.Count*74)));
            int index=0;foreach(var room in steam.Rooms)
            {
                float y=index++*74;Box(new Rect(0,y,790,64),panel);
                Text(new Rect(14,y+14,530,38),room.name+"   "+room.count+" / "+room.capacity,22);
                if(Button(new Rect(590,y+10,183,44),"Присоединиться",teal,!steam.Busy))steam.Join(room.id);
            }
            if(steam.Rooms.Count==0)Text(new Rect(15,20,750,140),steam.Busy?"Ищем подходящие лобби…":"Открытых столов пока нет. Создайте свой и передайте друзьям код.",22,muted);
            GUI.EndScrollView();
            if(steam.Busy&&Button(new Rect(70,885,310,48),"Отменить поиск",muted))steam.CancelSearch();
            Text(new Rect(410,885,1110,74),steam.Error,21,red);
        }
        void Lobby()
        {
            if(lobbyCanvas!=null)return;
            Text(new Rect(90,167,1110,50),"Код: "+steam.RoomId,30,teal,true);
            if(Button(new Rect(1220,164,285,50),"Скопировать код",gold))GUIUtility.systemCopyBuffer=steam.RoomId.ToString();
            Text(new Rect(90,231,1350,54),"Выберите колоду и нажмите «Готов». Хост начнёт, когда за столом будет хотя бы два готовых игрока.",22,muted);
            for(int i=0;i<4;i++)
            {
                float y=314+i*96;Box(new Rect(90,y,1415,80),panel);
                if(i<steam.Members.Count)
                {
                    var member=steam.Members[i];bool own=member.id==steam.UserId.ToString();
                    Text(new Rect(115,y+20,500,43),(i==0?"★ ":"")+member.name+(own?" (вы)":""),25,null,true);
                    Text(new Rect(670,y+24,460,35),catalog.Deck(member.deckId).name,23,muted);
                    Text(new Rect(1170,y+24,290,35),member.ready?"ГОТОВ":"выбирает колоду",21,member.ready?teal:gold);
                }
                else Text(new Rect(115,y+22,1250,42),i<steam.Capacity?"Свободное место — ждём компанию":"Место закрыто",23,muted);
            }
            var me=steam.Members.Find(m=>m.id==steam.UserId.ToString());
            for(int d=0;d<3;d++)if(Button(new Rect(90+d*474,730,453,55),catalog.decks[d].name,me?.deckId==catalog.decks[d].id?teal:muted))steam.SetMember(catalog.decks[d].id,false);
            if(Button(new Rect(90,821,340,60),me?.ready==true?"Снять готовность":"Я готов!",me?.ready==true?muted:teal))steam.SetMember(me?.deckId??"noise",me?.ready!=true);
            if(Button(new Rect(460,821,340,60),"Колоды и карты",gold)){returnPage="steam";page="cards";}
            if(steam.IsHost&&Button(new Rect(830,821,390,60),"Начать матч",teal,steam.CanStart))steam.StartMatch();
            if(Button(new Rect(1250,821,255,60),"Выйти из лобби",muted))steam.Leave();
            Text(new Rect(90,920,1410,55),steam.Error,21,red);
        }
        void CardBrowser()
        {
            Background("Колоды и карты","Бирюзовый — существо. Золотой — заклинание. Сиреневый — реакция. Полоса под рисунком — роль существа.");Back(returnPage);
            if(Button(new Rect(60,158,225,52),"Все 30 карт",catalogDeck<0?teal:muted)){catalogDeck=-1;scroll=Vector2.zero;}
            for(int d=0;d<3;d++)if(Button(new Rect(305+d*417,158,397,52),catalog.decks[d].name,catalogDeck==d?teal:muted)){catalogDeck=d;scroll=Vector2.zero;}
            var cards=catalogDeck<0?catalog.cards:catalog.decks[catalogDeck].entries.Select(e=>catalog.Card(e.cardId)).ToList();
            float start=235;
            if(catalogDeck>=0)
            {
                var deck=catalog.decks[catalogDeck];Text(new Rect(65,235,1460,68),deck.subtitle+" • 30 карт: 15 существ, 12 заклинаний, 3 реакции.",23,teal,true);
                Text(new Rect(65,290,1460,100),deck.guide,20,muted);start=400;
            }
            float height=Mathf.Ceil(cards.Count/5f)*430;
            scroll=GUI.BeginScrollView(new Rect(55,start,1495,H-start-25),scroll,new Rect(0,0,1470,height));
            for(int i=0;i<cards.Count;i++)
            {
                var r=new Rect((i%5)*294,Mathf.Floor(i/5f)*430,280,412);CardDetail(cards[i],r,true);
                if(catalogDeck>=0)Text(new Rect(r.x+225,r.y+8,42,22),"×"+catalog.decks[catalogDeck].entries.Find(e=>e.cardId==cards[i].id).count,16,ink,true);
            }
            GUI.EndScrollView();
        }
        void CardDetail(CardDef c,Rect r,bool compact=false)
        {
            Box(r,new Color(.09f,.15f,.18f,.98f));Frame(r,TypeColor(c),3);
            Box(new Rect(r.x+3,r.y+3,r.width-6,29),TypeColor(c));Text(new Rect(r.x+12,r.y+8,r.width-24,25),TypeName(c).ToUpper()+"  ·  "+c.id,14,ink,true);
            float artHeight=compact?125:170;Image(c.id,new Rect(r.x+8,r.y+37,r.width-16,artHeight));
            float y=r.y+41+artHeight;
            if(c.kind=="creature")
            {Box(new Rect(r.x+8,y,r.width-16,25),RoleColor(c));Text(new Rect(r.x+15,y+4,r.width-30,23),c.faction+" / "+c.role,13,ink,true);y+=34;}
            Text(new Rect(r.x+14,y,r.width-28,53),c.name,compact?20:23,null,true);y+=58;
            string stats=c.kind=="creature"?"АТК "+c.attack+"   HP "+c.health+"    QTE "+c.qte:c.kind=="reaction"?"Без QTE • окно реакции":"QTE "+c.qte+" • разовый эффект";
            Text(new Rect(r.x+14,y,r.width-28,30),stats,17,TypeColor(c),true);y+=36;
            Text(new Rect(r.x+14,y,r.width-28,r.yMax-y-10),c.rules,compact?16:19,Color.white);
        }
        void MiniCard(CardDef c,Rect r,bool selected)
        {
            Box(r,panel);Box(new Rect(r.x,r.y,r.width,25),TypeColor(c));
            Text(new Rect(r.x+4,r.y+4,r.width-8,21),TypeName(c),13,ink,true,TextAnchor.MiddleCenter);
            float artHeight=r.height*.43f;
            Image(c.id,new Rect(r.x+4,r.y+29,r.width-8,artHeight));
            if(c.kind=="creature")Box(new Rect(r.x+4,r.y+29+artHeight,r.width-8,5),RoleColor(c));
            Text(new Rect(r.x+7,r.y+39+artHeight,r.width-14,r.height-artHeight-68),c.name,16,null,true);
            Text(new Rect(r.x+7,r.yMax-25,r.width-14,23),c.kind=="creature"?c.attack+" / "+c.health+"  · Q"+c.qte:c.kind=="reaction"?"БЕЗ QTE":"QTE "+c.qte,15,TypeColor(c),true);
            Frame(r,selected?gold:TypeColor(c),selected?4:1);
        }
        void ScoreOverlay()
        {
            Dim();Box(new Rect(300,145,1000,710),panel);
            Text(new Rect(350,180,900,68),state.phase=="matchEnd"?"Вот это посидели!":"Раунд завершён",42,teal,true,TextAnchor.MiddleCenter);
            Text(new Rect(350,280,900,90),state.result,29,null,true,TextAnchor.MiddleCenter);
            int i=0;foreach(var p in state.players.OrderByDescending(p=>p.score))
            {
                float y=400+i++*70;Text(new Rect(390,y,600,50),p.name,28);Text(new Rect(1040,y,140,50),p.score+" оч.",28,gold,true,TextAnchor.MiddleRight);
            }
            if(state.phase=="roundEnd")
            {
                Text(new Rect(350,714,900,40),"Следующий раунд через "+Math.Max(0,Math.Ceiling(state.deadline-Clock))+" с",22,muted,false,TextAnchor.MiddleCenter);
                if(Button(new Rect(550,772,500,50),"Готов к следующему раунду"))
                {
                    if(local!=null)foreach(var p in state.players){var cmd=new GameCommand{kind="nextRound",seq=++seq[p.seat]};local.Submit(p.seat,cmd,localTime);}
                    else Send(new GameCommand{kind="nextRound"});
                }
            }
            else if(Button(new Rect(550,760,500,60),"В главное меню"))ExitMatch();
        }
        void Rules()
        {
            Dim();Box(new Rect(185,80,1230,850),panel);
            Text(new Rect(240,125,1100,60),"Как устроена потасовка",39,teal,true);
            string[] tips={
                "3 раунда. Победа в раунде: +3 очка; устранение соперника: +1. В конце побеждает лучший общий счёт.",
                "В начале хода доберите карту. За ход: до 3 заклинаний ИЛИ 1 заклинание и 1 существо. Рука до 8 карт, поле — 5 ячеек.",
                "Существо: выберите карту и свободный слот (или перетащите). Заклинание: выберите карту и цель. Центр означает случайную допустимую цель.",
                "Карта показывается 2 секунды. Затем владелец вводит A S D F G H J, остальные видят огоньки прогресса. После QTE назначьте существу цель. Атаки — по кнопке «Закончить ход».",
                "Стрелки показывают будущие атаки. Зажмите ЛКМ на своём существе, вытяните стрелку и отпустите над целью; центр — случайный герой. Назначение сохраняется.",
                "Реакции: до одной на чужой розыгрыш, без QTE. Копирование и возврат — на призыв; защита — на заклинание. На атаки существ реакций нет. Камера: колесо и ПКМ.",
                "Три ошибки или тайм-аут QTE: карта уходит показанному сопернику. Если закончить ход без QTE, получите бонусный добор. Пустая колода наносит растущую усталость."
            };
            for(int i=0;i<tips.Length;i++)
            {Text(new Rect(240,225+i*78,45,50),(i+1).ToString("00"),24,gold,true);Text(new Rect(305,225+i*78,1050,70),tips[i],21);}
            if(Button(new Rect(550,823,500,60),"Понятно, идём играть"))modal="";
        }
    }
}
