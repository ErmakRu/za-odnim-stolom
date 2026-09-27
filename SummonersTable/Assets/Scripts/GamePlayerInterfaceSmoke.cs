using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace SummonersTable
{
    public sealed partial class GameApp
    {
        IEnumerator CapturePlayerInterfaceSmoke()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--capture-dir");
            string directory=index>=0&&index+1<args.Length?args[index+1]:Path.Combine(Application.persistentDataPath,"interface-smoke");Directory.CreateDirectory(directory);
            bool passed=false;
            try
            {
                yield return null;
                if(menuCanvas.status.text.Contains("ТЕСТ")||menuCanvas.status.text.Contains("App ID"))throw new Exception("Developer label in menu");
                if(UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/PresentationLab.unity")<0)
                {
                    if(Debug.isDebugBuild)throw new Exception("Development Player");
                    if(Resources.Load<TextAsset>("PerformanceTestRunInfo")!=null||Resources.Load<TextAsset>("PerformanceTestRunSettings")!=null)throw new Exception("Test metadata embedded in release");
                }
                if(FindObjectsByType<TableBoard>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length!=0)throw new Exception("Menu created battle world");
                if(!string.IsNullOrEmpty(ConfigRuntime.Error))throw new Exception("Config blocked modes: "+ConfigRuntime.Error);
                yield return Shot(directory,"01-menu");
                menuCanvas.buttons[Array.IndexOf(menuCanvas.actions,"steam")].onClick.Invoke();yield return null;
                if(page!="steam")throw new Exception("Online button did not open online page");
                FrontEndAction("home");yield return null;
                menuCanvas.buttons[Array.IndexOf(menuCanvas.actions,"local")].onClick.Invoke();yield return null;
                yield return Shot(directory,"02-lobby");
                if(page!="local"||!lobbyCanvas.gameObject.activeInHierarchy)throw new Exception("Local button did not open lobby: page="+page+", active="+lobbyCanvas.gameObject.activeInHierarchy);
                StartLocal(2);handoff=false;yield return null;yield return null;
                if(FindObjectsByType<TableBoard>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length!=1)throw new Exception("Battle world count");
                state.phase="action";state.activeSeat=seat;state.deadline=Clock+9;
                yield return Shot(directory,"03-countdown");
                var countdown=ui.hud.GetComponent<LocalHeroHud>().countdown;
                if(countdown==null||!countdown.number.enabled||countdown.number.text!="9")throw new Exception("Countdown missing in Player");
                var card=state.players[seat].hand.First(h=>catalog.Card(h.cardId).kind=="creature");ChooseHand(card,new Vector2(750,850));previewAim=true;previewAimEnd=new Vector2(1050,450);
                yield return Shot(directory,"04-water-arrow");
                var arrow=board.GetComponentsInChildren<WorldArrowView>().First(x=>x.name=="Unified selection arrow");
                if(arrow.widthMultiplier!=3||!arrow.line.sharedMaterial.name.StartsWith("M_VFX_URP_Trail_Water_02"))throw new Exception("Arrow asset mismatch");
                ClearSelection();previewAim=false;
                Send(new GameCommand{kind="play",cardUid=card.uid,slot=2});
                if(local.State.cast==null)throw new Exception("Summon did not start");
                localTime=local.State.cast.revealUntil+.01;local.Tick(localTime);local.State.qte.deadline=localTime+3;local.State.deadline=localTime+3;state=local.View(seat,localTime);
                yield return Shot(directory,"05-qte-countdown");
                if(countdown.number.text!="3"||!countdown.number.enabled)throw new Exception("QTE countdown missing");
                passed=true;
            }
            finally{File.WriteAllText(Path.Combine(directory,"player-smoke.txt"),passed?"PASS: valid runtime Config; online and local button listeners; menu without TableWorld; lobby; one battle world; countdown; Water02 arrow; QTE countdown; five nonblank screenshots without missing shaders.":"FAILED: inspect Player.log");Application.Quit(passed?0:2);}
        }
    }
}
