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
        ComicFrame[] CurrentComic=>standaloneTutorial?campaign.tutorial:campaignSave.phase=="intro"?campaign.introduction:campaignSave.phase=="ending"||campaignSave.phase=="done"?campaign.ending:campaignSave.phase=="after"?campaign.chapters[campaignSave.chapter].after:campaign.chapters[campaignSave.chapter].before;
        void InitializeCampaign()
        {
            comic=FindFirstObjectByType<CampaignComicView>(FindObjectsInactive.Include);
            if(comic==null)throw new InvalidOperationException("Place the CampaignComic Canvas in the scene.");
            comic.Hide();
            if(FindFirstObjectByType<CampaignEntry>()!=null)OpenCampaign();
        }
        bool HandleCampaignAction(string action)
        {
            if(action=="campaign"){OpenCampaign();return true;}
            if(action=="campaign-new"||action=="tutorial"){OpenCampaign();standaloneTutorial=action=="tutorial";campaignSave=new CampaignProgress{deck=campaign.defaultDeck,phase="intro",chapter=0,frame=0,line=0,segment=0};campaignActive=true;ShowCampaignLine();return true;}
            if(action=="campaign-continue"){OpenCampaign();comic.resumeAction();return true;}
            return false;
        }
        public void OpenCampaign()
        {
            try
            {
                standaloneTutorial=false;campaign=CampaignBook.Load(catalog);campaignSave=CampaignProgress.Read(campaign);
                if(catalog.Deck(campaignSave.deck)==null)campaignSave.deck=campaign.defaultDeck;
                comic.Bind(campaign);comic.nextAction=CampaignNext;comic.previousAction=CampaignPrevious;comic.skipAction=CampaignSkip;
                comic.segmentChanged=index=>{campaignSave.segment=index;if(!standaloneTutorial)campaignSave.Save();};
                comic.menuAction=()=>{campaignActive=false;campaignMatch=false;comic.Hide();ExitMatch();};
                comic.resumeAction=()=>{campaignActive=true;if(campaignSave.phase=="battle"){campaignSave.phase="before";campaignSave.frame=campaignSave.line=campaignSave.segment=0;}if(campaignSave.phase=="done"){campaignSave.phase="ending";campaignSave.frame=campaignSave.line=campaignSave.segment=0;}ShowCampaignLine();};
                comic.newAction=()=>{campaignSave=new CampaignProgress{deck=campaignSave.deck};campaignActive=true;campaignSave.Save();ShowCampaignLine();};
                comic.deckPrevAction=()=>CampaignDeck(-1);comic.deckNextAction=()=>CampaignDeck(1);
                campaignActive=false;comic.Hide();page="menu";menuSection="campaign";
            }catch(Exception e){error=e.Message;Debug.LogError(e);}
        }
        void ClearCampaignMatch()
        {
            steam.Leave();online=false;local=null;localBotDirector=null;state=null;handoff=false;postMatchLobby=false;historyOpen=false;interfaceMatch="";ui.results.Show(false);page="campaign";settingsOpen=false;modal="";quitConfirm=false;ClearSelection();
        }
        void CampaignHub(){ClearCampaignMatch();campaignActive=campaignMatch=false;comic.Hide();page="menu";menuSection="campaign";}
        void CampaignDeck(int direction)
        {
            int index=catalog.decks.FindIndex(d=>d.id==campaignSave.deck);campaignSave.deck=catalog.decks[(index+direction+catalog.decks.Count)%catalog.decks.Count].id;if(!standaloneTutorial)campaignSave.Save();comic.ShowHub(campaign,campaignSave,catalog);ConfigureDeckSelection();
        }
        void ShowCampaignLine()
        {
            campaignMatch=false;var frames=CurrentComic;
            if(frames.Length==0){CampaignSectionFinished();return;}
            campaignSave.frame=Mathf.Clamp(campaignSave.frame,0,frames.Length-1);var frame=frames[campaignSave.frame];campaignSave.line=Mathf.Clamp(campaignSave.line,0,frame.lines.Length-1);
            bool last=campaignSave.frame==frames.Length-1&&campaignSave.line==frame.lines.Length-1;
            string heading=campaignSave.phase=="intro"?"ПРОЛОГ · "+frame.title:campaignSave.phase=="ending"?"ЭПИЛОГ · ПИР ХОХОТА":$"ПОЕДИНОК {campaignSave.chapter+1} / {campaign.chapters.Length} · {campaign.chapters[campaignSave.chapter].title}";
            string finish=campaignSave.phase=="before"?"Начать поединок":campaignSave.phase=="ending"?"Завершить историю":"Продолжить путь";
            var shownLine=frame.lines[campaignSave.line];
            bool inDream=shownLine.sourceId!=null&&shownLine.sourceId.StartsWith("1.",StringComparison.Ordinal);
            if(inDream){BeginDreamScene();dreamNarration=true;tutorialLesson="";if(tutorialPanel!=null)tutorialPanel.gameObject.SetActive(false);ClearSelection();}
            else {EndDreamScene();ClearCampaignMatch();}
            if(shownLine.actionType=="WALL_OF_SHAME_WRITE"&&!standaloneTutorial)
            {
                campaignSave.wallOfShame??=new List<string>();
                string entry=shownLine.actionTarget+"|"+shownLine.actionParameter;
                if(!campaignSave.wallOfShame.Contains(entry))campaignSave.wallOfShame.Add(entry);
            }
            int savedSegment=campaignSave.segment;
            comic.Present(frame,frame.lines[campaignSave.line],heading,$"Кадр {campaignSave.frame+1}/{frames.Length} · Реплика {campaignSave.line+1}/{frame.lines.Length}",campaignSave.frame>0||campaignSave.line>0,last,finish);
            comic.background.gameObject.SetActive(!inDream);
            comic.UseSegment(savedSegment);
            comic.skip.gameObject.SetActive(campaignSave.phase!="intro"&&!standaloneTutorial);
            comic.skip.GetComponentInChildren<Text>().text=campaignSave.phase=="before"?"К поединку":campaignSave.phase=="intro"?"Пропустить пролог":"Пропустить сцену";
            if(!standaloneTutorial)campaignSave.Save();
        }
        public void CampaignNext()
        {
            var line=CurrentComic[campaignSave.frame].lines[campaignSave.line];
            if(line.actionType=="UI_TUTORIAL"){StartTutorialLesson(line.actionTarget);return;}
            if(line.actionType=="UI_SHOW_DECK_SELECT"){comic.ShowHub(campaign,campaignSave,catalog);ConfigureDeckSelection();return;}
            AdvanceCampaignLine();
        }
        void ConfigureDeckSelection()
        {
            comic.hubTitle.text="Выберите колоду";comic.hubStatus.text="Чоп разложил карты Владыки по трём колодам";
            comic.newGame.gameObject.SetActive(false);comic.resume.gameObject.SetActive(true);comic.resume.GetComponentInChildren<Text>().text="Выбрать";
            comic.resumeAction=AdvanceCampaignLine;
        }
        void AdvanceCampaignLine()
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
        public void CampaignSkip()
        {
            // Skip dialogue, but never skip a required lesson, deck choice or battle action.
            var frames=CurrentComic;campaignSave.segment=0;
            if(!string.IsNullOrEmpty(frames[campaignSave.frame].lines[campaignSave.line].actionType)){CampaignNext();return;}
            while(campaignSave.frame<frames.Length)
            {
                if(++campaignSave.line>=frames[campaignSave.frame].lines.Length){campaignSave.line=0;campaignSave.frame++;}
                if(campaignSave.frame>=frames.Length){CampaignSectionFinished();return;}
                if(!string.IsNullOrEmpty(frames[campaignSave.frame].lines[campaignSave.line].actionType)){ShowCampaignLine();return;}
            }
        }
        void CampaignSectionFinished()
        {
            if(standaloneTutorial){CloseCampaign();page="menu";menuSection="play";return;}
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
            var members=new List<LobbyMember>{new LobbyMember{id="campaign-jester",name="Шут",deckId=campaignSave.deck,heroId="deer",ready=true},new LobbyMember{id=chapter.id,name=chapter.opponentName,deckId=chapter.opponentDeck,heroId=chapter.opponentHero,ready=true,isBot=true}};
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
            ui.results.Text("result",CampaignWon?"ПОБЕДА ШУТА":"ЕЩЁ ОДНА ПОПЫТКА");
            ui.results.Text("scores",string.Join("\n\n",state.players.Select(p=>p.name+" — "+p.score+" оч.")));
            ui.results.Text("votes",(CampaignWon?"Продолжите историю или вернитесь к выбору колоды.":"Прогресс сохранён. Этот поединок можно повторить."));
            ButtonLabel("again",CampaignWon?"Продолжить историю":"Повторить поединок");ButtonLabel("deck","Кампания");ui.results.Enabled("again",true);return true;
        }
        void CampaignEscape(){if(page!="campaign"||comic==null)return;CloseCampaign();page="menu";}
        void CloseCampaign(){EndDreamScene();standaloneTutorial=false;tutorialLesson="";if(tutorialPanel!=null)tutorialPanel.gameObject.SetActive(false);campaignActive=campaignMatch=false;if(comic!=null)comic.Hide();}
    }
}
