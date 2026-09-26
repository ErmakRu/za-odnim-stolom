using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace SummonersTable.Editor
{
    [InitializeOnLoad] public static class CampaignRecording
    {
        const string Key="Campaign.Recording";static bool started,finishing;static double deadline;
        static CampaignRecording(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
        public static void Run(){CampaignTests.Run();EditorApplication.ExecuteMenuItem("Window/General/Game");PlayModeWindow.SetCustomRenderingResolution(1600,1000,"Campaign recording");SessionState.SetBool(Key,true);EditorSceneManager.OpenScene(CampaignAuthoring.Scene);EditorApplication.isPlaying=true;}
        public static void RunFlow(){SessionState.SetBool("Campaign.Flow",true);Run();}
        public static void RunRevision(){PresentationRevisionTests.Run();SessionState.SetBool("Campaign.Revision",true);Run();}
        static void Log(string text,string stack,LogType type){if(SessionState.GetBool(Key,false)&&(type==LogType.Error||type==LogType.Exception))Finish(1);}
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||finishing)return;if(deadline==0)deadline=EditorApplication.timeSinceStartup+1200;
            try
            {
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Campaign recording timed out");
                if(!EditorApplication.isPlaying)return;var app=UnityEngine.Object.FindFirstObjectByType<GameApp>();if(app==null||!app.IsReady)return;
                if(SessionState.GetBool("Campaign.Flow",false)){SessionState.SetBool("Campaign.Flow",false);app.CheckCampaignFlow();Debug.Log("CAMPAIGN_FLOW_OK");Finish(0);return;}
                if(SessionState.GetBool("Campaign.Revision",false))
                {
                    if(!started){started=true;GameApp.EditorCapture=CampaignFrameCapture.Save;app.BeginPresentationRevisionCapture();return;}
                    if(app.PresentationRevisionDone){SessionState.SetBool("Campaign.Revision",false);Debug.Log("PRESENTATION_PLAY_OK");Finish(0);}return;
                }
                if(!started){started=true;GameApp.EditorCapture=CampaignFrameCapture.Save;app.BeginCampaignCapture();return;}
                if(app.CampaignCaptureDone){Debug.Log("CAMPAIGN_RECORDING_OK");Finish(0);}
            }catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        static void Finish(int code){if(finishing)return;finishing=true;SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>EditorApplication.Exit(code);}
    }
}
