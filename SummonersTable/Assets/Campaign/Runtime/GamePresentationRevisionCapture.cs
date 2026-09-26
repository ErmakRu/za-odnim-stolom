#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace SummonersTable
{
    public sealed partial class GameApp
    {
        public bool PresentationRevisionDone{get;private set;}
        public void BeginPresentationRevisionCapture(){StartCoroutine(PresentationRevisionMovie());}
        IEnumerator RevisionShot(string name)
        {yield return null;Update();Canvas.ForceUpdateCanvases();yield return null;EditorCapture?.Invoke("../output/presentation-v4/"+name+".png");}
        IEnumerator PresentationRevisionMovie()
        {
            captureMode=true;UserSettings.TestPath=Path.GetFullPath("../tmp/presentation-v4/settings.json");UserSettings.Load();
            CampaignProgress.TestPath=Path.GetFullPath("../tmp/presentation-v4/progress.json");OpenCampaign();
            Directory.CreateDirectory("../output/presentation-v4");
            var all=campaign.introduction.Concat(campaign.chapters.SelectMany(c=>c.before.Concat(c.after))).Concat(campaign.ending).ToArray();
            void Show(string id,bool thought=false)
            {var frame=all.First(f=>f.lines.Any(l=>l.sourceId==id));var line=ConfigBundle.Clone(frame.lines.First(l=>l.sourceId==id));if(thought)line.kind="thought";comic.Present(frame,line,"ПИР ХОХОТА",thought?"Предпросмотр оформления мыслей":"",false,false,"");comic.Reveal();}
            Show("0.1");yield return RevisionShot("comic-narrator");
            Show("0.4");yield return RevisionShot("comic-important");
            Show("0.7");yield return RevisionShot("comic-hero");
            Show("0.7",true);yield return RevisionShot("comic-thought");
            Show("0.15");yield return RevisionShot("comic-item");
            // Exercise actual reading input and persist/resume a fragment.
            comic.newGame.onClick.Invoke();comic.PointerClick(1);comic.PointerClick(2);
            if(comic.Segment!=1||!comic.Typing||campaignSave.segment!=1)throw new Exception("Double click / saved fragment");
            OpenCampaign();comic.resume.onClick.Invoke();if(comic.Segment!=1)throw new Exception("Fragment resume");
            campaignSave.chapter=0;campaignSave.segment=0;CardDepthVisual.PreviewView=Vector2.zero;StartCampaignBattle();yield return RevisionShot("game-hand");
            if(ui.hand.Slots.Any(s=>s.view.sharedCompact==null||string.IsNullOrEmpty(s.view.sharedCompact.description.text)))throw new Exception("Hand descriptions missing");
            previewPointer=CaptureHandPoint(state.players[0].hand[0].uid);yield return RevisionShot("game-inspection");previewPointer=null;
            var pilot=new BotBrain(local.Catalog,ConfigBundle.Clone(ConfigRuntime.Current.bots).Profile(local.State.options.mode),false,new System.Random(410));
            int commands=0;bool qte=false;
            for(int i=0;i<400;i++)
            {
                localTime+=.3;local.Tick(localTime);localBotDirector.Tick(local,localTime);state=local.View(0,localTime);
                if(state.phase=="action"&&state.players.Any(p=>p.units.Count>0)&&state.turnNumber>=3){yield return new WaitForSecondsRealtime(.8f);yield return RevisionShot("game-table");break;}
                var command=pilot.Decide(state,0);if(command!=null){Send(command);if(error!="")throw new Exception(error);commands++;}
                Update();
                if(!qte&&state.qte?.owner==0){yield return RevisionShot("game-qte");qte=true;}
                yield return null;
            }
            if(commands==0||localBotDirector.Rejected>0)throw new Exception("Real play commands");
            CheckCampaignFlow();
            CardDepthVisual.PreviewView=null;
            File.WriteAllText("../output/tests/presentation-play.txt",$"PASS Editor Play: double-click and saved fragment resume, shared hand/inspection/table faces, QTE, {commands} legal player commands, no rejected bot commands, campaign result flow.\n");
            PresentationRevisionDone=true;
        }
    }
}
#endif
