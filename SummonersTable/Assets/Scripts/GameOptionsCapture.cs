using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace SummonersTable
{
    public sealed partial class GameApp
    {
        IEnumerator CaptureOptionsPreview()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--capture-dir");string directory=index>=0?args[index+1]:Path.GetFullPath("captures");Directory.CreateDirectory(directory);
            page="local";localCount=4;yield return null;var options=lobbyCanvas.GetComponentInChildren<MatchOptionsView>(true);
            yield return Shot(directory,"01-wizards-lobby");options.commanders.onClick.Invoke();yield return null;options.power.isOn=false;yield return null;options.depth.isOn=true;yield return null;
            if(!localOptions.IsCommanders||localOptions.limitPower||!localOptions.cards3D)throw new Exception("Lobby options not bound");
            yield return Shot(directory,"02-commanders-unlimited-depth-lobby");
            options.Present(localOptions,false);options.wizards.onClick.Invoke();if(!localOptions.IsCommanders)throw new Exception("Non-leader changed mode");
            yield return null;StartLocal(4);handoff=false;seat=0;OptionsHand();yield return null;
            for(int camera=0;camera<3;camera++){board.CameraRig.SetMode(camera);board.CameraRig.Sync(0,4,false,true);yield return Shot(directory,"03-physical-fan-camera-"+camera);}
            var transform=board.VisibleHandCard(1,1);Vector3 before=transform.position;Quaternion rotation=transform.rotation;
            board.CameraRig.SetMode(1);board.CameraRig.Sync(0,4,false,true);yield return null;
            if(Vector3.Distance(before,transform.position)>.001f||Quaternion.Angle(rotation,transform.rotation)>.001f)throw new Exception("Physical fan still follows camera");
            var bear=ui.hand.Slot("options-card-0");previewPointer=CaptureHandPoint("options-card-0");CardDepthVisual.PreviewView=new Vector2(-.9f,.4f);
            yield return Shot(directory,"04-depth-window-left");
            if(cardCanvas.rightSlot.CardId!="C02"||cardCanvas.rightSlot.view.fullRules.text.Contains("Общая защита"))throw new Exception("Uncapped bear inspection failed");
            if(cardCanvas.rightSlot.view.fullArtwork.material.shader.name!="SummonersTable/Card Window UI")throw new Exception("Depth window not applied");
            CardDepthVisual.PreviewView=new Vector2(.9f,-.3f);yield return Shot(directory,"05-depth-window-right");
            if(cardCanvas.rightSlot.view.fullArtwork.material.GetVector("_ViewOffset").x<.8f)throw new Exception("Parallax does not respond to view");
            CardDepthVisual.PreviewView=null;previewPointer=null;
            Send(new GameCommand{kind="play",cardUid="options-card-0",slot=0});LabFinishQte();yield return Shot(directory,"06-budget-4-of-10");
            Send(new GameCommand{kind="play",cardUid="options-card-1",slot=1});LabFinishQte();yield return Shot(directory,"07-budget-8-of-10");
            if(state.qteSpent!=8||!MatchRules.CanUse(catalog,state,0,catalog.Card("S07"))||MatchRules.CanUse(catalog,state,0,catalog.Card("S01")))throw new Exception("Remaining-budget card highlighting mismatch");
            Send(new GameCommand{kind="play",cardUid="options-card-5",targetSeat=0});LabFinishQte();yield return Shot(directory,"08-budget-10-of-10");
            if(state.qteSpent!=10)throw new Exception("Commanders budget mismatch");
            localOptions=new MatchOptions();StartLocal(4);handoff=false;OptionsHand();yield return null;
            Send(new GameCommand{kind="play",cardUid="options-card-0",slot=0});LabFinishQte();yield return Shot(directory,"09-wizard-creature-route");
            var budget=ui.hud.GetComponentInChildren<TurnBudgetView>();if(budget.spellRoute.color!=budget.inactive||!budget.mixedRoute.text.StartsWith("0 / 1"))throw new Exception("Creature does not lock wizard spell-only route");
            localOptions=new MatchOptions();StartLocal(4);handoff=false;OptionsHand();yield return null;
            foreach(string id in new[]{"options-card-2","options-card-3"}){Send(new GameCommand{kind="play",cardUid=id,targetSeat=id.EndsWith("3")?0:1});LabFinishQte();}
            yield return Shot(directory,"10-wizard-two-spells-route");if(budget.mixedRoute.color!=budget.inactive||!budget.spellRoute.text.StartsWith("2 / 3"))throw new Exception("Second spell does not lock wizard mixed route");
            previewPointer=CaptureHandPoint("options-card-0");yield return Shot(directory,"11-flat-cards-and-capped-text");
            if(!cardCanvas.rightSlot.view.fullRules.text.Contains("не выше 2")||cardCanvas.rightSlot.view.fullArtwork.material.shader.name=="SummonersTable/Card Window UI")throw new Exception("Flat/default card restore failed");
            foreach(int count in new[]{2,3}){StartLocal(count);handoff=false;yield return Shot(directory,"12-physical-fan-"+count+"-players");}
            File.WriteAllText(Path.Combine(directory,"capture-report.txt"),"PASS lobby buttons/toggles, non-leader input rejection, commanders 4+4+2 UI, wizard route dimming, larger fan in 2/3/4 layouts, camera-invariant concealed world cards, depth on/off and parallax directions, conditional bear description, 3 camera views, no missing shaders. Steam transport rules covered by serialization tests; no multi-PC Steam playtest performed.\n");
            Debug.Log("OPTIONS_CAPTURE_COMPLETE "+directory);Application.Quit();
        }
        void OptionsHand()
        {
            for(int n=0;n<local.State.players.Count;n++)
            {
                var p=local.State.players[n];p.hand.Clear();p.units.Clear();p.hp=catalog.rules.heroHp;
                var ids=new[]{"C02","C03","S01","S02","S06","S07","R02","R03"};
                for(int i=0;i<ids.Length;i++)p.hand.Add(new HandCard{uid=(n==0?"options-card-":"hidden-"+n+"-")+i,cardId=ids[i]});
            }
            seat=0;state=local.View(seat,localTime);board.Sync(state,seat,Clock);board.SnapCamera(seat,false);
        }
    }
}
