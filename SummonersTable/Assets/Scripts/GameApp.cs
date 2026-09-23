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
        Catalog catalog; SteamSession steam; GameEngine local; MatchState state;
        MatchOptions localOptions=new MatchOptions();
        Font font;
        string page="menu",modal="",returnPage="menu",error="",selectedCard="",selectedUnit="";
        string[] localNames={"Игрок 1","Игрок 2","Игрок 3","Игрок 4"};
        string[] localHeroes={"badger","deer","owl","lion"};int[] localOutfits={0,1,2,3},localPalettes={0,1,2,3};
        double nextLookSend;Vector2 lastSentLook=new Vector2(999,999);int lastSentMode=-1;
        int[] localDecks={0,1,2,0};int localCount=2,capacity=4,seat,selectedSlot=-1,catalogDeck=-1;
        bool handoff,quitConfirm;double localTime;int[] seq=new int[4];
        float scale;Vector2 offset;bool online;
        string seenPhase="";int seenTurn=-1;
        AudioSource audioSource;AudioClip tickTone,failTone,successTone;string lastQte="";int lastProgress,lastMistakes;
        CardTableCanvas cardCanvas;
        MotionAnnouncements announcements;
        public bool IsReady {get{return steam!=null&&board!=null&&cardCanvas!=null;}}
        public Catalog Catalog {get {return catalog;}}

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(FindFirstObjectByType<AuthoringLab>()==null&&FindFirstObjectByType<GameApp>()==null)new GameObject("SummonersTable").AddComponent<GameApp>();}
        IEnumerator Start()
        {
            yield return LoadPresentationScenes();
            Application.runInBackground=true;ConfigRuntime.LoadInitial();UserSettings.Load();UserSettings.Apply(false,true);
            catalog=ConfigRuntime.ActiveCatalog;catalog.Validate();localOptions=ConfigRuntime.Current.rules.defaults.Copy();
            font=Font.CreateDynamicFontFromOSFont("Arial",24);BindFrontEnd();
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
            captureMode=Environment.GetCommandLineArgs().Contains("--capture-preview")||Environment.GetCommandLineArgs().Contains("--capture-lab")||Environment.GetCommandLineArgs().Contains("--capture-shaders")||Environment.GetCommandLineArgs().Contains("--capture-options")||Environment.GetCommandLineArgs().Contains("--capture-config");
            steam=new SteamSession(catalog);if(!captureMode&&FindFirstObjectByType<PresentationLab>()==null)steam.Initialize();
            BindPrefabInterface();ConfigRuntime.ApplyScene();
            tickTone=Tone(680,.045f);failTone=Tone(160,.12f);successTone=Tone(980,.14f);
            var args=Environment.GetCommandLineArgs();
            if(args.Contains("--local-test"))StartLocal(4);
            if(args.Contains("--capture-config"))StartCoroutine(CaptureConfigPreview());
            else if(args.Contains("--capture-options"))StartCoroutine(CaptureOptionsPreview());
            else if(args.Contains("--capture-shaders"))StartCoroutine(CaptureShaderPreview());
            else if(args.Contains("--capture-lab"))StartCoroutine(CaptureLabPreview());
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
            if(!string.IsNullOrEmpty(ConfigRuntime.Error)){error=ConfigRuntime.Message;return;}RefreshConfigBetweenMatches();if(count<Math.Max(2,catalog.world.playerRange.x)||count>Math.Min(4,catalog.world.playerRange.y)){error="Эта локация допускает "+catalog.world.playerRange.x+"–"+catalog.world.playerRange.y+" игроков. Матч поддерживает 2–4.";return;}steam.Leave();var members=new List<LobbyMember>();
            for(int i=0;i<count;i++)members.Add(new LobbyMember{id="local-"+i,name=string.IsNullOrWhiteSpace(localNames[i])?"Игрок "+(i+1):localNames[i],deckId=catalog.decks[localDecks[i]].id,heroId=localHeroes[i],outfit=localOutfits[i],palette=localPalettes[i],ready=true});
            localTime=0;local=new GameEngine(catalog,members,Environment.TickCount,0,localOptions);online=false;seq=new int[4];seat=0;
            page="game";modal="";handoff=true;ClearSelection();seenTurn=-1;state=local.View(seat,localTime);
        }
        void Update()
        {
            if(!IsReady)return;
            RefreshConfigBetweenMatches();if(!captureMode)UpdateSession();SyncFrontEnd();
            CardPresentationContext.Apply(page=="game"?state?.options:page=="local"?localOptions:steam.InRoom?steam.Options:null);
            UpdateJournal();
            if(!captureMode&&announcements!=null)announcements.Present(handoff?"handoff":page,state,page=="local"?localCount:steam.Members.Count);
            if(page=="game"&&state!=null&&!handoff)
            {
                board.inputEnabled=!captureMode&&!InputBlocked&&!historyOpen&&!OverInterface(Input.mousePosition);
                board.selectedUnit=selectedUnit;board.choosingTarget=selectedUnit!=""||(selectedCard!=""&&catalog.Card(state.players[seat].hand.Find(h=>h.uid==selectedCard)?.cardId)?.kind=="spell");
                var selectedDefinition=catalog.Card(state.players[seat].hand.Find(h=>h.uid==selectedCard)?.cardId);
                board.placingCreature=selectedDefinition?.kind=="creature";board.targetMode=selectedUnit!=""?"enemy":selectedDefinition?.target??"none";
                board.Sync(state,seat,Clock,selectedSlot);
                if(!captureMode&&Time.unscaledTimeAsDouble>=nextLookSend&&(Vector2.Distance(lastSentLook,board.CameraRig.Look)>1||lastSentMode!=board.CameraRig.Mode))
                {
                    nextLookSend=Time.unscaledTimeAsDouble+.2;lastSentLook=board.CameraRig.Look;lastSentMode=board.CameraRig.Mode;
                    if(online)steam.SubmitLook(lastSentLook.x,lastSentLook.y,lastSentMode);
                    else if(local!=null)local.Submit(seat,new GameCommand{seq=++seq[seat],kind="look",lookYaw=lastSentLook.x,lookPitch=lastSentLook.y,cameraMode=lastSentMode},localTime);
                }
                if(!captureMode&&!historyOpen&&!settingsOpen)ReadQteKeys();
            }
            else board.gameObject.SetActive(false);
            UpdateAuthoredInterface();
            var inspected=InspectionAt(Pointer);
            cardCanvas.Present(state,seat,state==null?0:Clock,inspected,catalog,board,page=="game"&&!handoff&&!settingsOpen&&modal==""&&!quitConfirm&&state.phase!="roundEnd"&&state.phase!="matchEnd");
            foreach(var key in cardCanvas.keyButtons)key.interactable=!historyOpen;
            if(historyOpen)cardCanvas.CoverWithJournal();
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                if(quitConfirm)quitConfirm=false;
                else if(modal!="")modal="";
                else if(historyOpen)historyOpen=false;
                else if(settingsOpen)settingsOpen=false;
                else if(page=="game"){settingsOpen=!settingsOpen;ClearSelection();}
                else if(steam.InRoom)steam.Leave();
                else{steam.CancelSearch();page="menu";}
            }
        }
        void Send(GameCommand command)
        {
            if(online){steam.Submit(command);error=steam.Error;state=steam.View;if(error==""&&(command.kind=="target"||command.kind=="play"))ui.arrow.SelectTarget();}
            else if(local!=null){command.seq=++seq[seat];var result=local.Submit(seat,command,localTime);error=result.ok?"":result.message;state=local.View(seat,localTime);if(result.ok&&(command.kind=="target"||command.kind=="play"))ui.arrow.SelectTarget();}
        }
        double Clock {get {return online?state.serverTime+Time.realtimeSinceStartupAsDouble-steam.ReceivedAt:localTime;}}
        void ClearSelection(){selectedCard="";selectedUnit="";selectedSlot=-1;mouseHeld=false;draggingCard=false;unitPointerHeld=false;unitDragMoved=false;error="";}
        void ExitMatch(){steam.Leave();local=null;state=null;online=false;page="menu";quitConfirm=false;settingsOpen=false;postMatchLobby=false;historyOpen=false;ClearSelection();}
        Color RoleColor(CardDef c){var role=catalog.roleColors.Find(r=>r.name==c.role);ColorUtility.TryParseHtmlString(role?.hex??"#FFFFFF",out var color);return color;}
    }
}
