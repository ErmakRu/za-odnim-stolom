using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SummonersTable
{
    public sealed partial class GameApp
    {
        FrontEndCanvas menuCanvas,lobbyCanvas;
        System.Collections.IEnumerator LoadPresentationScenes()
        {
            bool lab=SceneManager.GetActiveScene().name=="PresentationLab"||System.Environment.GetCommandLineArgs().Contains("--presentation-lab");
            if(lab&&!SceneManager.GetSceneByName("PresentationLab").isLoaded)
            {
                foreach(var events in FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None))events.gameObject.SetActive(false);
                yield return SceneManager.LoadSceneAsync("PresentationLab",LoadSceneMode.Additive);
            }
            if(!lab)
                foreach(string name in new[]{"MainMenu","Lobby","Match"})
                    if(!SceneManager.GetSceneByName(name).isLoaded&&Application.CanStreamedLevelBeLoaded(name))yield return SceneManager.LoadSceneAsync(name,LoadSceneMode.Additive);
        }
        void BindFrontEnd()
        {
            foreach(var front in FindObjectsByType<FrontEndCanvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {front.Bind(font,FrontEndAction);if(front.lobby)lobbyCanvas=front;else menuCanvas=front;}
            lobbyCanvas.GetComponentInChildren<MatchOptionsView>(true).changed=options=>
            {if(page=="local")localOptions=options.Copy();else if(page=="steam"&&steam.IsHost)steam.SetOptions(options);};
        }
        void FrontEndAction(string action)
        {
            if(!string.IsNullOrEmpty(ConfigRuntime.Error)&&(action=="local"||action=="steam"||action=="start"||action=="ready")){error=ConfigRuntime.Message;return;}
            if(HandleLobbyChoice(action))return;
            switch(action)
            {
                case "steam":page="steam";steam.Search();break;
                case "local":page="local";break;
                case "cards":returnPage="menu";page="cards";break;
                case "rules":modal="rules";break;
                case "quit":Application.Quit();break;
                case "settings":settingsOpen=true;break;
                case "ready":var me=steam.Members.Find(m=>m.id==steam.UserId.ToString());steam.SetMember(me?.deckId??"noise",me?.ready!=true);break;
                case "start":if(page=="local")StartLocal(localCount);else steam.StartMatch();break;
                case "leave":if(page=="local")page="menu";else steam.Leave();break;
                case "copy":GUIUtility.systemCopyBuffer=steam.RoomId.ToString();break;
                default:if(action.StartsWith("deck")&&int.TryParse(action.Substring(4),out int d))steam.SetMember(catalog.decks[d].id,false);break;
            }
        }
        void SyncFrontEnd()
        {
            if(previewLobby)return;
            bool visible=modal==""&&!quitConfirm&&!settingsOpen;
            if(menuCanvas!=null){menuCanvas.Visible(page=="menu"&&visible);menuCanvas.status.text=string.IsNullOrEmpty(ConfigRuntime.Error)?"ТЕСТ "+catalog.version+" · "+steam.Status:ConfigRuntime.Message;}
            if(lobbyCanvas==null)return;
            bool localLobby=page=="local";
            lobbyCanvas.Visible((localLobby||page=="steam"&&steam.InRoom)&&visible);
            if(localLobby)lobbyCanvas.PresentLobby(LocalLobbyMembers(),"",true,true,localCount>=catalog.world.playerRange.x&&localCount<=catalog.world.playerRange.y,catalog,"ЗА ОДНИМ ПК · "+localCount+" ИГРОКА","Локация: "+catalog.world.name+" · "+catalog.world.playerRange.x+"–"+catalog.world.playerRange.y+" места; матч 2–4 игрока. "+error);
            else if(steam.InRoom)lobbyCanvas.PresentLobby(steam.Members,steam.UserId.ToString(),false,steam.IsHost,steam.CanStart,catalog,steam.RoomName+" · КОД "+steam.RoomId,steam.Error);
            lobbyCanvas.GetComponentInChildren<MatchOptionsView>(true).Present(localLobby?localOptions:steam.Options,localLobby||steam.IsHost&&(steam.View==null||steam.View.phase=="matchEnd"));
        }
        System.Collections.Generic.List<LobbyMember> LocalLobbyMembers()
        {
            return Enumerable.Range(0,localCount).Select(i=>new LobbyMember{id="local-"+i,name=localNames[i],deckId=catalog.decks[localDecks[i]].id,heroId=localHeroes[i],outfit=localOutfits[i],palette=localPalettes[i],ready=true}).ToList();
        }
        bool HandleLobbyChoice(string action)
        {
            bool localLobby=page=="local";
            if(action=="customize-close"){lobbyCanvas.CloseAppearance();return true;}
            if(action.StartsWith("players:")&&localLobby){localCount=int.Parse(action.Substring(8));lobbyCanvas.CloseAppearance();return true;}
            int split=action.IndexOf(':');string kind=split>=0?action.Substring(0,split):action;
            if(!new[]{"deck-prev","deck-next","hero-prev","hero-next","customize","outfit-prev","outfit-next","palette"}.Contains(kind))return false;
            int seatIndex=kind.StartsWith("outfit")||kind=="palette"?lobbyCanvas.AppearanceSeat:int.Parse(action.Substring(split+1));
            var list=localLobby?LocalLobbyMembers():steam.Members;
            if(seatIndex<0||seatIndex>=list.Count||!localLobby&&list[seatIndex].id!=steam.UserId.ToString())return true;
            var member=list[seatIndex];if(kind=="customize"){lobbyCanvas.OpenAppearance(seatIndex);return true;}
            int deck=catalog.decks.FindIndex(d=>d.id==member.deckId),hero=System.Array.IndexOf(HeroOptions.Ids,member.heroId),outfit=member.outfit,palette=member.palette;
            if(kind.StartsWith("deck"))deck=(deck+(kind.EndsWith("next")?1:catalog.decks.Count-1))%catalog.decks.Count;
            if(kind.StartsWith("hero"))hero=(hero+(kind.EndsWith("next")?1:7))%8;
            if(kind.StartsWith("outfit"))outfit=(outfit+(kind.EndsWith("next")?1:7))%8;
            if(kind=="palette")palette=int.Parse(action.Substring(split+1));
            if(localLobby){localDecks[seatIndex]=deck;localHeroes[seatIndex]=HeroOptions.Ids[hero];localOutfits[seatIndex]=outfit;localPalettes[seatIndex]=palette;}
            else if(kind.StartsWith("deck"))steam.SetMember(catalog.decks[deck].id,false);
            else steam.SetAppearance(HeroOptions.Ids[hero],outfit,palette);
            return true;
        }
        public void StartPresentationLab()
        {
            StartLocal(4);handoff=false;
            foreach(var p in local.State.players){p.hand.Clear();p.units.Clear();p.hp=30;}
            foreach(string id in new[]{"C01","C02","S01","S02","R02","R03"})local.State.players[0].hand.Add(new HandCard{uid="lab-"+id,cardId=id});
            for(int i=0;i<4;i++)local.State.players[i].units.Add(new UnitState{uid="lab-unit-"+i,cardId="C03",slot=0,hp=4});
            state=local.View(0,0);
        }
        public void LabAttack(){if(MyAction)Send(new GameCommand{kind="end"});}
        public void LabFinishQte()
        {
            if(local==null)return;
            if(local.State.phase=="reveal"){localTime=local.State.cast.revealUntil;local.Tick(localTime);}
            while(local.State.qte!=null){var q=local.State.qte;local.Submit(q.owner,new GameCommand{seq=++seq[q.owner],kind="key",phaseId=q.id,key=q.sequence[q.index].ToString()},localTime);}
            state=local.View(seat,localTime);
        }
    }
}
