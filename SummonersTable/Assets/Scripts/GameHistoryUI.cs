using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SummonersTable
{
    public sealed partial class GameApp
    {
        readonly HistoryArchive journal=new HistoryArchive();
        bool historyOpen;
        Vector2 historyScroll;
        readonly Rect historyToggle=new Rect(24,120,264,42),historyWindow=new Rect(309,116,956,615);
        readonly List<JournalRow> journalRows=new List<JournalRow>();
        int journalLayoutId=-1;float journalHeight;string journalMatch="";
        sealed class JournalRow {public HistoryEntry entry;public bool header;public Rect rect;}
        Rect HistoryViewport => new Rect(historyWindow.x+12,historyWindow.y+57,historyWindow.width-24,historyWindow.height-69);
        string OwnerName(int who){return who>=0&&who<state.players.Count?state.players[who].name:"Случайная цель";}
        Vector2 LogicalPointer(Vector2 screen)
        {
            float s=Mathf.Min(Screen.width/W,Screen.height/H);
            return new Vector2((screen.x-(Screen.width-W*s)/2)/s,(Screen.height-screen.y-(Screen.height-H*s)/2)/s);
        }
        bool JournalCovers(Vector2 p){return historyToggle.Contains(p)||historyOpen&&historyWindow.Contains(p);}
        bool JournalCoversScreen(Vector2 screen){return JournalCovers(LogicalPointer(screen));}
        void UpdateJournal()
        {
            if(journalMatch!=(state?.matchId??"")){journalMatch=state?.matchId??"";journalLayoutId=-1;historyScroll=Vector2.zero;historyOpen=false;}
            journal.Observe(state);int newest=journal.Entries.LastOrDefault()?.id??0;
            if(newest==journalLayoutId)return;journalLayoutId=newest;journalRows.Clear();journalHeight=0;
            foreach(var turn in journal.Entries.GroupBy(e=>new{e.round,e.turn,e.turnSeat}).Reverse())
            {
                journalRows.Add(new JournalRow{entry=turn.First(),header=true,rect=new Rect(0,journalHeight,908,37)});journalHeight+=40;
                foreach(var entry in turn.Where(e=>e.kind!="turn"))
                {
                    float height=Mathf.Max(84,entry.targets.Count*82);
                    journalRows.Add(new JournalRow{entry=entry,rect=new Rect(0,journalHeight,908,height)});journalHeight+=height+5;
                }
            }
            if(!historyOpen)historyScroll=Vector2.zero;
        }
        Rect HistorySource(JournalRow row){return new Rect(row.rect.x+10,row.rect.y+7,49,70);}
        Rect HistoryTargetCard(JournalRow row,int i){return new Rect(522,row.rect.y+7+i*82,49,70);}
        string JournalInspection(Vector2 screen)
        {
            if(!historyOpen)return "";var pointer=LogicalPointer(screen);var viewport=HistoryViewport;if(!viewport.Contains(pointer))return "";
            pointer=pointer-viewport.position+historyScroll;
            foreach(var row in journalRows.Where(r=>!r.header&&r.rect.Contains(pointer)))
            {
                if(new Rect(8,row.rect.y+3,458,78).Contains(pointer))return row.entry.cardId;
                for(int i=0;i<row.entry.targets.Count;i++)
                    if(new Rect(512,row.rect.y+i*82,393,82).Contains(pointer))return row.entry.targets[i].cardId;
            }
            return "";
        }
        string HistoryVerb(HistoryEntry e)
        {
            switch(e.kind)
            {
                case "play":return e.detail;
                case "reaction":return "Реакция на карту";
                case "attack":return "Атакует";
                case "damage":return e.detail;
                case "heal":return "Лечение";
                case "failed":return "QTE сорван · передача карты";
                case "success":return "QTE пройден";
                default:return e.detail;
            }
        }
        void DrawJournal()
        {
            if(Button(historyToggle,historyOpen?"▲ СКРЫТЬ ЖУРНАЛ":"▼ ЖУРНАЛ ДЕЙСТВИЙ",muted)){historyOpen=!historyOpen;ClearSelection();}
            if(!historyOpen)return;
            Box(historyWindow,new Color(.027f,.055f,.07f,.99f));Frame(historyWindow,gold,2);
            Text(new Rect(historyWindow.x+15,historyWindow.y+12,860,32),"История стола · наведите на карту или её название",22,gold,true);
            var viewport=HistoryViewport;
            historyScroll=GUI.BeginScrollView(viewport,historyScroll,new Rect(0,0,908,Mathf.Max(viewport.height-1,journalHeight)),false,true);
            if(journalRows.Count==0)Text(new Rect(15,10,880,50),"Здесь появятся розыгрыши и атаки.",20,muted);
            foreach(var row in journalRows)
            {
                if(row.rect.yMax<historyScroll.y||row.rect.y>historyScroll.y+viewport.height)continue;
                var entry=row.entry;
                if(row.header){Box(row.rect,new Color(.11f,.20f,.22f));Text(new Rect(12,row.rect.y+6,875,28),"РАУНД "+entry.round+" · ХОД "+entry.turn+" · "+OwnerName(entry.turnSeat),19,gold,true);continue;}
                Box(row.rect,new Color(.055f,.10f,.13f));
                var card=catalog.Card(entry.cardId);
                if(card!=null){Image(card.id,HistorySource(row));Frame(HistorySource(row),TypeColor(card));}
                Text(new Rect(70,row.rect.y+5,390,24),OwnerName(entry.actor),17,entry.actor>=0?TableBoard.SeatColors[entry.actor]:muted,true);
                Text(new Rect(70,row.rect.y+29,390,27),card?.name??entry.detail,19,Color.white,true);
                Text(new Rect(70,row.rect.y+56,390,24),HistoryVerb(entry),16,muted);
                if(entry.targets.Count>0)Text(new Rect(476,row.rect.y+24,32,30),"→",25,gold);
                for(int i=0;i<entry.targets.Count;i++)
                {
                    var target=entry.targets[i];float y=row.rect.y+i*82;var targetCard=catalog.Card(target.cardId);
                    if(targetCard!=null){Image(targetCard.id,HistoryTargetCard(row,i));Frame(HistoryTargetCard(row,i),TypeColor(targetCard));}
                    Text(new Rect(583,y+8,305,25),OwnerName(target.seat),17,target.seat>=0?TableBoard.SeatColors[target.seat]:muted,true);
                    string victim=targetCard?.name??(target.seat<0?"Из центра стола":"Герой");
                    Text(new Rect(583,y+32,305,28),victim,18,Color.white,true);
                    if(entry.kind=="damage"||entry.kind=="heal")Text(new Rect(583,y+60,305,23),(entry.kind=="heal"?"+":"−")+target.amount+" HP",17,entry.kind=="heal"?teal:red,true);
                }
            }
            GUI.EndScrollView();
            if(historyWindow.Contains(Event.current.mousePosition)&&(Event.current.isMouse||Event.current.type==EventType.ScrollWheel))Event.current.Use();
        }
    }
}
