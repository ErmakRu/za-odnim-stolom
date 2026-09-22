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
        }
        void FrontEndAction(string action)
        {
            switch(action)
            {
                case "steam":page="steam";steam.Search();break;
                case "local":page="local";break;
                case "cards":returnPage="menu";page="cards";break;
                case "rules":modal="rules";break;
                case "quit":Application.Quit();break;
                case "ready":var me=steam.Members.Find(m=>m.id==steam.UserId.ToString());steam.SetMember(me?.deckId??"noise",me?.ready!=true);break;
                case "start":steam.StartMatch();break;
                case "leave":steam.Leave();break;
                case "copy":GUIUtility.systemCopyBuffer=steam.RoomId.ToString();break;
                default:if(action.StartsWith("deck")&&int.TryParse(action.Substring(4),out int d))steam.SetMember(catalog.decks[d].id,false);break;
            }
        }
        void SyncFrontEnd()
        {
            if(previewLobby)return;
            bool visible=modal==""&&!quitConfirm;
            if(menuCanvas!=null){menuCanvas.Visible(page=="menu"&&visible);menuCanvas.status.text="ТЕСТ "+Application.version+" · "+steam.Status;}
            if(lobbyCanvas==null)return;
            lobbyCanvas.Visible(page=="steam"&&steam.InRoom&&visible);
            if(!steam.InRoom)return;
            lobbyCanvas.title.text=steam.RoomName;lobbyCanvas.subtitle.text="Код лобби: "+steam.RoomId+" · Выберите колоду и подтвердите готовность";lobbyCanvas.status.text=steam.Error;
            for(int i=0;i<4;i++)
            {
                var member=i<steam.Members.Count?steam.Members[i]:null;
                lobbyCanvas.members[i].text=member==null?"Свободное место":member.name+"  ·  "+catalog.Deck(member.deckId).name+"  ·  "+(member.ready?"ГОТОВ":"выбирает колоду");
            }
            for(int i=0;i<3;i++)lobbyCanvas.buttons[i].GetComponentInChildren<Text>().text=catalog.decks[i].name;
            var own=steam.Members.Find(m=>m.id==steam.UserId.ToString());lobbyCanvas.buttons[3].GetComponentInChildren<Text>().text=own?.ready==true?"Снять готовность":"Я готов!";
            lobbyCanvas.buttons[4].interactable=steam.IsHost&&steam.CanStart;
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
