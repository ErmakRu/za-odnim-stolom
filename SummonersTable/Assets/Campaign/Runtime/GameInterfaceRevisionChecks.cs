#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed partial class GameApp
    {
        public string InterfaceRevisionStatus{get;private set;}="not run";
        public void BeginInterfaceRevisionChecks(){if(InterfaceRevisionStatus!="not run")return;InterfaceRevisionStatus="running";StartCoroutine(CheckInterfaceRevision());}
        static void CaptureInterface(string name){var t=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("SummonersTable.Editor.EditorFrameCapture")).First(x=>x!=null);t.GetMethod("Save").Invoke(null,new object[]{"Captures/InterfaceRevision/"+name+".png"});}
        IEnumerator CheckInterfaceRevision()
        {
            float waitUntil=Time.realtimeSinceStartup+20;while(!IsReady&&Time.realtimeSinceStartup<waitUntil)yield return null;if(!IsReady){InterfaceRevisionStatus="FAILED startup timeout";yield break;}
            var checks=new List<string>();bool oldCapture=captureMode;string oldPath=CampaignProgress.TestPath;captureMode=true;CampaignProgress.TestPath=Path.GetFullPath("../tmp/interface-revision/progress.json");
            void Check(bool ok,string label){if(!ok)throw new Exception("Interface revision: "+label);checks.Add("PASS "+label);}
            try
            {
                FrontEndAction("home");yield return new WaitForSeconds(.4f);
                var play=menuCanvas.buttons[Array.IndexOf(menuCanvas.actions,"play")];Check(!play.gameObject.activeSelf,"no extra Play button before modes");CaptureInterface("home");
                FrontEndAction("play");yield return null;
                var cards=menuCanvas.playPage.GetComponentsInChildren<MenuCardIllustration>();Check(cards.Length==4,"four illustrated mode cards");Check(cards.All(c=>c.artwork.texture!=null&&c.artwork.material.shader.name=="SummonersTable/Card Window UI"),"existing illustrations and card shader");
                Check(cards.Select(c=>((RectTransform)c.transform).anchoredPosition.y).Distinct().Count()==1,"horizontal mode row");CaptureInterface("play-modes");
                FrontEndAction("campaign");yield return new WaitForSeconds(.4f);Check(menuCanvas.campaignPage.activeSelf&&menuCanvas.continueCampaign.gameObject.activeSelf,"campaign actions in one submenu");CaptureInterface("campaign-menu");
                Check(!menuCanvas.campaignChoice.illustration.gameObject.activeSelf,"choices replace only campaign illustration");
                menuCanvas.campaignChoice.dismiss();yield return new WaitForSeconds(.4f);Check(menuCanvas.campaignChoice.illustration.gameObject.activeSelf&&!menuCanvas.campaignPage.activeSelf,"dismiss restores illustration");
                FrontEndAction("campaign-new");yield return new WaitForSeconds(.4f);comic.Reveal();
                Check(campaignSave.phase=="intro"&&campaignSave.frame==0&&CurrentComic[0].lines[0].sourceId=="0.1","new game always starts at first prologue line");
                Check(comic.background.texture.name=="bg_throne_hall","throne hall backdrop in prologue");CaptureInterface("prologue-first-line");
                for(int i=0;i<14;i++)AdvanceCampaignLine();yield return new WaitForSeconds(.3f);comic.Reveal();
                Check(CurrentComic[campaignSave.frame].lines[0].sourceId=="1.1","all fourteen prologue lines precede childhood");Check(comic.background.texture.name=="bg_barn_sepia","barn backdrop in childhood");CaptureInterface("barn-first-line");
                CloseCampaign();
                FrontEndAction("local");yield return new WaitForSeconds(.4f);Check(lobbyCanvas.gameObject.activeSelf,"historical lobby opens");Check(lobbyCanvas.seatControls[4].GetComponent<TavernPanel>().color.r>.4f,"historical red customization button");CaptureInterface("lobby-restored-25-september");
                FrontEndAction("settings");yield return new WaitForSeconds(.4f);Check(ui.settings.gameObject.activeSelf,"settings opens from front end");Check(!ui.settings.transform.Find("ConfigControls").gameObject.activeSelf,"developer reload controls not in player settings");CaptureInterface("settings");settingsOpen=false;
                FrontEndAction("tutorial");CampaignNext();CampaignNext();yield return new WaitForSeconds(.4f);
                Check(tutorialLesson=="STEP_HP_BARS","tutorial starts at health lesson");Check(!tutorialPanel.instruction.text.Contains("Найд")&&tutorialPanel.proceed.GetComponentInChildren<Text>().text=="Понятно","direct health explanation");
                Check(tutorialPanel.shade.FocusAreas.Length==2,"both actual health bars spotlighted");Check(!tutorialPanel.shade.raycastTarget,"spotlight does not eat clicks");Check(tutorialPanel.GetComponent<Canvas>().sortingOrder>cardCanvas.GetComponent<Canvas>().sortingOrder,"tutorial darkening above game UI");CaptureInterface("tutorial-health");
                for(int mode=0;mode<2;mode++){board.CameraRig.SetMode(mode);board.CameraRig.Sync(0,2,false,true);yield return null;var rect=ScreenArea(board.Seat(1).status.healthNumber.rectTransform);Check(tutorialPanel.shade.FocusAreas[1].Contains(rect.center),"spotlight follows opponent HP in camera "+mode);}
                StartTutorialLesson("STEP_BOARD_SLOTS");yield return null;yield return null;ChooseHand(state.players[0].hand[0],Vector2.zero);yield return null;
                for(int i=0;i<5;i++){var point=board.ViewCamera.WorldToScreenPoint(board.Seat(0).slots[i].position);Check(tutorialPanel.shade.FocusAreas[0].Contains(point),"slot lit "+i);}CaptureInterface("tutorial-slots");ApplyWorldTarget("slot",0,"",3);Check(state.cast.slot==3,"slot selection still works under tutorial overlay");
                StartTutorialLesson("STEP_QTE_RITUAL");localTime=local.State.cast.revealUntil+.01;local.Tick(localTime);state=local.View(0,localTime);yield return null;yield return null;Check(tutorialPanel.shade.FocusAreas[0].Contains(ScreenArea(cardCanvas.qtePanel).center),"QTE spotlight tracks real panel");CaptureInterface("tutorial-qte");LabFinishQte();yield return null;Check(tutorialComplete,"QTE completes with overlay active");
                tutorialLesson="";tutorialPanel.gameObject.SetActive(false);CloseCampaign();StartLocal(2);handoff=false;yield return null;yield return null;
                Check(ui.battleVignette.gameObject.activeInHierarchy,"battle vignette visible");Check(ui.battleVignette.GetComponentInParent<Canvas>().sortingOrder<ui.hud.GetComponent<Canvas>().sortingOrder,"battle vignette below HUD");Check(!ui.battleVignette.raycastTarget,"battle vignette never blocks input");Check(ConfigRuntime.Current.ui.arrow.width>=14&&ConfigRuntime.Current.ui.arrow.worldWidth>=.09f,"thicker screen and world arrows");CaptureInterface("battle-vignette");
                for(int mode=0;mode<2;mode++){board.CameraRig.SetMode(mode);board.CameraRig.Sync(0,2,false,true);yield return null;Check(board.Actor(0).GetComponentsInChildren<Renderer>().All(r=>r.enabled),"local actor rendered in camera "+mode);}
                var enemyStatus=board.Seat(1).status;Check(enemyStatus.healthNumber.color==Color.black,"black health digits");
                Check(!enemyStatus.nameAnchor.GetComponentsInChildren<Graphic>().Any(g=>!(g is Text)&&g.enabled),"name has no background");
                var from=new Vector3(1,2,3);var to=new Vector3(7,5,10);var center=(from+to)*.5f;float radius=Vector3.Distance(from,to)*.5f;
                for(int i=0;i<=4;i++)Check(Mathf.Abs(Vector3.Distance(TableBoard.AttackArcPoint(from,to,i/4f),center)-radius)<.001f,"attack follows semicircle sample "+i);
                previewAim=true;selectedUnit="";ChooseHand(state.players[0].hand.First(h=>catalog.Card(h.cardId).kind=="creature"),new Vector2(750,850));previewAimEnd=new Vector2(1050,450);yield return new WaitForSeconds(.3f);
                var arrow=board.GetComponentsInChildren<WorldArrowView>().First(x=>x.name=="Unified selection arrow");
                Check(arrow.line.positionCount==65&&arrow.line.sharedMaterial.name.StartsWith("M_VFX_URP_Trail_Water_02"),"continuous Water shader arrow");CaptureInterface("continuous-water-arrow");ClearSelection();previewAim=false;
                ExitMatch();InterfaceRevisionStatus="PASS "+checks.Count;
            }
            finally{captureMode=oldCapture;CampaignProgress.TestPath=oldPath;if(InterfaceRevisionStatus=="running")InterfaceRevisionStatus="FAILED";Directory.CreateDirectory("Captures/InterfaceRevision");File.WriteAllText("Captures/InterfaceRevision/regression.txt",InterfaceRevisionStatus+"\n"+string.Join("\n",checks));}
        }
    }
}
#endif
