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
        public string FramingCheckStatus{get;private set;}="not run";
        public void BeginFramingChecks(){if(FramingCheckStatus!="not run")return;FramingCheckStatus="running";StartCoroutine(CheckStartupFraming());}
        IEnumerator CheckStartupFraming()
        {
            while(!IsReady||ui==null)yield return null;
            var report=new List<string>();bool previousCapture=captureMode;captureMode=true;
            void Check(bool yes,string label){if(!yes)throw new Exception(label);report.Add("PASS "+label);}
            try
            {
                Check(!cardCanvas.qtePanel.gameObject.activeInHierarchy,"QTE hidden in fresh menu");
                for(int count=2;count<=4;count++)
                {
                    Array.Clear(localBots,0,localBots.Length);StartLocal(count);handoff=false;
                    local.State.players[0].hand.Clear();for(int i=0;i<8;i++)local.State.players[0].hand.Add(new HandCard{uid="framing-"+i,cardId="C02"});
                    state=local.View(0,0);yield return null;yield return null;
                    for(int mode=0;mode<=1;mode++)
                    {
                    board.CameraRig.SetMode(mode);board.CameraRig.Sync(0,count,false,true);yield return null;
                    Check(Mathf.Abs(board.ViewCamera.transform.eulerAngles.x-(mode==0?16.699f:53.130f))<.01f,"original camera pitch "+mode);
                    Check(!cardCanvas.qtePanel.gameObject.activeInHierarchy,"QTE hidden at battle start "+count);
                    var bounds=new List<Rect>();var corners=new Vector3[4];
                    foreach(var slot in ui.hand.Slots){var r=(RectTransform)slot.transform;r.GetWorldCorners(corners);var min=(Vector2)corners[0];var max=min;foreach(var p in corners){min=Vector2.Min(min,p);max=Vector2.Max(max,p);}bounds.Add(Rect.MinMaxRect(min.x,min.y,max.x,max.y));}
                    for(int i=0;i<5;i++)
                    {
                        var anchor=board.Seat(0).slots[i];var mesh=anchor.GetComponent<MeshFilter>().sharedMesh.bounds;
                        var points=new[]{new Vector3(mesh.min.x,mesh.max.y,mesh.min.z),new Vector3(mesh.min.x,mesh.max.y,mesh.max.z),new Vector3(mesh.max.x,mesh.max.y,mesh.min.z),new Vector3(mesh.max.x,mesh.max.y,mesh.max.z)};
                        foreach(var localPoint in points){var p=board.ViewCamera.WorldToScreenPoint(anchor.TransformPoint(localPoint));Check(p.z>0&&p.x>0&&p.x<Screen.width&&p.y>0&&p.y<Screen.height&&!bounds.Any(r=>r.Contains(p)),"slot corner visible above eight-card hand "+count+"/"+i);}
                    }
                    Check(board.Actor(0).GetComponentsInChildren<Renderer>().All(r=>r.enabled),"local model remains rendered "+count);
                    }
                }
                StartLocal(2);handoff=false;local.State.players[0].hand.Clear();local.State.players[0].hand.Add(new HandCard{uid="qte-check",cardId="C02"});state=local.View(0,0);
                Send(new GameCommand{kind="play",cardUid="qte-check",slot=2});localTime=local.State.cast.revealUntil+.01;local.Tick(localTime);state=local.View(0,localTime);yield return null;
                Check(cardCanvas.qtePanel.gameObject.activeInHierarchy&&cardCanvas.GetComponent<Canvas>().enabled,"legitimate QTE appears");
                cardCanvas.Present(state,0,localTime,"",catalog,board,false);Check(!cardCanvas.qtePanel.gameObject.activeSelf,"QTE cleared while screen is hidden");
                cardCanvas.Present(state,0,localTime,"",catalog,board,true);Check(cardCanvas.qtePanel.gameObject.activeInHierarchy,"legitimate QTE restored after hide");
                LabFinishQte();yield return null;Check(!cardCanvas.qtePanel.gameObject.activeInHierarchy,"QTE closes after ritual");
                ExitMatch();yield return null;Check(!cardCanvas.qtePanel.gameObject.activeSelf,"QTE stays closed on return to menu");
                FramingCheckStatus="PASS "+report.Count;
            }
            finally{captureMode=previousCapture;if(FramingCheckStatus=="running")FramingCheckStatus="FAILED";Directory.CreateDirectory("Captures/RequestedPolish");File.WriteAllText("Captures/RequestedPolish/framing-regression.txt",FramingCheckStatus+"\n"+string.Join("\n",report));}
        }
    }
}
#endif
