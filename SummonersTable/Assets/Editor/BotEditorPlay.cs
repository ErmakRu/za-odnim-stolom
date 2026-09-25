using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace SummonersTable.Editor
{
    [InitializeOnLoad] public static class BotEditorPlay
    {
        const string Key="SummonersTable.BotEditorPlay";static bool started,finishing;static double deadline;
        static BotEditorPlay(){EditorApplication.update+=Tick;Application.logMessageReceived+=OnLog;}
        public static void Run()
        {BotTests.Run();RuleOptionsTests.Run();CoreTests.Run();SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");EditorApplication.isPlaying=true;}
        static void OnLog(string text,string stack,LogType type){if(SessionState.GetBool(Key,false)&&(type==LogType.Exception||type==LogType.Error))Finish(1);}
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||finishing)return;if(deadline==0)deadline=EditorApplication.timeSinceStartup+240;
            try
            {
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("BOT Editor Play timeout");
                if(!EditorApplication.isPlaying)return;var app=UnityEngine.Object.FindFirstObjectByType<GameApp>();if(app==null||!app.IsReady)return;
                if(!started){if(!string.IsNullOrEmpty(ConfigRuntime.Error))throw new Exception(ConfigRuntime.Error);started=true;GameApp.EditorCapture=EditorFrameCapture.Save;app.BeginEditorBotSmoke();return;}
                if(app.EditorBotSmokeDone){Debug.Log("BOT_EDITOR_PLAY_OK");Finish(0);}
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        static void Finish(int code){if(finishing)return;finishing=true;SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>EditorApplication.Exit(code);}
    }
}
