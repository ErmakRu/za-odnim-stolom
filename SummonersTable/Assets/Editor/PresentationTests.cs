using System;
using System.IO;
using UnityEngine;
using UnityEditor;
namespace SummonersTable.Editor
{
    public static class PresentationTests
    {
        static void Check(bool ok,string message){if(!ok)throw new Exception("PRESENTATION TEST: "+message);}
        public static void Run()
        {
            var settings=Resources.Load<PresentationSettings>("PresentationSettings");Check(settings!=null,"settings asset exists");
            var data=JsonUtility.FromJson<PresentationData>(settings.ToJson());data.Validate();
            Check(data.cameraModes.Length==3&&Mathf.Abs(data.hoverScale-1.1f)<.001f,"three modes and 10% hover");
            var obj=new GameObject("Camera test");var camera=obj.AddComponent<Camera>();var rig=obj.AddComponent<ManualTableCamera>();rig.settings=settings;rig.Initialize(camera);
            try
            {
                for(int mode=0;mode<3;mode++)
                {
                    rig.SetMode(mode);rig.RotateView(new Vector2(1000,1000));var m=data.cameraModes[mode];
                    Check(Mathf.Abs(rig.Look.x-m.yawLimit)<.001f&&Mathf.Abs(rig.Look.y-m.downLimit)<.001f,"positive look limits");
                    rig.RotateView(new Vector2(-2000,-2000));Check(Mathf.Abs(rig.Look.x+m.yawLimit)<.001f&&Mathf.Abs(rig.Look.y+m.upLimit)<.001f,"negative look limits");
                    rig.Sync(0,2,false,true);var before=camera.transform.position;rig.Sync(0,2,false,true);
                    Check(rig.Mode==mode&&Vector3.Distance(before,camera.transform.position)<.001f,"sync preserves manual mode");
                }
                rig.SetMode(20);Check(rig.Mode==2,"mode clamp");
                var invalid=new PresentationData();invalid.cameraModes[0].height=float.NaN;bool rejected=false;try{invalid.Validate();}catch(ArgumentException){rejected=true;}Check(rejected,"bad JSON numeric value rejected");
                Check(Vector3.Dot(TableBoard.Away(0,2),TableBoard.Away(1,2))<-.999f,"duel sits opposite");
                foreach(string scene in ProjectScaffolder.ScenePaths)Check(File.Exists(scene),"authored scene "+scene);
                var canvas=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CardTableCanvas.prefab").GetComponent<CardTableCanvas>();
                var left=((RectTransform)canvas.leftSlot.transform).sizeDelta;var right=((RectTransform)canvas.rightSlot.transform).sizeDelta;var center=((RectTransform)canvas.centerSlot.transform).sizeDelta;
                Check(left==right&&center.x>left.x&&center.y>left.y,"equal side slots, larger center");
                Check(canvas.reactionSlots!=null&&canvas.reactionSlots.Length==3,"three editable reaction overlays");
                foreach(var reaction in canvas.reactionSlots)Check(reaction!=null&&reaction!=canvas.leftSlot,"independent overlay slot");
                var board=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TableWorld.prefab").GetComponent<TableBoard>();
                foreach(var layout in board.layouts)Check(layout.slotAnchors.Length==5*layout.playerCount&&layout.heroes.Length==layout.playerCount,"editable seat banks");
                var heroes=Resources.Load<HeroLibrary>("HeroLibrary");Check(heroes!=null&&heroes.heroes.Length==8&&heroes.controller!=null,"eight imported heroes and animation controller");
                foreach(var hero in heroes.heroes)Check(hero.prefab!=null&&hero.prefab.GetComponentInChildren<Animator>().avatar.isHuman,"valid humanoid avatar "+hero.id);
                foreach(var layout in board.layouts)foreach(var actor in layout.avatars)Check(actor.GetComponent<HeroActor>()?.library==heroes,"all table seats use imported avatars");
                Check(settings.cardHover!=null&&settings.invalidAction!=null&&settings.attackEffect!=null&&settings.impactEffect!=null&&settings.qteFire!=null&&settings.qteSmoke!=null&&settings.motionTitle!=null,"stuff effects and audio assigned");
                var lobby=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LobbyCanvas.prefab").GetComponent<FrontEndCanvas>();
                Check(lobby.portraits.Length==4&&lobby.seatControls.Length==20&&lobby.appearancePanel!=null,"four editable lobby places and wardrobe");
                Check(TargetArrowGeometry.Build(Vector2.zero,new Vector2(10,0)).Length==0,"no arrow before pulling away");
                var shortArrow=TargetArrowGeometry.Build(Vector2.zero,new Vector2(80,0));
                var longArrow=TargetArrowGeometry.Build(Vector2.zero,new Vector2(420,0));
                Check(shortArrow.Length<longArrow.Length,"drag distance adds links rather than stretching all links");
                foreach(var arrow in new[]{shortArrow,longArrow,TargetArrowGeometry.Build(Vector2.zero,new Vector2(-200,-150))})
                    for(int i=0;i<arrow.Length-2;i+=2)Check(Mathf.Abs(Vector2.Distance(arrow[i],arrow[i+1])-14)<.2f,"arrow links keep their length");
                Directory.CreateDirectory("../output/tests");File.WriteAllText("../output/tests/presentation-tests.txt","PASS camera clamps in all 3 modes, mode persistence, JSON roundtrip and validation, opposite duel seats, 4 authored scenes, 2/3/4 seat layouts, Canvas slot dimensions. Eight imported humanoid rigs, controller, all table avatars, four lobby places, wardrobe controls, required VFX/audio assignments. Target arrow adds fixed-length links as drag distance grows; no arrow at click distance.\n");
                Debug.Log("ALL_PRESENTATION_TESTS_PASSED");
            }
            finally{UnityEngine.Object.DestroyImmediate(obj);}
        }
    }
}
