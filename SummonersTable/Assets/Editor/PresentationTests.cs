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
                var board=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TableWorld.prefab").GetComponent<TableBoard>();
                foreach(var layout in board.layouts)Check(layout.slotAnchors.Length==5*layout.playerCount&&layout.heroes.Length==layout.playerCount,"editable seat banks");
                Directory.CreateDirectory("../output/tests");File.WriteAllText("../output/tests/presentation-tests.txt","PASS camera clamps in all 3 modes, mode persistence, JSON roundtrip and validation, opposite duel seats, 4 authored scenes, 2/3/4 seat layouts, Canvas slot dimensions.\n");
                Debug.Log("ALL_PRESENTATION_TESTS_PASSED");
            }
            finally{UnityEngine.Object.DestroyImmediate(obj);}
        }
    }
}
