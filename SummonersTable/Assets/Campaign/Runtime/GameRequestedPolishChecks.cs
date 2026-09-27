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
        public string RequestedChecksStatus{get;private set;}="not run";
        public void BeginRequestedPolishChecks(){if(RequestedChecksStatus=="running")return;StartCoroutine(RequestedPolishChecks());}
        IEnumerator RequestedPolishChecks()
        {
            RequestedChecksStatus="running";
            while(!IsReady||menuCanvas==null)yield return null;
            string oldPath=CampaignProgress.TestPath;bool oldCapture=captureMode;var checks=new List<string>();
            void Check(bool ok,string label){if(!ok)throw new Exception("Requested polish: "+label);checks.Add("PASS "+label);}
            CampaignProgress.TestPath=Path.GetFullPath("../tmp/requested-polish/regression-progress.json");captureMode=true;
            RequestedChecksStatus="running";
            try
            {
                Check(UnityEngine.SceneManagement.SceneManager.sceneCount==1,"one loaded entry scene");
                Check(Camera.allCameras.Any(c=>c.isActiveAndEnabled&&c.targetTexture==null),"startup has a rendering camera");
                FrontEndAction("play");SyncFrontEnd();Check(menuCanvas.playPage.activeSelf,"play menu");
                FrontEndAction("campaign-new");Check(CurrentComic[0].lines[0].sourceId=="0.1","new campaign starts at approved prologue");
                Check(!comic.title.gameObject.activeSelf&&!comic.progress.gameObject.activeSelf,"no duel/frame counters");
                var longSentence=new string('а',220)+".";Check(ComicText.Split(longSentence).Single()==longSentence,"long sentence remains whole");
                Check(ComicText.Split("Первая фраза. Вторая фраза.").Length==1,"short sentences share a page");
                var expected=new[]{"lizard","rat","deer","lion","badger","owl","rabbit","lion","owl","deer","dog","badger","owl","deer","dog","rat","lion"};
                Check(campaign.chapters.Select(c=>c.opponentHero).SequenceEqual(expected),"all 17 campaign models match roster");
                var completed=new HashSet<string>();int guard=0;
                while(!campaignMatch&&++guard<100)
                {
                    if(tutorialLesson!="")
                    {
                        string lesson=tutorialLesson;
                        if(lesson=="STEP_HP_BARS"||lesson=="STEP_DRAW_CARD"||lesson=="STEP_HAND_LIMIT_8")tutorialPanel.proceed.onClick.Invoke();
                        else if(lesson=="STEP_BOARD_SLOTS")
                        {
                            ChooseHand(state.players[0].hand[0],Vector2.zero);ApplyWorldTarget("slot",0,"",3);UpdateTutorial();
                            Check(local.State.cast!=null&&local.State.cast.slot==3,"player selects creature slot");
                        }
                        else if(lesson=="STEP_QTE_RITUAL"){LabFinishQte();UpdateTutorial();}
                        else if(lesson=="STEP_SPELL_CAST")
                        {ChooseHand(state.players[0].hand[0],Vector2.zero);ApplyWorldTarget("hero",1,"",-1);LabFinishQte();UpdateTutorial();}
                        else if(lesson=="STEP_COMBAT_PHASE")
                        {Send(new GameCommand{kind="end"});for(int i=0;i<30&&local.State.players[1].hp==30;i++){localTime+=.5;local.Tick(localTime);}state=local.View(0,localTime);UpdateTutorial();}
                        Check(tutorialComplete,"interactive "+lesson);completed.Add(lesson);
                        if(lesson=="STEP_HAND_LIMIT_8")Check(local.State.players[0].hand.Count==8&&local.State.log.Any(l=>l.Contains("сгорает")),"ninth card actually burns in engine");
                        tutorialPanel.proceed.onClick.Invoke();
                    }
                    else if(comic.hub.activeSelf){Check(CurrentComic[campaignSave.frame].lines[0].sourceId=="2.10","deck selection follows waking scene");comic.resume.onClick.Invoke();}
                    else CampaignNext();
                    yield return null;
                }
                Check(completed.Count==7,"all seven lessons occur before first battle");
                Check(campaignMatch&&campaignSave.chapter==0&&state.players[1].heroId=="lizard","first campaign battle is the baron");
                yield return null;
                Check(board.Actor(1).HeroId=="lizard","baron instantiated as lizard");
                string first=state.matchId;local.State.phase="matchEnd";local.State.winners.Clear();local.State.winners.Add(1);local.State.players[0].hp=0;state=local.View(0,0);UpdateAuthoredInterface();
                ChoosePostMatch("again");UpdateAuthoredInterface();
                Check(state.matchId!=first&&state.phase=="action"&&state.players[0].hp==30&&!ui.results.gameObject.activeSelf,"loss retry clears result and starts a fresh duel");
                Check(campaignSave.chapter==0,"loss retry preserves chapter");
                local.State.phase="matchEnd";local.State.winners.Clear();local.State.winners.Add(0);state=local.View(0,0);CampaignResults();ChoosePostMatch("again");
                Check(campaignSave.phase=="after"&&CurrentComic[0].lines[0].sourceId=="3.7","victory resumes after battle story");
                CampaignNext();Check(campaignSave.wallOfShame.Count==1,"wall of shame records victory");
                string saved=File.ReadAllText(CampaignProgress.TestPath);FrontEndAction("tutorial");Check(CurrentComic[0].lines[0].sourceId=="1.1","standalone tutorial starts in dream");Check(File.ReadAllText(CampaignProgress.TestPath)==saved,"standalone tutorial preserves campaign save");
                CloseCampaign();page="menu";menuSection="home";SyncFrontEnd();
                RequestedChecksStatus="PASS "+checks.Count;
            }
            finally
            {
                CampaignProgress.TestPath=oldPath;captureMode=oldCapture;
                Directory.CreateDirectory("Captures/RequestedPolish");File.WriteAllText("Captures/RequestedPolish/regression.txt",RequestedChecksStatus+"\n"+string.Join("\n",checks));
                if(RequestedChecksStatus=="running")RequestedChecksStatus="FAILED — see console and regression.txt";
            }
        }
    }
}
