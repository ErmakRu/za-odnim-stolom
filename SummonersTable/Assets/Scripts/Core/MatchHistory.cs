using System;
using System.Collections.Generic;
using System.Linq;

namespace SummonersTable
{
    [Serializable] public sealed class HistoryTarget
    {
        public int seat=-1,amount,slot=-1;
        public bool prevented;
        public string cardId="",unit="";
        public HistoryTarget Copy(){return (HistoryTarget)MemberwiseClone();}
    }
    [Serializable] public sealed class HistoryEntry
    {
        public int id,round,turn,turnSeat,actor;
        public double at;
        public string kind="",cardId="",detail="";
        public List<HistoryTarget> targets=new List<HistoryTarget>();
        public HistoryEntry Copy(){var copy=(HistoryEntry)MemberwiseClone();copy.targets=targets.Select(t=>t.Copy()).ToList();return copy;}
    }
    // The wire sends a bounded recent window; each viewer retains entries already seen.
    // Only publicly played cards/board units enter this log, never private draws or QTE keys.
    public sealed class HistoryArchive
    {
        string match="";int lastId;
        public readonly List<HistoryEntry> Entries=new List<HistoryEntry>();
        public void Observe(MatchState state)
        {
            if(state==null||match!=state.matchId){Entries.Clear();lastId=0;match=state?.matchId??"";}
            if(state?.history==null)return;
            foreach(var e in state.history.Where(e=>e.id>lastId).OrderBy(e=>e.id))
            {Entries.Add(e.Copy());lastId=e.id;}
            if(Entries.Count>4096)Entries.RemoveRange(0,Entries.Count-4096);
        }
    }
    public sealed partial class GameEngine
    {
        int historyId;
        void History(string kind,int actor,string cardId="",IEnumerable<TargetRef> targets=null,string detail="",int amount=0)
        {
            var entry=new HistoryEntry{id=++historyId,at=now,round=State.round,turn=State.turnNumber,turnSeat=State.activeSeat,actor=actor,kind=kind,cardId=cardId,detail=detail};
            if(targets!=null)foreach(var t in targets)entry.targets.Add(new HistoryTarget{seat=t.seat,unit=t.unit,cardId=Unit(t.seat,t.unit)?.cardId??"",slot=Unit(t.seat,t.unit)?.slot??-1,amount=amount,prevented=kind=="effect"&&reactions.Values.Any(r=>r.effect=="deny"&&Same(r.target,t))});
            State.history.Add(entry);if(State.history.Count>80)State.history.RemoveAt(0);
        }
        void HistoryDamage(int actor,string cardId,TargetRef target,int amount,string detail="")
        {History("damage",actor,cardId,new[]{target},detail,amount);}
    }
}
