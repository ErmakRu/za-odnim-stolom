using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SummonersTable
{
    public sealed class GameApp : MonoBehaviour
    {
        const float W=1600,H=1000;
        readonly Color ink=new Color(.06f,.11f,.14f),panel=new Color(.055f,.105f,.13f,.96f),muted=new Color(.64f,.75f,.76f);
        readonly Color teal=new Color(.25f,.79f,.69f),gold=new Color(.97f,.74f,.36f),red=new Color(1,.43f,.43f);
        readonly Dictionary<string,Texture2D> art=new Dictionary<string,Texture2D>();
        readonly Dictionary<int,GUIStyle> labels=new Dictionary<int,GUIStyle>();
        Catalog catalog; SteamSession steam; GameEngine local; MatchState state;
        GUIStyle buttonStyle,fieldStyle;Font font;
        string page="menu",modal="",returnPage="menu",error="",selectedCard="",selectedUnit="",detailId="",joinCode="",roomName="Весёлый стол";
        string[] localNames={"Игрок 1","Игрок 2","Игрок 3","Игрок 4"};
        int[] localDecks={0,1,2,0};int localCount=2,capacity=4,seat,selectedSlot=-1,targetSeat=-1,catalogDeck=-1;
        string targetUnit="";bool handoff,initialized,quitConfirm;double localTime;int[] seq=new int[4];
        float scale;Vector2 offset,scroll;bool online;
        string seenPhase="";int seenTurn=-1;
        AudioSource audioSource;AudioClip tickTone,failTone,successTone;string lastQte="";int lastProgress,lastMistakes;
        public Catalog Catalog {get {return catalog;}}

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(FindFirstObjectByType<GameApp>()==null)new GameObject("SummonersTable").AddComponent<GameApp>();}
        void Awake()
        {
            Application.runInBackground=true;Application.targetFrameRate=60;
            catalog=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("Data/catalog").text);catalog.Validate();
            font=Font.CreateDynamicFontFromOSFont("Arial",24);
            foreach(var c in catalog.cards)art[c.id]=Resources.Load<Texture2D>("Art/"+c.id);
            foreach(var key in new[]{"menu","board","card_back"})art[key]=Resources.Load<Texture2D>("Art/"+key);
            steam=new SteamSession(catalog);steam.Initialize();
            audioSource=gameObject.AddComponent<AudioSource>();audioSource.volume=.15f;
            tickTone=Tone(680,.045f);failTone=Tone(160,.12f);successTone=Tone(980,.14f);
            var args=Environment.GetCommandLineArgs();
            if(args.Contains("--local-test"))StartLocal(4);
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
            for(int i=0;i<count;i++)members.Add(new LobbyMember{id="local-"+i,name=string.IsNullOrWhiteSpace(localNames[i])?"Игрок "+(i+1):localNames[i],deckId=catalog.decks[localDecks[i]].id,ready=true});
            localTime=0;local=new GameEngine(catalog,members,Environment.TickCount);online=false;seq=new int[4];seat=0;
            page="game";modal="";handoff=true;ClearSelection();seenTurn=-1;state=local.View(seat,localTime);
        }
        void Update()
        {
            steam.Update();
            if(steam.View!=null)
            {
                if(!online||page!="game"){online=true;local=null;page="game";handoff=false;ClearSelection();seenTurn=-1;}
                state=steam.View;seat=steam.Seat;
            }
            else if(online){online=false;page="steam";state=null;}
            if(page=="game"&&local!=null)
            {
                var s=local.State;int next=s.activeSeat;
                if(s.phase=="reaction"&&s.pending!=null)
                {
                    var p=s.players.FirstOrDefault(x=>x.alive&&!s.pending.responded.Contains(x.seat));if(p!=null)next=p.seat;
                }
                if(s.phase!="roundEnd"&&s.phase!="matchEnd"&&next!=seat)
                {seat=next;handoff=true;ClearSelection();}
                if(!handoff&&modal==""&&!quitConfirm)localTime+=Time.unscaledDeltaTime;
                local.Tick(localTime);state=local.View(seat,localTime);
            }
            if(state!=null&&page=="game")
            {
                if(seenTurn!=state.turnNumber){ClearSelection();seenTurn=state.turnNumber;}
                if(seenPhase!=state.phase){seenPhase=state.phase;if(state.phase=="reaction"){selectedCard="";targetSeat=-1;targetUnit="";}}
                var q=state.qte;
                if(q!=null)
                {
                    if(lastQte!=q.id){lastQte=q.id;lastProgress=0;lastMistakes=0;}
                    if(q.index>lastProgress)audioSource.PlayOneShot(tickTone);
                    if(q.mistakes>lastMistakes)audioSource.PlayOneShot(failTone);
                    lastProgress=q.index;lastMistakes=q.mistakes;
                    if(q.owner==seat&&!handoff&&modal==""&&!quitConfirm)
                        foreach(char k in "ASDFGHJ")if(Input.GetKeyDown((KeyCode)Enum.Parse(typeof(KeyCode),k.ToString())))
                        {Send(new GameCommand{kind="key",key=k.ToString(),phaseId=q.id});break;}
                }
                else if(lastQte!=""){lastQte="";audioSource.PlayOneShot(successTone);}
            }
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                if(modal!="")modal="";
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
        void ClearSelection(){selectedCard="";selectedUnit="";detailId="";selectedSlot=-1;targetSeat=-1;targetUnit="";error="";}
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
            SetupStyles();GUI.color=Color.white;Box(new Rect(0,0,Screen.width,Screen.height),Color.black);
            scale=Mathf.Min(Screen.width/W,Screen.height/H);offset=new Vector2((Screen.width-W*scale)/2,(Screen.height-H*scale)/2);
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
            Image("menu",new Rect(0,0,W,H));Box(new Rect(0,0,700,H),new Color(.02f,.06f,.08f,.85f));
            Text(new Rect(75,85,550,38),"АРЕНА ПРИЗЫВА",22,teal,true);
            Text(new Rect(70,160,590,200),"За одним\nстолом",76,null,true);
            Text(new Rect(77,384,515,92),"Призывай нелепых героев.\nПорти планы друзьям. Жми в ритм.",25,muted);
            if(Button(new Rect(78,515,480,66),"Играть через Steam")){page="steam";steam.Search();}
            if(Button(new Rect(78,596,480,60),"За одним ПК • 2–4 игрока",gold))page="local";
            if(Button(new Rect(78,676,232,54),"Колоды и карты",new Color(.77f,.71f,.95f))){returnPage="menu";page="cards";}
            if(Button(new Rect(326,676,232,54),"Как играть",new Color(.7f,.78f,.77f)))modal="rules";
            Text(new Rect(78,780,520,65),steam.Status,17,muted);
            Text(new Rect(78,870,510,50),"ТЕСТ 0.1.0  /  3 РАУНДА  /  30 КАРТ",16,teal,true);
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
                var deck=catalog.decks[catalogDeck];Text(new Rect(65,235,1460,68),deck.subtitle+" • 30 карт: по 2 копии каждой показанной карты.",23,teal,true);
                Text(new Rect(65,290,1460,100),deck.guide,20,muted);start=400;
            }
            float height=Mathf.Ceil(cards.Count/5f)*430;
            scroll=GUI.BeginScrollView(new Rect(55,start,1495,H-start-25),scroll,new Rect(0,0,1470,height));
            for(int i=0;i<cards.Count;i++)CardDetail(cards[i],new Rect((i%5)*294,Mathf.Floor(i/5f)*430,280,412),true);
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
        void Game()
        {
            if(state==null)return;
            Image("board",new Rect(0,0,W,H));Box(new Rect(0,0,W,H),new Color(.02f,.06f,.075f,.56f));
            if(handoff)
            {
                Dim();Text(new Rect(330,220,940,65),"Передайте клавиатуру",42,muted,true,TextAnchor.MiddleCenter);
                Text(new Rect(250,325,1100,95),state.players[seat].name,58,teal,true,TextAnchor.MiddleCenter);
                Text(new Rect(330,450,940,120),state.phase=="reaction"?"Есть возможность сыграть реакцию.\nТаймер запустится после нажатия кнопки.":"Сейчас появится ваша рука.\nОстальные игроки, отвернитесь на минутку.",26,null,false,TextAnchor.MiddleCenter);
                if(Button(new Rect(530,640,540,70),"Я за столом — показать карты")){handoff=false;error="";}
                if(Button(new Rect(650,750,300,48),"В главное меню",muted))quitConfirm=true;return;
            }
            var me=state.players[seat];
            bool inputEnabled=GUI.enabled;
            GUI.enabled=inputEnabled&&state.phase=="action";
            Box(new Rect(18,18,1564,70),panel);
            Text(new Rect(40,34,480,40),"РАУНД "+state.round+" / 3  ·  ХОД "+state.turnNumber,24,teal,true);
            string active=state.players[state.activeSeat].name;
            Text(new Rect(525,32,770,48),(state.phase=="action"?"Ходит: ":"Ритуал: ")+active+"   ·   "+Math.Max(0,Math.Ceiling(state.deadline-Clock))+" с",24);
            if(Button(new Rect(1320,30,115,42),"Правила",muted))modal="rules";
            if(Button(new Rect(1450,30,110,42),"Выйти",muted))quitConfirm=true;
            var opponents=state.players.Where(p=>p.seat!=seat).ToList();
            float opponentWidth=1195f/opponents.Count;
            for(int i=0;i<opponents.Count;i++)Opponent(opponents[i],new Rect(22+i*opponentWidth,110,opponentWidth-12,258));
            Box(new Rect(24,386,1180,79),panel);
            Text(new Rect(42,401,1140,52),state.lastEvent,22,gold,true);
            Text(new Rect(33,482,1170,40),me.name+"  ·  HP "+me.hp+" / 30  ·  Очки "+me.score+"  ·  Колода "+me.deckCount+"  ·  "+catalog.Deck(me.deckId).name,23,me.alive?Color.white:red,true);
            for(int i=0;i<5;i++)
            {
                var r=new Rect(25+i*238,539,220,139);var u=me.units.Find(x=>x.slot==i);
                if(u!=null)UnitTile(me,u,r,true);
                else
                {
                    Box(r,new Color(.05f,.14f,.16f,.80f));Frame(r,selectedSlot==i?gold:new Color(.19f,.35f,.36f),selectedSlot==i?3:1);
                    Text(r,"+\nМесто "+(i+1),21,muted,false,TextAnchor.MiddleCenter);
                    if(GUI.Button(r,"",GUIStyle.none)){selectedSlot=i;selectedUnit="";}
                }
            }
            Text(new Rect(28,699,1180,37),me.alive?"ВАША РУКА  "+me.hand.Count+" / 8   ·   выберите карту, цель и свободное место для существа":"Вы наблюдаете до следующего раунда",17,muted,true);
            float cardWidth=Math.Min(180,1170f/Math.Max(1,me.hand.Count));
            for(int i=0;i<me.hand.Count;i++)
            {
                var hand=me.hand[i];var c=catalog.Card(hand.cardId);var r=new Rect(27+i*cardWidth,743,cardWidth-8,230);
                MiniCard(c,r,selectedCard==hand.uid);
                if(GUI.Button(r,"",GUIStyle.none))
                {
                    selectedCard=hand.uid;selectedUnit="";detailId=c.id;error="";
                    if(c.kind=="creature"&&selectedSlot<0)selectedSlot=Enumerable.Range(0,5).FirstOrDefault(s=>!me.units.Any(u=>u.slot==s));
                }
            }
            SidePanel(me);
            GUI.enabled=inputEnabled;
            if(state.phase=="qte"&&state.qte!=null)QteOverlay();
            if(state.phase=="reaction"&&state.pending!=null)ReactionOverlay();
            if(state.phase=="roundEnd"||state.phase=="matchEnd")ScoreOverlay();
        }
        bool SelectedTarget(int s,string uid=""){return targetSeat==s&&targetUnit==(uid??"");}
        void SelectTarget(int s,string uid="")
        {
            targetSeat=s;targetUnit=uid;
            if(selectedUnit!=""&&state.phase=="action"&&state.activeSeat==seat)
                Send(new GameCommand{kind="target",unitUid=selectedUnit,targetSeat=s,targetUnit=uid});
        }
        void Opponent(PlayerState p,Rect r)
        {
            Box(r,p.alive?panel:new Color(.11f,.10f,.12f,.92f));
            var hero=new Rect(r.x+8,r.y+8,r.width-16,78);
            if(SelectedTarget(p.seat))Frame(hero,gold,3);
            Text(new Rect(hero.x+8,hero.y+5,hero.width-16,34),p.name+(!p.alive?" · выбыл":""),22,null,true);
            Text(new Rect(hero.x+8,hero.y+43,hero.width-16,32),"HP "+p.hp+"  |  Очки "+p.score+"  |  Рука "+p.handCount+"  |  Колода "+p.deckCount,17,p.alive?teal:muted);
            if(GUI.Button(hero,"",GUIStyle.none))SelectTarget(p.seat);
            float uw=(r.width-20)/5;
            for(int s=0;s<5;s++)
            {
                var slot=new Rect(r.x+10+s*uw,r.y+98,uw-5,149);var u=p.units.Find(x=>x.slot==s);
                if(u!=null)UnitTile(p,u,slot,false);
                else{Box(slot,new Color(.08f,.16f,.18f,.7f));Text(slot,"·",20,muted,false,TextAnchor.MiddleCenter);}
            }
        }
        int UnitAttack(PlayerState p,UnitState u)
        {return catalog.Card(u.cardId).attack+Math.Min(2,p.units.Where(x=>x.uid!=u.uid&&catalog.Card(x.cardId).effect=="attackAura").Sum(x=>catalog.Card(x.cardId).value));}
        void UnitTile(PlayerState p,UnitState u,Rect r,bool own)
        {
            var c=catalog.Card(u.cardId);Image(c.id,r);Box(new Rect(r.x,r.yMax-55,r.width,55),new Color(.02f,.06f,.08f,.90f));
            Frame(r,SelectedTarget(p.seat,u.uid)||selectedUnit==u.uid?gold:RoleColor(c),3);
            if(own)
            {
                Text(new Rect(r.x+7,r.y+5,r.width-14,50),c.name,17,null,true);
                Text(new Rect(r.x+7,r.yMax-52,r.width-14,26),"АТК "+UnitAttack(p,u)+"   HP "+u.hp,20,Color.white,true);
                string target=u.plannedSeat<0?"Случайный герой":state.players[u.plannedSeat].name+(u.plannedUnit!=""?" · существо":"");
                Text(new Rect(r.x+7,r.yMax-25,r.width-14,23),u.exhausted?"Отдыхает":u.skipAttacks>0?"Пропустит атаку":target,13,muted);
            }
            else
            {
                Text(new Rect(r.x+3,r.yMax-52,r.width-6,22),UnitAttack(p,u)+" / "+u.hp,16,Color.white,true,TextAnchor.MiddleCenter);
                Text(new Rect(r.x+3,r.yMax-29,r.width-6,26),c.id,13,RoleColor(c),true,TextAnchor.MiddleCenter);
            }
            if(GUI.Button(r,"",GUIStyle.none))
            {
                detailId=c.id;
                if(own&&selectedCard==""&&state.phase=="action"&&state.activeSeat==seat){selectedUnit=u.uid;targetSeat=-1;targetUnit="";}
                else SelectTarget(p.seat,u.uid);
            }
        }
        void MiniCard(CardDef c,Rect r,bool selected)
        {
            Box(r,panel);Box(new Rect(r.x,r.y,r.width,25),TypeColor(c));
            Text(new Rect(r.x+4,r.y+4,r.width-8,21),TypeName(c),13,ink,true,TextAnchor.MiddleCenter);
            Image(c.id,new Rect(r.x+4,r.y+29,r.width-8,105));
            if(c.kind=="creature")Box(new Rect(r.x+4,r.y+134,r.width-8,5),RoleColor(c));
            Text(new Rect(r.x+7,r.y+148,r.width-14,53),c.name,16,null,true);
            Text(new Rect(r.x+7,r.y+203,r.width-14,25),c.kind=="creature"?c.attack+" / "+c.health+"  · Q"+c.qte:c.kind=="reaction"?"БЕЗ QTE":"QTE "+c.qte,15,TypeColor(c),true);
            Frame(r,selected?gold:TypeColor(c),selected?4:1);
        }
        string TargetLabel()
        {
            if(targetSeat<0)return "случайный герой противника";
            var p=state.players[targetSeat];var unit=p.units.Find(u=>u.uid==targetUnit);
            return p.name+(unit!=null?" / "+catalog.Card(unit.cardId).name:" / герой");
        }
        void SidePanel(PlayerState me)
        {
            Box(new Rect(1230,110,346,864),panel);
            var hand=me.hand.Find(h=>h.uid==selectedCard);var card=hand==null?catalog.Card(detailId):catalog.Card(hand.cardId);
            if(card!=null)CardDetail(card,new Rect(1243,122,320,485));
            else
            {
                Text(new Rect(1253,145,300,54),"За одним столом",28,teal,true);
                Text(new Rect(1253,232,300,230),"Нажмите карту в руке: здесь появится её эффект.\n\nВыберите героя или существо на столе, чтобы указать цель.\n\nСвоим существам можно назначить цели перед завершением хода.",21,muted);
            }
            Text(new Rect(1250,619,306,82),"Цель: "+TargetLabel(),18,gold);
            bool turn=state.phase=="action"&&state.activeSeat==seat&&me.alive;
            if(hand!=null&&card.kind!="reaction")
            {
                if(Button(new Rect(1250,709,307,55),"Начать ритуал",teal,turn))
                {
                    Send(new GameCommand{kind="play",cardUid=hand.uid,slot=selectedSlot,targetSeat=targetSeat,targetUnit=targetUnit});
                    if(error==""){selectedCard="";selectedUnit="";}
                }
            }
            else if(selectedUnit!="")
            {
                if(Button(new Rect(1250,709,307,55),"Атаковать случайного героя",gold,turn))
                {Send(new GameCommand{kind="target",unitUid=selectedUnit,targetSeat=-1});targetSeat=-1;targetUnit="";}
            }
            else Text(new Rect(1250,716,308,55),card?.kind=="reaction"?"Реакции доступны в ответ на действие.":"До 3 заклинаний ИЛИ\nзаклинание + существо",18,muted);
            if(Button(new Rect(1250,781,307,51),"Сбросить выбор",muted)){ClearSelection();}
            if(Button(new Rect(1250,845,307,58),"Завершить ход",gold,turn))Send(new GameCommand{kind="end"});
            string message=online&&steam.Error!=""?steam.Error:error;
            Text(new Rect(1250,916,307,56),message!=""?message:turn?"Заклинаний: "+state.spellsPlayed+" / 3":"Ожидаем остальных…",16,message!=""?red:muted);
            // The local hero is also a legal target for healing and rescue.
            if(GUI.Button(new Rect(25,480,1180,43),"",GUIStyle.none))SelectTarget(seat);
        }
        void QteOverlay()
        {
            var q=state.qte;Dim();Box(new Rect(185,174,1230,650),panel);Frame(new Rect(185,174,1230,650),teal,2);
            bool mine=q.owner==seat;Text(new Rect(230,204,1140,46),mine?"ВАШ РИТУАЛ":"РИТУАЛ: "+state.players[q.owner].name,30,teal,true,TextAnchor.MiddleCenter);
            Text(new Rect(230,271,1140,58),catalog.Card(q.cardId).name,38,null,true,TextAnchor.MiddleCenter);
            Text(new Rect(230,342,1140,57),mine?"Нажимайте A S D F G H J на клавиатуре или кнопки ниже":"Наблюдаем за попыткой соперника…",23,muted,false,TextAnchor.MiddleCenter);
            float sw=76*q.sequence.Length;
            for(int i=0;i<q.sequence.Length;i++)
            {
                var r=new Rect(800-sw/2+i*76,425,60,80);
                Box(r,i<q.index?teal:i==q.index?gold:new Color(.17f,.23f,.26f));Text(r,q.sequence[i].ToString(),43,i<=q.index?ink:Color.white,true,TextAnchor.MiddleCenter);
            }
            double left=Math.Max(0,q.deadline-Clock);Box(new Rect(275,542,1050,9),new Color(.18f,.25f,.28f));Box(new Rect(275,542,1050*Mathf.Clamp01((float)(left/q.duration)),9),left<4?red:teal);
            Text(new Rect(275,573,1050,38),"Осталось "+left.ToString("0.0")+" с  ·  Ошибки "+q.mistakes+" / 3  ·  При срыве карта → "+state.players[q.recipient].name,22,Color.white,false,TextAnchor.MiddleCenter);
            for(int i=0;i<7;i++)if(Button(new Rect(428+i*108,647,90,64),"ASDFGHJ"[i].ToString(),gold,mine))Send(new GameCommand{kind="key",phaseId=q.id,key="ASDFGHJ"[i].ToString()});
            Text(new Rect(230,753,1140,35),"Промах: −2 секунды. Реакции не требуют QTE.",19,muted,false,TextAnchor.MiddleCenter);
        }
        bool CanReact(CardDef card,TargetRef t)
        {
            var a=state.pending;if(a==null||card.kind!="reaction")return false;
            bool damage=new[]{"opening","combat","damage","areaDamage"}.Contains(a.kind);
            switch(card.effect)
            {
                case "reduce":case "reflect":return damage&&t.seat==seat&&a.source!=seat;
                case "rescue":return damage;
                case "deny":return t.seat==seat&&a.source!=seat&&catalog.Card(a.cardId).kind=="spell"&&(damage||new[]{"stun","swap","bounce","heal"}.Contains(a.kind));
            }
            return false;
        }
        void ReactionOverlay()
        {
            var a=state.pending;Dim();Box(new Rect(100,100,1400,800),panel);
            Text(new Rect(140,125,1320,53),"МОМЕНТ ДЛЯ РЕАКЦИИ",34,new Color(.73f,.65f,.98f),true);
            Text(new Rect(140,191,1320,60),a.label+"  ·  "+Math.Max(0,Math.Ceiling(state.deadline-Clock))+" с",28);
            Text(new Rect(140,261,1300,60),"Цель действия: "+string.Join(", ",a.targets.Select(t=>state.players[t.seat].name+(t.unit!=""?" / существо":" / герой"))),21,gold);
            bool waiting=a.responded.Contains(seat);var me=state.players[seat];
            var eligible=me.hand.Where(h=>a.targets.Any(t=>CanReact(catalog.Card(h.cardId),t))).ToList();
            if(!waiting)
            {
                Text(new Rect(140,323,1320,45),"Можно сыграть одну реакцию. Выберите карту, затем цель защиты. QTE не нужен.",21,muted);
                for(int i=0;i<eligible.Count;i++)
                {
                    var h=eligible[i];var r=new Rect(140+i*160,390,148,230);MiniCard(catalog.Card(h.cardId),r,selectedCard==h.uid);
                    if(GUI.Button(r,"",GUIStyle.none))selectedCard=h.uid;
                }
                var selected=eligible.Find(h=>h.uid==selectedCard);
                if(selected!=null)
                {
                    var c=catalog.Card(selected.cardId);Text(new Rect(140,640,1310,70),c.rules,22);
                    var targets=a.targets.Where(t=>CanReact(c,t)).ToList();
                    for(int i=0;i<targets.Count;i++)
                    {
                        var t=targets[i];if(Button(new Rect(140+i*325,737,305,55),"Защитить: "+state.players[t.seat].name,new Color(.73f,.65f,.98f)))
                        {Send(new GameCommand{kind="react",cardUid=selected.uid,phaseId=a.id,targetSeat=t.seat,targetUnit=t.unit});selectedCard="";break;}
                    }
                }
                if(Button(new Rect(1110,821,345,50),"Пропустить реакцию",muted))Send(new GameCommand{kind="pass",phaseId=a.id});
            }
            else
            {
                Text(new Rect(220,450,1160,150),"Ваш ответ принят или подходящих реакций нет.\nОжидаем остальных игроков.",31,muted,false,TextAnchor.MiddleCenter);
            }
            Text(new Rect(140,831,910,40),error,19,red);
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
                "Выберите карту → цель → свободное место для существа → «Начать ритуал». Все существа и заклинания требуют QTE: A S D F G H J.",
                "Существо сначала атакует, затем выходит на стол и включает постоянный эффект. Атака в существо вызывает ответный урон. После призыва ход завершается.",
                "Готовые существа атакуют в конце хода. Нажмите своё существо, затем цель. Без назначения они бьют случайного вражеского героя.",
                "Реакции — ответ на объявленную атаку или заклинание, без QTE. До одной реакции от игрока за окно в 7 секунд.",
                "Три ошибки или тайм-аут QTE: карта уходит показанному сопернику. Если закончить ход без QTE, получите бонусный добор. Пустая колода наносит растущую усталость."
            };
            for(int i=0;i<tips.Length;i++)
            {Text(new Rect(240,225+i*78,45,50),(i+1).ToString("00"),24,gold,true);Text(new Rect(305,225+i*78,1050,70),tips[i],21);}
            if(Button(new Rect(550,823,500,60),"Понятно, идём играть"))modal="";
        }
    }
}
