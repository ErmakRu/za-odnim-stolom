using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace SummonersTable.Editor
{
    [InitializeOnLoad] public static class LayeredCardsPlayTest
    {
        const string Key="LayeredCards.PlayTest";static double start;static int frames;static bool finishing;static Quaternion initial;
        static LayeredCardsPlayTest(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
        public static void Run(){SessionState.SetBool(Key,true);EditorSceneManager.OpenScene(LayeredCardsAuthoring.Scene);EditorApplication.isPlaying=true;}
        static void Log(string message,string stack,LogType type){if(SessionState.GetBool(Key,false)&&(type==LogType.Exception||type==LogType.Error))Finish(1);}
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||finishing||!EditorApplication.isPlaying)return;
            try
            {
                if(start==0)start=EditorApplication.timeSinceStartup;
                var demo=UnityEngine.Object.FindFirstObjectByType<LayeredCardsDemo>();if(demo==null)throw new Exception("Layered demo missing in Play");
                if(UnityEngine.Object.FindFirstObjectByType<GameApp>()!=null)throw new Exception("Game bootstrap must not start in layered lab");
                foreach(var card in demo.cards)if(card.ArtMaterial==null||!card.ArtMaterial.shader.isSupported||string.IsNullOrEmpty(card.title.text))throw new Exception("JSON card did not load in Play");
                if(frames++==0)initial=demo.cards[0].transform.localRotation;
                if(EditorApplication.timeSinceStartup-start<2)return;
                if(Quaternion.Angle(initial,demo.cards[0].transform.localRotation)<1)throw new Exception("Auto rotation did not animate in Play");
                File.WriteAllText("../output/tests/layered-cards-play.txt","PASS Unity Editor Play: standalone scene, JSON initialization, two visible three-layer cards, live rotation, no game bootstrap, no exceptions.\n");
                Debug.Log("LAYERED_PLAY_OK");Finish(0);
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        static void Finish(int code){if(finishing)return;finishing=true;SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>EditorApplication.Exit(code);}
    }
}
