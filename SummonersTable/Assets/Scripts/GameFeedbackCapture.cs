using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace SummonersTable
{
    public sealed partial class GameApp
    {
        void CheckHandBackCounts()
        {
            foreach(var p in state.players)if(board.VisibleHandCount(p.seat)!=(p.connected&&p.alive?p.handCount:0))throw new Exception("3D hand count mismatch");
        }
        IEnumerator CaptureFailedSummon(string directory)
        {
            foreach(var p in local.State.players){p.hand.Clear();p.units.Clear();}
            local.State.players[0].hand.Add(new HandCard{uid="failed-summon",cardId="C02"});
            seat=0;state=local.View(seat,localTime);Send(new GameCommand{kind="play",cardUid="failed-summon",slot=0});
            localTime=local.State.cast.revealUntil;local.Tick(localTime);state=local.View(seat,localTime);
            yield return Shot(directory,"13-summon-before-failure");CheckCardPanels("C02",true);CheckHandBackCounts();
            var q=local.State.qte;
            for(int i=0;i<3;i++)Send(new GameCommand{kind="key",phaseId=q.id,key=q.sequence[q.index]=='A'?"S":"A"});
            yield return Shot(directory,"14-failed-summon-cleared");CheckSummonPreviewCleared();CheckHandBackCounts();
            if(!local.State.history.Any(e=>e.kind=="failed"&&e.cardId=="C02"))throw new Exception("Failed QTE not logged");
            seat=1;state=local.View(seat,localTime);yield return Shot(directory,"15-observer-failed-summon-cleared");CheckSummonPreviewCleared();CheckHandBackCounts();
        }
        IEnumerator CaptureFeedbackDetails(string directory)
        {
            int safety=30;while(local.State.phase=="combat"&&safety-->0){localTime=local.State.deadline+.001;local.Tick(localTime);}
            if(safety<=0)throw new Exception("Combat fixture did not finish");
            state=local.View(seat,localTime);board.Sync(state,seat,Clock);UpdateJournal();
            CheckHandBackCounts();
            previewPointer=board.ViewCamera.WorldToScreenPoint(TableBoard.HeroPosition(1,4));
            yield return Shot(directory,"10a-hero-hover-info-and-hand-backs");previewPointer=null;
            historyOpen=true;yield return Shot(directory,"10b-history-dropdown");
            var row=journalRows.First(r=>!r.header&&r.entry.kind=="damage"&&r.entry.targets.Any(t=>t.cardId!=""));
            historyScroll.y=Mathf.Clamp(row.rect.y,0,Mathf.Max(0,journalHeight-HistoryViewport.height));
            var source=HistorySource(row).center+HistoryViewport.position-historyScroll;
            previewPointer=new Vector2(source.x*scale+offset.x,Screen.height-(source.y*scale+offset.y));
            yield return Shot(directory,"10c-history-source-hover");
            if(!cardCanvas.rightSlot.gameObject.activeSelf||cardCanvas.rightSlot.CardId!=row.entry.cardId)throw new Exception("Journal source inspection failed");
            int targetIndex=row.entry.targets.FindIndex(t=>t.cardId!="");
            var target=HistoryTargetCard(row,targetIndex).center+HistoryViewport.position-historyScroll;
            previewPointer=new Vector2(target.x*scale+offset.x,Screen.height-(target.y*scale+offset.y));
            yield return Shot(directory,"10d-history-target-hover");
            if(cardCanvas.rightSlot.CardId!=row.entry.targets[targetIndex].cardId)throw new Exception("Journal target inspection failed");
            if(!JournalCoversScreen(previewPointer.Value))throw new Exception("Journal does not block board input");
            historyOpen=false;previewPointer=null;
        }
    }
}
