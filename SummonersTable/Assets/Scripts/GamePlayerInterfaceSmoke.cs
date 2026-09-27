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
                if(FindObjectsByType<TableBoard>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length!=0)throw new Exception("Menu created battle world");
                yield return Shot(directory,"01-menu");FrontEndAction("local");yield return Shot(directory,"02-lobby");
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
            finally{File.WriteAllText(Path.Combine(directory,"player-smoke.txt"),passed?"PASS: menu without TableWorld; lobby; one battle world; countdown; Water02 arrow; QTE countdown; five nonblank screenshots without missing shaders.":"FAILED: inspect Player.log");Application.Quit(passed?0:2);}
        }
    }
}
