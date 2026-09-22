using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Steamworks;
using UnityEngine;

namespace SummonersTable
{
    public sealed class RoomInfo
    {
        public ulong id; public string name; public int count, capacity;
    }

    // Lobby membership authenticates the sender; all game rules run on the owner.
    public sealed class SteamSession : IDisposable
    {
        const string GameTag="gamejams.summoners-table.20260921";
        const int Channel=17423, MaxPacket=65536;
        readonly Catalog catalog;
        readonly IntPtr[] inbox=new IntPtr[32];
        readonly Dictionary<ulong,double> lastSeen=new Dictionary<ulong,double>();
        readonly Dictionary<ulong,int> rateCount=new Dictionary<ulong,int>();
        readonly List<IDisposable> callbacks=new List<IDisposable>();
        CallResult<LobbyCreated_t> createResult;
        CallResult<LobbyEnter_t> joinResult;
        CallResult<LobbyMatchList_t> listResult;
        CSteamID lobby, matchHost;
        double lastPublish, lastHeartbeat, lastRefresh, operationStarted, rateWindow;
        int requestGeneration, commandSequence;
        bool quickSearch;
        string selectedDeck="noise";
        string selectedHero="badger";int selectedOutfit,selectedPalette;
        public bool Available {get;private set;}
        public bool Busy {get;private set;}
        public bool InRoom {get {return lobby.m_SteamID!=0;}}
        public bool IsHost {get {return Available&&InRoom&&SteamMatchmaking.GetLobbyOwner(lobby)==SteamUser.GetSteamID();}}
        public string Status="Steam не подключён";
        public string Error="";
        public string Name {get;private set;}
        public ulong UserId {get;private set;}
        public ulong RoomId {get {return lobby.m_SteamID;}}
        public string RoomName="";
        public int Capacity=4;
        public List<LobbyMember> Members=new List<LobbyMember>();
        public List<RoomInfo> Rooms=new List<RoomInfo>();
        public GameEngine Engine {get;private set;}
        public MatchState View {get;private set;}
        public int Seat=-1;
        public double ReceivedAt {get;private set;}
        public SteamSession(Catalog catalog) {this.catalog=catalog;}
        static double TimeNow {get {return Time.realtimeSinceStartupAsDouble;}}
        public void Initialize()
        {
            if(Available)return;
            try
            {
                if(!SteamAPI.Init()){Status="Запустите Steam, войдите в аккаунт и нажмите «Повторить».";return;}
                Available=true;UserId=SteamUser.GetSteamID().m_SteamID;Name=Clean(SteamFriends.GetPersonaName(),28);
                Status="Steam: "+Name+" • тестовый App ID 480";Debug.Log("STEAM_INITIALIZED app=480");
                SteamNetworkingUtils.InitRelayNetworkAccess();
                callbacks.Add(Callback<LobbyDataUpdate_t>.Create(x=>{if(x.m_ulSteamIDLobby==RoomId)RefreshMembers();}));
                callbacks.Add(Callback<LobbyChatUpdate_t>.Create(x=>{if(x.m_ulSteamIDLobby==RoomId)RefreshMembers();}));
                callbacks.Add(Callback<SteamNetworkingMessagesSessionRequest_t>.Create(x=>{
                    var identity=x.m_identityRemote;
                    if(AllowedPeer(identity.GetSteamID64()))SteamNetworkingMessages.AcceptSessionWithUser(ref identity);
                    else SteamNetworkingMessages.CloseSessionWithUser(ref identity);
                }));
                callbacks.Add(Callback<SteamNetworkingMessagesSessionFailed_t>.Create(x=>{
                    if(AllowedPeer(x.m_info.m_identityRemote.GetSteamID64()))Error="Steam: соединение прервалось. Ожидаем восстановления…";
                }));
                callbacks.Add(Callback<GameLobbyJoinRequested_t>.Create(x=>{if(View==null)Join(x.m_steamIDLobby.m_SteamID);}));
            }
            catch(Exception e){Status="Steam недоступен: "+e.Message;Debug.LogWarning(Status);Available=false;}
        }
        public static string Clean(string text,int max)
        {
            var s=new string((text??"").Where(c=>!char.IsControl(c)&&c!='<'&&c!='>').ToArray());
            return s.Length>max?s.Substring(0,max):s;
        }
        bool Compatible(CSteamID room)
        {
            return SteamMatchmaking.GetLobbyData(room,"game")==GameTag&&
                SteamMatchmaking.GetLobbyData(room,"version")==catalog.version&&SteamMatchmaking.GetLobbyData(room,"protocol")=="4";
        }
        public void Search(bool quick=false)
        {
            if(!Available||Busy||InRoom)return;
            Busy=true;quickSearch=quick;Error="";Rooms.Clear();operationStarted=TimeNow;int token=++requestGeneration;
            SteamMatchmaking.AddRequestLobbyListStringFilter("game",GameTag,ELobbyComparison.k_ELobbyComparisonEqual);
            SteamMatchmaking.AddRequestLobbyListStringFilter("version",catalog.version,ELobbyComparison.k_ELobbyComparisonEqual);
            SteamMatchmaking.AddRequestLobbyListStringFilter("protocol","4",ELobbyComparison.k_ELobbyComparisonEqual);
            SteamMatchmaking.AddRequestLobbyListStringFilter("state","waiting",ELobbyComparison.k_ELobbyComparisonEqual);
            SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
            SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable(1);
            SteamMatchmaking.AddRequestLobbyListResultCountFilter(40);
            listResult=CallResult<LobbyMatchList_t>.Create((result,failed)=>{
                if(token!=requestGeneration)return;Busy=false;
                if(failed){Error="Steam не вернул список комнат. Попробуйте ещё раз.";return;}
                for(int i=0;i<result.m_nLobbiesMatching;i++)
                {
                    var room=SteamMatchmaking.GetLobbyByIndex(i);if(!Compatible(room))continue;
                    Rooms.Add(new RoomInfo{id=room.m_SteamID,name=Clean(SteamMatchmaking.GetLobbyData(room,"name"),36),
                        count=SteamMatchmaking.GetNumLobbyMembers(room),capacity=SteamMatchmaking.GetLobbyMemberLimit(room)});
                }
                if(quickSearch){if(Rooms.Count>0)Join(Rooms[0].id);else Create("Стол "+Name,4,true);}
            });
            listResult.Set(SteamMatchmaking.RequestLobbyList());
        }
        public void Create(string name,int capacity,bool isPublic)
        {
            if(!Available||Busy||InRoom)return;
            Busy=true;Error="";Capacity=Mathf.Clamp(capacity,2,4);RoomName=Clean(name,36);operationStarted=TimeNow;
            if(string.IsNullOrWhiteSpace(RoomName))RoomName="За одним столом";
            int token=++requestGeneration;
            createResult=CallResult<LobbyCreated_t>.Create((result,failed)=>{
                if(token!=requestGeneration){if(result.m_ulSteamIDLobby!=0)SteamMatchmaking.LeaveLobby(new CSteamID(result.m_ulSteamIDLobby));return;}
                Busy=false;
                if(failed||result.m_eResult!=EResult.k_EResultOK){Error="Не удалось создать лобби: "+result.m_eResult;return;}
                lobby=new CSteamID(result.m_ulSteamIDLobby);Debug.Log("STEAM_LOBBY_CREATED");
                SteamMatchmaking.SetLobbyData(lobby,"game",GameTag);SteamMatchmaking.SetLobbyData(lobby,"version",catalog.version);
                SteamMatchmaking.SetLobbyData(lobby,"protocol","4");
                SteamMatchmaking.SetLobbyData(lobby,"state","waiting");SteamMatchmaking.SetLobbyData(lobby,"name",RoomName);
                SteamMatchmaking.SetLobbyJoinable(lobby,true);SetMember(selectedDeck,false);RefreshMembers();
            });
            createResult.Set(SteamMatchmaking.CreateLobby(isPublic?ELobbyType.k_ELobbyTypePublic:ELobbyType.k_ELobbyTypePrivate,Capacity));
        }
        public void Join(ulong id)
        {
            if(!Available||Busy||InRoom||id==0)return;
            Busy=true;Error="";operationStarted=TimeNow;int token=++requestGeneration;
            joinResult=CallResult<LobbyEnter_t>.Create((result,failed)=>{
                var joined=new CSteamID(result.m_ulSteamIDLobby);
                if(token!=requestGeneration){if(joined.m_SteamID!=0)SteamMatchmaking.LeaveLobby(joined);return;}
                Busy=false;
                if(failed||result.m_EChatRoomEnterResponse!=(uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
                {Error="Лобби недоступно, заполнено или уже играет.";return;}
                if(!Compatible(joined)||SteamMatchmaking.GetLobbyData(joined,"state")!="waiting")
                {SteamMatchmaking.LeaveLobby(joined);Error="Это лобби другой игры, версии или начавшегося матча.";return;}
                lobby=joined;RoomName=Clean(SteamMatchmaking.GetLobbyData(lobby,"name"),36);
                Capacity=SteamMatchmaking.GetLobbyMemberLimit(lobby);SetMember(selectedDeck,false);RefreshMembers();
            });
            joinResult.Set(SteamMatchmaking.JoinLobby(new CSteamID(id)));
        }
        public void CancelSearch(){requestGeneration++;Busy=false;quickSearch=false;}
        public void SetMember(string deck,bool ready)
        {
            if(catalog.Deck(deck)==null)return;selectedDeck=deck;
            if(!Available||!InRoom||View!=null)return;
            SteamMatchmaking.SetLobbyMemberData(lobby,"deck",deck);
            SteamMatchmaking.SetLobbyMemberData(lobby,"hero",selectedHero);
            SteamMatchmaking.SetLobbyMemberData(lobby,"outfit",selectedOutfit.ToString());SteamMatchmaking.SetLobbyMemberData(lobby,"palette",selectedPalette.ToString());
            SteamMatchmaking.SetLobbyMemberData(lobby,"ready",ready?"1":"0");RefreshMembers();
        }
        public void SetAppearance(string hero,int outfit,int palette)
        {
            if(!HeroOptions.Valid(hero)||outfit<0||outfit>7||palette<0||palette>3)return;
            selectedHero=hero;selectedOutfit=outfit;selectedPalette=palette;SetMember(selectedDeck,false);
        }
        public void SubmitLook(float yaw,float pitch,int mode)
        {string previous=Error;Submit(new GameCommand{kind="look",lookYaw=yaw,lookPitch=pitch,cameraMode=mode});Error=previous;}
        public void RefreshMembers()
        {
            if(!Available||!InRoom)return;
            var next=new List<LobbyMember>();var owner=SteamMatchmaking.GetLobbyOwner(lobby);
            for(int i=0;i<SteamMatchmaking.GetNumLobbyMembers(lobby);i++)
            {
                var id=SteamMatchmaking.GetLobbyMemberByIndex(lobby,i);string deck=SteamMatchmaking.GetLobbyMemberData(lobby,id,"deck");
                bool valid=catalog.Deck(deck)!=null;
                string hero=SteamMatchmaking.GetLobbyMemberData(lobby,id,"hero");
                int.TryParse(SteamMatchmaking.GetLobbyMemberData(lobby,id,"outfit"),out int outfit);int.TryParse(SteamMatchmaking.GetLobbyMemberData(lobby,id,"palette"),out int palette);
                next.Add(new LobbyMember{id=id.m_SteamID.ToString(),name=Clean(SteamFriends.GetFriendPersonaName(id),28),
                    heroId=HeroOptions.Normalize(hero),outfit=HeroOptions.Outfit(outfit),palette=HeroOptions.Palette(palette),deckId=valid?deck:"noise",ready=valid&&HeroOptions.Valid(hero)&&SteamMatchmaking.GetLobbyMemberData(lobby,id,"ready")=="1"});
            }
            Members=next.OrderBy(m=>m.id==owner.m_SteamID.ToString()?0:1).ThenBy(m=>m.id).ToList();
            if(View!=null&&owner!=matchHost){EndBrokenMatch("Хост покинул матч. Создайте новое лобби.");return;}
            if(Engine!=null)
                foreach(var p in Engine.State.players.Where(p=>p.connected&&!Members.Any(m=>m.id==p.id)).ToList())Engine.Disconnect(p.seat,TimeNow);
        }
        public bool CanStart {get {return IsHost&&!Busy&&View==null&&Members.Count>=2&&Members.All(m=>m.ready);}}
        public void StartMatch()
        {
            RefreshMembers();if(!CanStart)return;
            matchHost=SteamUser.GetSteamID();commandSequence=0;
            SteamMatchmaking.SetLobbyJoinable(lobby,false);SteamMatchmaking.SetLobbyData(lobby,"state","playing");
            Engine=new GameEngine(catalog,Members,Environment.TickCount,TimeNow);Seat=Engine.State.players.Find(p=>p.id==UserId.ToString()).seat;
            foreach(var m in Members)lastSeen[ulong.Parse(m.id)]=TimeNow;
            Publish();
        }
        bool AllowedPeer(ulong id)
        {
            return Available&&InRoom&&id!=UserId&&Members.Any(m=>m.id==id.ToString())&&
                (IsHost||id==(matchHost.m_SteamID!=0?matchHost:SteamMatchmaking.GetLobbyOwner(lobby)).m_SteamID);
        }
        void Send(ulong id,WireMessage message)
        {
            if(!AllowedPeer(id))return;
            byte[] bytes=Encoding.UTF8.GetBytes(JsonUtility.ToJson(message));if(bytes.Length>MaxPacket)return;
            IntPtr data=Marshal.AllocHGlobal(bytes.Length);
            try
            {
                Marshal.Copy(bytes,0,data,bytes.Length);var identity=new SteamNetworkingIdentity();identity.SetSteamID64(id);
                var result=SteamNetworkingMessages.SendMessageToUser(ref identity,data,(uint)bytes.Length,Constants.k_nSteamNetworkingSend_Reliable,Channel);
                if(result!=EResult.k_EResultOK)Error="Ожидание Steam-соединения: "+result;
            }
            finally{Marshal.FreeHGlobal(data);}
        }
        public void Submit(GameCommand command)
        {
            if(View==null||Seat<0)return;command.seq=++commandSequence;Error="";
            if(Engine!=null)
            {
                var result=Engine.Submit(Seat,command,TimeNow);if(!result.ok)Error=result.message;else Error="";Publish();
            }
            else Send(matchHost.m_SteamID,new WireMessage{kind="command",matchId=View.matchId,command=command});
        }
        void Publish()
        {
            if(Engine==null)return;
            foreach(var p in Engine.State.players.Where(p=>p.connected))
            {
                var view=Engine.View(p.seat,TimeNow);
                if(p.id==UserId.ToString()){View=view;ReceivedAt=TimeNow;}
                else Send(ulong.Parse(p.id),new WireMessage{kind="state",matchId=view.matchId,state=view});
            }
            lastPublish=TimeNow;
        }
        void Receive()
        {
            int count=SteamNetworkingMessages.ReceiveMessagesOnChannel(Channel,inbox,inbox.Length);
            for(int i=0;i<count;i++)
            {
                try
                {
                    var packet=SteamNetworkingMessage_t.FromIntPtr(inbox[i]);ulong sender=packet.m_identityPeer.GetSteamID64();
                    if(packet.m_cbSize<=0||packet.m_cbSize>MaxPacket||!AllowedPeer(sender))continue;
                    int used=rateCount.TryGetValue(sender,out var c)?c:0;if(used>=120)continue;rateCount[sender]=used+1;
                    var bytes=new byte[packet.m_cbSize];Marshal.Copy(packet.m_pData,bytes,0,bytes.Length);
                    var wire=JsonUtility.FromJson<WireMessage>(Encoding.UTF8.GetString(bytes));
                    if(wire==null||wire.protocol!=4)continue;
                    if(Engine!=null)
                    {
                        var p=Engine.State.players.Find(x=>x.id==sender.ToString()&&x.connected);
                        if(p==null||wire.matchId!=Engine.State.matchId)continue;
                        lastSeen[sender]=TimeNow;
                        if(wire.kind=="command"&&wire.command!=null&&bytes.Length<=2048)
                        {
                            var result=Engine.Submit(p.seat,wire.command,TimeNow);
                            if(!result.ok)Send(sender,new WireMessage{kind="error",matchId=Engine.State.matchId,text=result.message});
                            Publish();
                        }
                    }
                    else if(wire.kind=="state"&&wire.state!=null&&wire.state.version==catalog.version&&wire.state.matchId==wire.matchId)
                    {
                        if(wire.state.players==null||wire.state.players.Count<2||wire.state.players.Count>4)continue;
                        var own=wire.state.players.Find(p=>p.id==UserId.ToString());if(own==null)continue;
                        if(View!=null&&(View.matchId!=wire.matchId||wire.state.revision<View.revision))continue;
                        if(View==null){matchHost=new CSteamID(sender);commandSequence=0;Error="";}
                        if(Error.StartsWith("Нет обновлений от хоста.")||Error.StartsWith("Ожидание Steam-соединения:"))Error="";
                        wire.state.RestoreViewPrivacy(own.seat);
                        Seat=own.seat;View=wire.state;ReceivedAt=TimeNow;
                    }
                    else if(wire.kind=="error"&&View!=null&&View.matchId==wire.matchId)Error=Clean(wire.text,180);
                }
                catch(Exception e){Debug.LogWarning("Discarded Steam packet: "+e.Message);}
                finally{SteamNetworkingMessage_t.Release(inbox[i]);}
            }
        }
        public void Update()
        {
            if(!Available)return;
            SteamAPI.RunCallbacks();double now=TimeNow;
            if(now-rateWindow>=1){rateCount.Clear();rateWindow=now;}
            if(Busy&&now-operationStarted>25){CancelSearch();Error="Steam не ответил за 25 секунд. Повторите поиск.";}
            if(!InRoom)return;
            if(now-lastRefresh>1){lastRefresh=now;RefreshMembers();}
            Receive();
            if(Engine!=null)
            {
                Engine.Tick(now);
                foreach(var p in Engine.State.players.Where(p=>p.connected&&p.seat!=Seat).ToList())
                    if(lastSeen.TryGetValue(ulong.Parse(p.id),out var seen)&&now-seen>45)Engine.Disconnect(p.seat,now);
                if(now-lastPublish>.2)Publish();
            }
            else if(View!=null)
            {
                if(now-lastHeartbeat>2){lastHeartbeat=now;Send(matchHost.m_SteamID,new WireMessage{kind="heartbeat",matchId=View.matchId});}
                if(now-ReceivedAt>12)Error="Нет обновлений от хоста. Восстанавливаем соединение…";
                if(now-ReceivedAt>45)EndBrokenMatch("Соединение с хостом потеряно. Вернитесь в поиск лобби.");
            }
        }
        void EndBrokenMatch(string message){Leave();Error=message;}
        public void Leave()
        {
            CancelSearch();
            if(Available&&InRoom)
            {
                foreach(var m in Members.Where(m=>m.id!=UserId.ToString()))
                {var identity=new SteamNetworkingIdentity();identity.SetSteamID64(ulong.Parse(m.id));SteamNetworkingMessages.CloseSessionWithUser(ref identity);}
                SteamMatchmaking.LeaveLobby(lobby);
            }
            lobby=new CSteamID(0);matchHost=new CSteamID(0);Members.Clear();lastSeen.Clear();Engine=null;View=null;Seat=-1;
        }
        public void Dispose()
        {
            Leave();foreach(var cb in callbacks)cb.Dispose();callbacks.Clear();
            createResult?.Dispose();joinResult?.Dispose();listResult?.Dispose();if(Available)SteamAPI.Shutdown();Available=false;
        }
    }
}
