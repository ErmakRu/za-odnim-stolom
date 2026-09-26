using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace SummonersTable.Editor
{
    [InitializeOnLoad]
    public static class LayeredCardsGalleryPlayTest
    {
        const string Key="LayeredCards.GalleryPlayTest";
        static double started;
        static Quaternion initial;
        static bool finishing;
        static LayeredCardsGalleryPlayTest(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
        public static void BuildVerifyAndPlay()
        {
            LayeredCardsGalleryAuthoring.BuildAndVerify();
            LayeredCardsTests.Run();LayeredMotionTests.Run();Run();
        }
        public static void Run()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Run this smoke test in a separate batch Unity session.");
            SessionState.SetBool(Key,true);EditorSceneManager.OpenScene(LayeredCardsGalleryAuthoring.Scene);EditorApplication.isPlaying=true;
        }
        static void Log(string message,string stack,LogType type)
        {
            if(SessionState.GetBool(Key,false)&&(type==LogType.Exception||type==LogType.Error))Finish(1);
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||finishing||!EditorApplication.isPlaying)return;
            var gallery=Object.FindFirstObjectByType<LayeredCardsGallery>();if(gallery==null||gallery.FilteredCount==0)return;
            try
            {
                void Check(bool ok,string why){if(!ok)throw new Exception("Gallery Play: "+why);}
                if(started==0){started=EditorApplication.timeSinceStartup;initial=gallery.slots[0].transform.localRotation;}
                if(EditorApplication.timeSinceStartup-started<2)return;
                Check(Object.FindFirstObjectByType<GameApp>()==null,"no game bootstrap");
                Check(Quaternion.Angle(initial,gallery.slots[0].transform.localRotation)>1,"live auto rotation");
                gallery.rotation.onClick.Invoke();Check(!gallery.autoRotate,"pause button");
                gallery.reset.onClick.Invoke();Check(gallery.look==Vector2.zero,"reset button");
                var visited=new System.Collections.Generic.HashSet<string>();
                for(int page=0;page<10;page++)
                {
                    Check(gallery.Page==page,"next button binding");
                    foreach(var face in gallery.slots.Where(s=>s.gameObject.activeSelf))
                    {
                        Check(face.ArtMaterial!=null&&face.ArtMaterial.shader.isSupported,"loaded layered shader");visited.Add(face.cardName);
                    }
                    for(int mode=0;mode<4;mode++){gallery.modes[mode].onClick.Invoke();Check(gallery.Mode==mode,"inspection button");}
                    gallery.modes[0].onClick.Invoke();if(page<9)gallery.next.onClick.Invoke();
                }
                Check(visited.Count==30,"all 30 reachable in Play");
                gallery.filters[3].onClick.Invoke();Check(gallery.FilteredCount==4&&gallery.Page==0,"reaction filter");
                gallery.next.onClick.Invoke();Check(gallery.slots.Count(s=>s.gameObject.activeSelf)==1,"last page hides unused slots");
                gallery.previous.onClick.Invoke();Check(gallery.Page==0,"previous button");
                gallery.reload.onClick.Invoke();Check(gallery.FilteredCount==4,"reload preserves filter");
                gallery.filters[0].onClick.Invoke();gallery.rotation.onClick.Invoke();Check(gallery.autoRotate,"resume button");
                File.WriteAllText("../output/layered-cards-all/play-verification.txt","PASS Unity Play: all 30 cards via actual bound navigation buttons; 4 layer modes; last page; filters; reload; pause/reset/resume; live rotation; no GameApp and no exceptions.\n");
                Debug.Log("LAYERED_GALLERY_PLAY_PASS");Finish(0);
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        static void Finish(int code)
        {
            if(finishing)return;finishing=true;SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;
            EditorApplication.delayCall+=()=>EditorApplication.Exit(code);
        }
    }
}
