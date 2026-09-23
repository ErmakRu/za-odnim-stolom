using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed class HistoryListView : MonoBehaviour
    {
        public ScrollRect scroll;public HistoryRowView rowPrefab;
        public float rowHeight=84,headerHeight=40,gap=5;
        public RectTransform window;public Button close;
        sealed class Row{public HistoryEntry entry;public bool header;public float y,height;}
        readonly List<Row> rows=new List<Row>();readonly List<HistoryRowView> pool=new List<HistoryRowView>();
        int latest=-1;string match="";
        public void ScrollTo(int id){var row=rows.Find(r=>!r.header&&r.entry.id==id);if(row!=null)scroll.content.anchoredPosition=new Vector2(0,Mathf.Min(row.y,Mathf.Max(0,scroll.content.rect.height-scroll.viewport.rect.height)));}
        public void Present(HistoryArchive archive,MatchState state,Catalog catalog,Font font)
        {
            int next=archive.Entries.LastOrDefault()?.id??0;
            if(match!=state.matchId){match=state.matchId;latest=-1;scroll.content.anchoredPosition=Vector2.zero;}
            if(next!=latest)
            {
                latest=next;rows.Clear();float y=0;
                foreach(var group in archive.Entries.GroupBy(e=>new{e.round,e.turn,e.turnSeat}).Reverse())
                {
                    rows.Add(new Row{entry=group.First(),header=true,y=y,height=headerHeight});y+=headerHeight;
                    foreach(var e in group.Where(e=>e.kind!="turn")){float h=Mathf.Max(rowHeight,e.targets.Count*rowHeight);rows.Add(new Row{entry=e,y=y,height=h});y+=h+gap;}
                }
                scroll.content.sizeDelta=new Vector2(scroll.content.sizeDelta.x,Mathf.Max(scroll.viewport.rect.height,y));
            }
            var visible=rows.Where(r=>r.y+r.height>=scroll.content.anchoredPosition.y&&r.y<=scroll.content.anchoredPosition.y+scroll.viewport.rect.height).ToList();
            while(pool.Count<visible.Count)pool.Add(Instantiate(rowPrefab,scroll.content,false));
            for(int i=0;i<pool.Count;i++)
            {
                var view=pool[i];view.gameObject.SetActive(i<visible.Count);if(i>=visible.Count)continue;var row=visible[i];var e=row.entry;
                var rt=(RectTransform)view.transform;rt.anchoredPosition=new Vector2(0,-row.y);rt.sizeDelta=new Vector2(scroll.content.rect.width,row.height);
                view.header.SetActive(row.header);view.body.SetActive(!row.header);view.entry=row.header?null:e;
                if(row.header){view.heading.text="РАУНД "+e.round+" · ХОД "+e.turn+" · "+Name(state,e.turnSeat);continue;}
                view.actor.text=e.actor<0?"Локация":Name(state,e.actor);view.actor.color=Tint(e.actor);view.sourceName.text=catalog.Card(e.cardId)?.name??e.detail;view.verb.text=e.detail;
                view.sourceCard.Show(catalog.Card(e.cardId),catalog,font);
                for(int n=0;n<view.targetGroups.Length;n++)
                {
                    view.targetGroups[n].SetActive(n<e.targets.Count);if(n>=e.targets.Count)continue;var t=e.targets[n];
                    view.targetOwners[n].text=Name(state,t.seat);view.targetOwners[n].color=Tint(t.seat);view.targetNames[n].text=catalog.Card(t.cardId)?.name??(t.seat<0?"Из центра стола":"Герой");
                    view.amounts[n].text=e.kind=="damage"||e.kind=="heal"?(e.kind=="heal"?"+":"−")+t.amount+" HP":"";
                    view.targetCards[n].Show(catalog.Card(t.cardId),catalog,font);
                }
            }
        }
        public string Inspect(Vector2 p){if(!RectTransformUtility.RectangleContainsScreenPoint(scroll.viewport,p))return "";foreach(var row in pool.Where(r=>r.gameObject.activeInHierarchy)){string id=row.Inspect(p);if(id!="")return id;}return "";}
        public bool Covers(Vector2 p){return gameObject.activeInHierarchy&&RectTransformUtility.RectangleContainsScreenPoint(window,p);}
        static string Name(MatchState s,int seat){return seat>=0&&seat<s.players.Count?s.players[seat].name:"Случайная цель";}
        static Color Tint(int seat){return seat>=0&&seat<4?TableBoard.SeatColors[seat]:Color.gray;}
    }
}
