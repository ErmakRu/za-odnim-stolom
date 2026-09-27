#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace SummonersTable
{
    public sealed partial class GameApp
    {
        public string DreamPresentationStatus{get;private set;}="not run";
        public void BeginDreamPresentationChecks(){DreamPresentationStatus="running";StartCoroutine(CheckDreamPresentation());}
        IEnumerator CheckDreamPresentation()
        {
            while(!IsReady)yield return null;
            var report=new List<string>();bool oldCapture=captureMode;string oldPath=CampaignProgress.TestPath;captureMode=true;CampaignProgress.TestPath=Path.GetFullPath("../tmp/dream-presentation/progress.json");
            void Check(bool ok,string label){if(!ok)throw new Exception("Dream presentation: "+label);report.Add("PASS "+label);}
            try
            {
                FrontEndAction("campaign-new");yield return new WaitForSeconds(.4f);
                for(int i=0;i<14;i++)AdvanceCampaignLine();yield return new WaitForSeconds(.5f);comic.Reveal();
                Check(dreamNarration&&dreamScene&&page=="game","dream dialogue retains battle table");
                int worldId=board.GetInstanceID();var backdrop=board.GetComponent<DreamTableBackdrop>();
                Check(backdrop.Showing&&backdrop.surroundings.All(x=>!x.activeSelf),"no tavern or floor during dream");
                Check(RenderSettings.skybox==backdrop.dreamSky&&board.ViewCamera.clearFlags==CameraClearFlags.Skybox,"story art replaces skybox");
                Check(!comic.background.gameObject.activeSelf&&comic.storyBackdropLayers.All(x=>!x.activeSelf),"no full screen backdrop over dream table");CaptureInterface("dream-dialogue");
                CampaignNext();CampaignNext();yield return new WaitForSeconds(.5f);
                Check(board.GetInstanceID()==worldId&&!dreamNarration&&tutorialLesson=="STEP_HP_BARS","same world transitions to interactive lesson");
                var own=ui.hud.GetComponent<LocalHeroHud>().healthFill.GetComponentInParent<HeroHealthBar>();var other=board.Seat(1).status.healthAnchor.GetComponent<HeroHealthBar>();
                Check(own!=null&&other!=null,"both players use shared health prefab");
                Check(own.fill.color==other.fill.color&&own.value.color==other.value.color&&own.value.font==other.value.font&&own.value.fontSize==other.value.fontSize,"identical health styling for both players");
                Check(board.CameraRig.Mode==0&&board.ViewCamera.transform.position.y<9,"lower ordinary camera is initial view");
                Check(board.Seat(0).body.Find("Character placement").localPosition.z==-2,"chair and actor moved slightly as one group");
                Check(board.Actor(0).GetComponentsInChildren<Renderer>().All(x=>x.enabled),"own actor not hidden");CaptureInterface("dream-health");
                tutorialLesson="";tutorialPanel.gameObject.SetActive(false);CloseCampaign();StartLocal(2);handoff=false;local.State.players[0].hand.Clear();
                for(int i=0;i<8;i++)local.State.players[0].hand.Add(new HandCard{uid="framing-"+i,cardId="C02"});state=local.View(0,localTime);yield return new WaitForSeconds(.5f);
                Check(!backdrop.Showing&&backdrop.surroundings.Where(x=>x.name.StartsWith("TavernEnvironment")).All(x=>x.activeSelf),"normal environment restored after dream");
                var hand=ui.hand.Slots.Where(c=>c.gameObject.activeInHierarchy).Select(c=>ScreenArea((RectTransform)c.transform,0)).ToArray();
                for(int i=0;i<5;i++)
                {
                    var slot=board.Seat(0).slots[i];var bounds=slot.GetComponent<MeshFilter>().sharedMesh.bounds;
                    for(int corner=0;corner<4;corner++)
                    {
                        var p=new Vector3((corner&1)==0?bounds.min.x:bounds.max.x,bounds.max.y,(corner&2)==0?bounds.min.z:bounds.max.z);
                        var point=board.ViewCamera.WorldToScreenPoint(slot.TransformPoint(p));
                        Check(point.z>0&&point.x>0&&point.x<Screen.width&&point.y>0&&point.y<Screen.height,"slot corner in ordinary camera "+i+"/"+corner);
                        Check(!hand.Any(rect=>rect.Contains(point)),"slot corner not covered by full hand "+i+"/"+corner);
                    }
                }
                CaptureInterface("ordinary-camera-full-hand");
                previewAim=true;ChooseHand(state.players[0].hand[0],new Vector2(750,850));previewAimEnd=new Vector2(1050,450);yield return new WaitForSeconds(.3f);CaptureInterface("wide-water-arrow");ClearSelection();previewAim=false;
                DreamPresentationStatus="PASS "+report.Count;
            }
            finally{captureMode=oldCapture;CampaignProgress.TestPath=oldPath;if(DreamPresentationStatus=="running")DreamPresentationStatus="FAILED";Directory.CreateDirectory("Captures/InterfaceRevision");File.WriteAllText("Captures/InterfaceRevision/dream-regression.txt",DreamPresentationStatus+"\n"+string.Join("\n",report));}
        }
    }
}
#endif
