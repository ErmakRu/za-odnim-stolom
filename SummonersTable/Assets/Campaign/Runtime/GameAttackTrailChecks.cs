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
        public string AttackTrailCheckStatus{get;private set;}="not run";
        public void BeginAttackTrailChecks(){if(AttackTrailCheckStatus!="not run")return;AttackTrailCheckStatus="running";StartCoroutine(CheckAttackTrail());}
        IEnumerator CheckAttackTrail()
        {
            while(!IsReady)yield return null;
            bool oldCapture=captureMode;captureMode=true;var report=new List<string>();
            void Check(bool ok,string label){if(!ok)throw new Exception(label);report.Add("PASS "+label);}
            try
            {
                StartLocal(2);handoff=false;yield return null;
                board.CameraRig.SetMode(0);board.CameraRig.Sync(0,2,false,true);
                Check(board.CameraRig.settings.attackEffect.name=="VFX_Trail_Dark","original Dark trail prefab assigned");
                var cameraData=board.ViewCamera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                Check(cameraData.requiresColorTexture&&cameraData.requiresDepthTexture,"camera provides opaque colour and depth");
                var water=ConfigRuntime.Assets.Get<Material>(ConfigRuntime.Current.ui.arrow.worldMaterial);
                var waterUI=ConfigRuntime.Assets.Get<Material>(ConfigRuntime.Current.ui.arrow.uiMaterial);
                Check(water.name=="M_VFX_URP_Trail_Water_02"&&waterUI.shader==water.shader,"Water 02 on world and UI arrows");
                Check(ConfigRuntime.Current.ui.arrow.width>=14,"readable thicker arrow width");
                local.State.players[0].units.Add(new UnitState{uid="trail-source",cardId="C02",slot=2,hp=4});
                state=local.View(0,localTime);yield return null;
                for(int target=0;target<2;target++)
                {
                    string id="trail-check-"+target;
                    var e=new CombatEvent{id=id,unitUid="trail-source",source=0,sourceSlot=2,targetSeat=1,targetSlot=target==0?2:-1,targetUnit="",startedAt=localTime};
                    local.State.combatEvents.Add(e);state=local.View(0,localTime);
                    GameObject trailObject=null;
                    for(int step=0;step<=8;step++)
                    {
                        localTime=e.startedAt+step*.05;state=local.View(0,localTime);board.Sync(state,0,localTime);
                        if(step==0){trailObject=board.GetComponentsInChildren<TrailRenderer>().First(t=>t.name=="Distortion").transform.parent.gameObject;Check(trailObject.GetComponentsInChildren<TrailRenderer>().Length==3,"three trail layers "+target);}
                        if(step==4)
                        {
                            Vector3 from=TableBoard.SlotPosition(0,2,2)+Vector3.up*.65f;
                            Vector3 to=(target==0?TableBoard.SlotPosition(1,2,2):TableBoard.HeroPosition(1,2))+Vector3.up*.3f;
                            Check(Vector3.Distance(trailObject.transform.position,(from+to)*.5f+Vector3.ProjectOnPlane(Vector3.up,to-from).normalized*Vector3.Distance(from,to)*.5f)<.01f,"arc midpoint "+target);
                            var card=board.GetComponentsInChildren<CardView>().First(v=>v.name=="Unit trail-source");
                            Check(Vector3.Distance(card.transform.position,TableBoard.SlotPosition(0,2,2))<.5f,"card only twitches "+target);
                            if(target==0)AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("SummonersTable.Editor.EditorFrameCapture")).First(t=>t!=null).GetMethod("Save").Invoke(null,new object[]{"Captures/RequestedPolish/attack-refraction.png"});
                        }
                        yield return new WaitForSeconds(.05f);
                    }
                    Check(trailObject!=null&&trailObject.GetComponentsInChildren<TrailRenderer>().All(t=>!t.emitting),"tail remains after arrival "+target);
                    yield return new WaitForSeconds(2.2f);
                    Check(trailObject==null,"tail cleaned up "+target);
                }
                board.ClearMatchVisuals();Check(!board.GetComponentsInChildren<TrailRenderer>().Any(t=>t.name=="Distortion"),"match cleanup");
                AttackTrailCheckStatus="PASS "+report.Count;
            }
            finally{captureMode=oldCapture;if(AttackTrailCheckStatus=="running")AttackTrailCheckStatus="FAILED";Directory.CreateDirectory("Captures/RequestedPolish");File.WriteAllText("Captures/RequestedPolish/attack-trail-regression.txt",AttackTrailCheckStatus+"\n"+string.Join("\n",report));}
        }
    }
}
#endif
