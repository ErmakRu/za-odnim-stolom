#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace SummonersTable
{
    public sealed partial class GameApp
    {
        public bool CampaignCaptureDone{get;private set;}
        int movieFrame;const int MovieFps=12;
        public void BeginCampaignCapture(){StartCoroutine(CampaignMovie());}
        void MovieFrame()
        {
            Update();Canvas.ForceUpdateCanvases();EditorCapture?.Invoke("../output/campaign/frames/frame-"+movieFrame.ToString("0000")+".png");movieFrame++;
        }
        IEnumerator HoldMovie(float seconds)
        {for(int i=0;i<Mathf.RoundToInt(seconds*MovieFps);i++){MovieFrame();yield return null;}}
        IEnumerator CampaignMovie()
        {
            captureMode=true;Time.captureFramerate=MovieFps;
            Debug.Log("CAMPAIGN_SCREEN "+Screen.width+"x"+Screen.height);
            UserSettings.TestPath=Path.GetFullPath("../tmp/campaign-record/user-settings.json");UserSettings.Load();
            CampaignProgress.TestPath=Path.GetFullPath("../tmp/campaign-record/progress.json");OpenCampaign();
            Directory.CreateDirectory("../output/campaign/frames");
            yield return HoldMovie(2);
            comic.newGame.onClick.Invoke();comic.Advance();yield return HoldMovie(5);
            EditorCapture?.Invoke("../output/campaign/prologue.png");
            comic.Advance();comic.Advance();yield return HoldMovie(4);
            // Use the player's visible Skip Prologue control; no story is removed from the JSON.
            comic.skip.onClick.Invoke();comic.Advance();yield return HoldMovie(5);
            EditorCapture?.Invoke("../output/campaign/before-battle.png");
            int lines=0;
            while(page=="campaign")
            {
                comic.Advance();if(page!="campaign")break;
                comic.Advance();yield return HoldMovie(4);
                if(++lines>30)throw new Exception("Campaign comic did not reach the battle");
            }
            if(!campaignMatch||local==null||state.players.Count!=2||seat!=0||state.players[1].hand.Count!=0)throw new Exception("Campaign Play transition/privacy");
            EditorCapture?.Invoke("../output/campaign/battle-start.png");
            var pilot=new BotBrain(local.Catalog,ConfigBundle.Clone(ConfigRuntime.Current.bots).Profile(local.State.options.mode),false,new System.Random(410));
            double nextAction=localTime+2;bool qteShot=false,combatShot=false;int commands=0;
            for(int f=0;f<MovieFps*55;f++)
            {
                localTime+=1.0/MovieFps;local.Tick(localTime);localBotDirector.Tick(local,localTime);
                state=local.View(0,localTime);
                if(localTime>=nextAction)
                {
                    var command=pilot.Decide(state,0);nextAction=localTime+(state.qte?.owner==0?.35:1.25);
                    if(command!=null){Send(command);if(error!="")throw new Exception("Campaign movie command: "+error);commands++;}
                }
                MovieFrame();
                if(!qteShot&&state.phase=="qte"&&state.qte?.owner==0){EditorCapture?.Invoke("../output/campaign/battle-qte.png");qteShot=true;}
                if(!combatShot&&state.turnNumber>=3){EditorCapture?.Invoke("../output/campaign/battle-table.png");combatShot=true;}
                yield return null;if(state.phase=="matchEnd"){yield return HoldMovie(3);break;}
            }
            if(!qteShot||commands==0||localBotDirector.Accepted==0||localBotDirector.Rejected!=0)throw new Exception("Campaign capture did not exercise a real human/bot duel");
            File.WriteAllText("../output/campaign/recording.json",$"{{\"frames\":{movieFrame},\"fps\":{MovieFps},\"seconds\":{movieFrame/(float)MovieFps},\"source\":\"Unity Editor Play; real comic controls and legal game commands\"}}");
            File.WriteAllText("../output/tests/campaign-play.txt",$"PASS Editor Play: hub/deck, new campaign, typewriter/next, skip intro, complete first pre-battle comic, real duel/QTE, hidden bot hand, {commands} legal player inputs, {localBotDirector.Accepted} accepted bot commands, zero rejected. {movieFrame} rendered frames.\n");
            CampaignCaptureDone=true;Time.captureFramerate=0;
        }
    }
}
#endif
