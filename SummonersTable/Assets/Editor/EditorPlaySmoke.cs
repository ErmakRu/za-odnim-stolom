using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace SummonersTable.Editor
{
    [InitializeOnLoad] public static class EditorPlaySmoke
    {
        const string Key="SummonersTable.ConfigPlaySmoke";static int stage;static double deadline;
        static EditorPlaySmoke(){EditorApplication.update+=Tick;Application.logMessageReceived+=OnLog;}
        public static void Run()
        {ConfigTests.Run();SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");EditorApplication.isPlaying=true;}
        static void OnLog(string condition,string stack,LogType type)
        {if(SessionState.GetBool(Key,false)&&type==LogType.Exception)Finish(1);}
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false))return;if(deadline==0)deadline=EditorApplication.timeSinceStartup+180;
            try
            {
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Editor Play timed out");
                if(!EditorApplication.isPlaying)return;var app=UnityEngine.Object.FindFirstObjectByType<GameApp>();if(app==null||!app.IsReady)return;
                if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"--editor-render-captures")>=0)GameApp.EditorCapture=EditorFrameCapture.Save;
                if(stage==0){if(!string.IsNullOrEmpty(ConfigRuntime.Error))throw new Exception(ConfigRuntime.Error);app.BeginEditorConfigSmoke();stage=1;return;}
                if(stage==1&&app.EditorConfigSmokeDone){app.BeginEditorRegressionSmoke();stage=2;return;}
                if(stage!=2||!app.EditorRegressionSmokeDone)return;
                Directory.CreateDirectory("../output/tests");
                File.WriteAllText("../output/tests/editor-play-v0.8.txt","PASS Unity Editor Play from MainMenu: Config Inspector validation, local 2/3/4 players, staged balance vs live world/audio reload, invalid JSON atomic rejection, all existing gameplay/UI regression assertions. Offscreen screenshots captured when --editor-render-captures is supplied. No Player build.\n");Finish(0);
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        static void Finish(int code){SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>EditorApplication.Exit(code);}
    }
}
