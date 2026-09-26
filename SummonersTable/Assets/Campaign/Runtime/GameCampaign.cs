using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SummonersTable
{
    public sealed partial class GameApp
    {
        CampaignBook campaign;CampaignProgress campaignSave;CampaignComicView comic;
        bool campaignActive,campaignMatch;
        ComicFrame[] CurrentComic=>campaignSave.phase=="intro"?campaign.introduction:campaignSave.phase=="ending"||campaignSave.phase=="done"?campaign.ending:campaignSave.phase=="after"?campaign.chapters[campaignSave.chapter].after:campaign.chapters[campaignSave.chapter].before;
        void InitializeCampaign()
        {
            var prefab=Resources.Load<CampaignComicView>("Campaign/CampaignComic");if(prefab==null)return;
            comic=Instantiate(prefab);comic.Hide();
            if(FindFirstObjectByType<CampaignEntry>()!=null)OpenCampaign();
        }
        bool HandleCampaignAction(string action){if(action!="campaign")return false;OpenCampaign();return true;}
        public void OpenCampaign()
        {
            try
            {
                campaign=CampaignBook.Load(catalog);campaignSave=CampaignProgress.Read(campaign);
                if(catalog.Deck(campaignSave.deck)==null)campaignSave.deck=campaign.defaultDeck;
                comic.Bind(campaign);comic.nextAction=CampaignNext;comic.previousAction=CampaignPrevious;comic.skipAction=CampaignSkip;
                comic.segmentChanged=index=>{campaignSave.segment=index;campaignSave.Save();};
                comic.menuAction=()=>{campaignActive=false;campaignMatch=false;comic.Hide();ExitMatch();};
                comic.resumeAction=()=>{campaignActive=true;if(campaignSave.phase=="battle"){campaignSave.phase="before";campaignSave.frame=campaignSave.line=campaignSave.segment=0;}if(campaignSave.phase=="done"){campaignSave.phase="ending";campaignSave.frame=campaignSave.line=campaignSave.segment=0;}ShowCampaignLine();};
                comic.newAction=()=>{campaignSave=new CampaignProgress{deck=campaignSave.deck};campaignActive=true;campaignSave.Save();ShowCampaignLine();};
                comic.deckPrevAction=()=>CampaignDeck(-1);comic.deckNextAction=()=>CampaignDeck(1);
                campaignActive=true;CampaignHub();
            }catch(Exception e){error=e.Message;Debug.LogError(e);}
        }
        void ClearCampaignMatch()
        {
            steam.Leave();online=false;local=null;localBotDirector=null;state=null;handoff=false;page="campaign";settingsOpen=false;modal="";quitConfirm=false;ClearSelection();
        }
        void CampaignHub(){ClearCampaignMatch();campaignMatch=false;comic.ShowHub(campaign,campaignSave,catalog);}
        void CampaignDeck(int direction)
        {
            int index=catalog.decks.FindIndex(d=>d.id==campaignSave.deck);campaignSave.deck=catalog.decks[(index+direction+catalog.decks.Count)%catalog.decks.Count].id;campaignSave.Save();comic.ShowHub(campaign,campaignSave,catalog);
        }
        void ShowCampaignLine()
        {
            ClearCampaignMatch();campaignMatch=false;var frames=CurrentComic;
            if(frames.Length==0){CampaignSectionFinished();return;}
            campaignSave.frame=Mathf.Clamp(campaignSave.frame,0,frames.Length-1);var frame=frames[campaignSave.frame];campaignSave.line=Mathf.Clamp(campaignSave.line,0,frame.lines.Length-1);
            bool last=campaignSave.frame==frames.Length-1&&campaignSave.line==frame.lines.Length-1;
            string heading=campaignSave.phase=="intro"?"ПРОЛОГ · "+frame.title:campaignSave.phase=="ending"?"ЭПИЛОГ · ПИР ХОХОТА":$"ПОЕДИНОК {campaignSave.chapter+1} / {campaign.chapters.Length} · {campaign.chapters[campaignSave.chapter].title}";
            string finish=campaignSave.phase=="before"?"Начать поединок":campaignSave.phase=="ending"?"Завершить историю":"Продолжить путь";
            int savedSegment=campaignSave.segment;
            comic.Present(frame,frame.lines[campaignSave.line],heading,$"Кадр {campaignSave.frame+1}/{frames.Length} · Реплика {campaignSave.line+1}/{frame.lines.Length}",campaignSave.frame>0||campaignSave.line>0,last,finish);
            comic.UseSegment(savedSegment);
            comic.skip.GetComponentInChildren<Text>().text=campaignSave.phase=="before"?"К поединку":campaignSave.phase=="intro"?"Пропустить пролог":"Пропустить сцену";
            campaignSave.Save();
        }
        public void CampaignNext()
        {
            campaignSave.segment=0;
            var frames=CurrentComic;if(++campaignSave.line>=frames[campaignSave.frame].lines.Length){campaignSave.line=0;campaignSave.frame++;}
            if(campaignSave.frame>=frames.Length)CampaignSectionFinished();else ShowCampaignLine();
        }
        void CampaignPrevious()
        {
            campaignSave.segment=0;
            if(campaignSave.line>0)campaignSave.line--;else if(campaignSave.frame>0){campaignSave.frame--;campaignSave.line=CurrentComic[campaignSave.frame].lines.Length-1;}else return;ShowCampaignLine();
        }
        public void CampaignSkip(){CampaignSectionFinished();}
        void CampaignSectionFinished()
        {
            campaignSave.segment=0;
            campaignSave.frame=campaignSave.line=campaignSave.segment=0;
            if(campaignSave.phase=="before"){StartCampaignBattle();return;}
            if(campaignSave.phase=="intro")campaignSave.phase="before";
            else if(campaignSave.phase=="after")
            {
                if(campaignSave.chapter+1<campaign.chapters.Length){campaignSave.chapter++;campaignSave.phase="before";}
                else campaignSave.phase="ending";
            }
            else{campaignSave.phase="done";campaignSave.Save();CampaignHub();return;}
            campaignSave.Save();ShowCampaignLine();
        }
        public void StartCampaignBattle()
        {
            var chapter=campaign.chapters[campaignSave.chapter];comic.Hide();ClearCampaignMatch();
            var members=new List<LobbyMember>{new LobbyMember{id="campaign-jester",name="Йорик",deckId=campaignSave.deck,heroId="deer",ready=true},new LobbyMember{id=chapter.id,name=chapter.opponentName,deckId=chapter.opponentDeck,heroId=chapter.opponentHero,ready=true,isBot=true}};
            var options=ConfigRuntime.Current.rules.defaults.Copy();options.mode=chapter.mode;options.cards3D=true;
            var battleCatalog=ConfigBundle.Clone(catalog);battleCatalog.rules.rounds=1;
            localTime=0;local=new GameEngine(battleCatalog,members,chapter.seed,0,options);seq=new int[4];seat=0;
            var bots=ConfigBundle.Clone(ConfigRuntime.Current.bots);bots.autoRematch=false;localBotDirector=new BotDirector(local,bots,chapter.seed);
            state=local.View(0,0);page="game";handoff=false;campaignActive=campaignMatch=true;seenTurn=-1;seenPhase="";campaignSave.phase="battle";campaignSave.Save();
        }
        bool CampaignWon=>state?.phase=="matchEnd"&&state.winners.Count==1&&state.winners.Contains(0);
        bool CampaignResultChoice(string choice)
        {
            if(!campaignActive||!campaignMatch)return false;
            if(choice=="menu"){campaignActive=campaignMatch=false;comic.Hide();ExitMatch();}
            else if(choice=="deck")CampaignHub();
            else if(CampaignWon){campaignSave.phase="after";campaignSave.frame=campaignSave.line=campaignSave.segment=0;campaignSave.Save();ShowCampaignLine();}
            else StartCampaignBattle();
            return true;
        }
        bool CampaignResults()
        {
            string ButtonLabel(string id,string text){ui.results.Get<Button>(id).GetComponentInChildren<Text>().text=text;return text;}
            if(!campaignActive||!campaignMatch){ButtonLabel("again","Сыграть ещё раз");ButtonLabel("deck","Выбрать другую колоду");return false;}
            if(CampaignWon&&campaignSave.phase=="battle"){campaignSave.phase="after";campaignSave.frame=campaignSave.line=campaignSave.segment=0;campaignSave.Save();}
            ui.results.Text("result",CampaignWon?"ПОБЕДА ЙОРИКА":"ЕЩЁ ОДНА ПОПЫТКА");
            ui.results.Text("scores",string.Join("\n\n",state.players.Select(p=>p.name+" — "+p.score+" оч.")));
            ui.results.Text("votes",$"Поединок {campaignSave.chapter+1} из {campaign.chapters.Length}\n"+(CampaignWon?"Продолжите историю или вернитесь к выбору колоды.":"Прогресс сохранён. Этот поединок можно повторить."));
            ButtonLabel("again",CampaignWon?"Продолжить историю":"Повторить поединок");ButtonLabel("deck","Кампания · выбрать колоду");ui.results.Enabled("again",true);return true;
        }
        void CampaignEscape(){if(page!="campaign"||comic==null)return;campaignActive=false;comic.Hide();page="menu";}
        void CloseCampaign(){campaignActive=campaignMatch=false;if(comic!=null)comic.Hide();}
    }
}
