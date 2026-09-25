#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
namespace SummonersTable
{
    public sealed partial class GameApp
    {
        public void CheckCampaignFlow()
        {
            void Check(bool ok,string why){if(!ok)throw new Exception("CAMPAIGN FLOW: "+why);}
            void FinishFixture(int winner)
            {
                // Engine combat is covered separately for all 17 chapters. Inject only
                // the terminal result here to exercise both UI branches deterministically.
                state=local.View(0,localTime);state.phase="matchEnd";state.winners.Clear();state.winners.Add(winner);PresentResults();
            }
            captureMode=true;
            string oldPath=CampaignProgress.TestPath;
            CampaignProgress.TestPath=Path.GetFullPath("../tmp/campaign-flow/progress.json");
            try
            {
                OpenCampaign();comic.newGame.onClick.Invoke();Check(campaignSave.phase=="intro","new story");
                comic.skip.onClick.Invoke();Check(campaignSave.phase=="before","intro to first comic");
                comic.skip.onClick.Invoke();Check(campaignMatch&&page=="game","comic to battle");
                FinishFixture(1);Check(campaignSave.phase=="battle","loss does not advance");
                var first=local;ChoosePostMatch("again");Check(local!=first&&campaignSave.chapter==0&&campaignMatch,"retry same fight");
                FinishFixture(0);Check(CampaignProgress.Read(campaign).phase=="after","victory persists before clicking");
                ChoosePostMatch("again");
                if(campaignSave.phase=="after")comic.skip.onClick.Invoke();
                Check(campaignSave.chapter==1&&campaignSave.phase=="before"&&!campaignMatch,"win advances through victory comic to next pre-battle comic");
                comic.skip.onClick.Invoke();FinishFixture(1);ChoosePostMatch("deck");Check(page=="campaign"&&comic.hub.activeSelf&&!campaignMatch,"deck returns to campaign hub");
                string deck=campaignSave.deck;comic.deckNext.onClick.Invoke();Check(campaignSave.deck!=deck,"deck changes");
                comic.resume.onClick.Invoke();Check(campaignSave.phase=="before","interrupted battle restarts with comic");
                comic.skip.onClick.Invoke();Check(state.players[0].deckId==campaignSave.deck,"selected deck enters battle");
                campaignSave.chapter=campaign.chapters.Length-1;StartCampaignBattle();FinishFixture(0);ChoosePostMatch("again");
                int skips=0;while(campaignSave.phase!="done"&&skips++<5)comic.skip.onClick.Invoke();
                Check(campaignSave.phase=="done"&&comic.hub.activeSelf,"last victory through ending to completed hub");
                comic.resume.onClick.Invoke();Check(campaignSave.phase=="ending","replay ending");
                comic.menu.onClick.Invoke();Check(page=="menu"&&!campaignActive&&local==null,"menu exit clears campaign");
                StartLocal(2);Check(!campaignActive&&!campaignMatch&&page=="game","normal match remains independent");ExitMatch();
                File.WriteAllText("../output/tests/campaign-flow.txt","PASS Editor Play result routes: loss/retry, win/checkpoint/next chapter, deck hub/change/resume, final victory/ending/completion/replay, menu exit and normal-game isolation. Terminal results injected; combat itself tested separately.\n");
            }
            finally{CampaignProgress.TestPath=oldPath;}
        }
    }
}
#endif
