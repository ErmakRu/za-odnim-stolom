using System;
using System.Collections.Generic;
using System.Linq;

namespace SummonersTable
{
    [Serializable] public sealed class CardDef
    {
        public string id, name, kind, faction, role, effect, target, rules, flavor, art;
        public int attack, health, qte, value;
    }
    [Serializable] public sealed class DeckEntry { public string cardId; public int count; }
    [Serializable] public sealed class DeckDef
    {
        public string id, name, subtitle, guide;
        public List<DeckEntry> entries = new List<DeckEntry>();
    }
    [Serializable] public sealed class ColorDef { public string id, name, hex; }
    [Serializable] public sealed class RulesDef
    {
        public int heroHp=30, deckSize=30, startingHand=5, handLimit=8, boardSlots=5,
            rounds=3, roundWinPoints=3, eliminationPoints=1, turnSeconds=45, revealSeconds=2, qteMistakes=3;
    }
    [Serializable] public sealed class Catalog
    {
        public string version, title;
        public RulesDef rules;
        public List<CardDef> cards;
        public List<DeckDef> decks;
        public List<ColorDef> typeColors, roleColors;
        public CardDef Card(string id) { return cards.Find(c=>c.id==id); }
        public DeckDef Deck(string id) { return decks.Find(d=>d.id==id); }
        public void Validate()
        {
            if (cards.Count!=30 || cards.Select(c=>c.id).Distinct().Count()!=cards.Count)
                throw new InvalidOperationException("Catalogue must contain 30 distinct cards.");
            foreach(var d in decks)
                if(d.entries.Sum(e=>e.count)!=rules.deckSize || d.entries.Any(e=>Card(e.cardId)==null || e.count<1))
                    throw new InvalidOperationException("Invalid deck "+d.id);
            foreach(var f in cards.Where(c=>c.kind=="creature").GroupBy(c=>c.faction))
                if(f.Select(c=>c.role).Distinct().Count()<3) throw new InvalidOperationException("Faction lacks roles: "+f.Key);
        }
    }
    [Serializable] public sealed class LobbyMember
    {
        public string id, name, deckId="noise";
        public string heroId="badger";public int outfit,palette;
        public bool ready;
    }
    [Serializable] public sealed class HandCard
    {
        public string uid, cardId;
        public HandCard Copy() { return new HandCard { uid=uid, cardId=cardId }; }
    }
    [Serializable] public sealed class UnitState
    {
        public string uid, cardId, plannedUnit="";
        public int slot, hp, plannedSeat=-1, skipAttacks;
        public bool exhausted, deploying, targetAssigned;
        public int openingBonus;
        public UnitState Copy() { return (UnitState)MemberwiseClone(); }
    }
    [Serializable] public sealed class PlayerState
    {
        public string id, name, deckId;
        public string heroId="badger";public int outfit,palette,cameraMode=1;
        public float lookYaw,lookPitch;
        public int seat, hp, score, handCount, deckCount, fatigue;
        public bool alive=true, connected=true;
        public List<HandCard> hand=new List<HandCard>();
        public List<UnitState> units=new List<UnitState>();
        public PlayerState View(bool own)
        {
            var p=(PlayerState)MemberwiseClone();
            p.handCount=hand.Count;
            p.hand=own?hand.Select(c=>c.Copy()).ToList():new List<HandCard>();
            p.units=units.Select(u=>u.Copy()).ToList();
            return p;
        }
    }
    [Serializable] public sealed class TargetRef
    {
        public int seat=-1;
        public string unit="";
        public TargetRef() {}
        public TargetRef(int seat,string unit="") { this.seat=seat;this.unit=unit??""; }
        public TargetRef Copy() { return new TargetRef(seat,unit); }
    }
    [Serializable] public sealed class QteState
    {
        public string id, cardId, cardUid, sequence, targetUnit;
        public int owner, recipient, targetSeat, slot, index, mistakes, forgiven, openingBonus;
        public bool randomTarget;
        public double deadline, duration;
        public QteState Copy() { return (QteState)MemberwiseClone(); }
    }
    [Serializable] public sealed class CastState
    {
        public string id,cardId,targetUnit="";
        public int owner,targetSeat=-1,slot=-1;
        public bool randomTarget;
        public double revealUntil, startedAt;
        public int qteLength, qteProgress, qteMistakes;
        public CastState Copy(){return (CastState)MemberwiseClone();}
    }
    [Serializable] public sealed class TableReaction
    {
        public string uid,cardId,castId,targetUnit="";
        public int owner,targetSeat;
        public double playedAt;
        public bool resolved,successful;
        public TableReaction Copy(){return (TableReaction)MemberwiseClone();}
    }
    [Serializable] public sealed class PendingAction
    {
        public string id, kind, cardId, unitUid, continuation, label;
        public int source, damage, slot, summonHp;
        public int plannedSeat=-1;
        public string plannedUnit="";
        public List<TargetRef> targets=new List<TargetRef>();
        public List<int> responded=new List<int>();
        public PendingAction Copy()
        {
            var p=(PendingAction)MemberwiseClone();
            p.targets=targets.Select(t=>t.Copy()).ToList();
            p.responded=new List<int>(responded);
            return p;
        }
    }
    [Serializable] public sealed class CombatEvent
    {
        public string id, unitUid, targetUnit;
        public int source, targetSeat, damage,sourceSlot=-1,targetSlot=-1;
        public double startedAt;
        public CombatEvent Copy(){return (CombatEvent)MemberwiseClone();}
    }
    [Serializable] public sealed class MatchState
    {
        public string version="0.3.0", phase="lobby", result="", lastEvent="", matchId;
        public int revision, round, turnNumber, activeSeat, creaturePlayed, spellsPlayed, riskBonus;
        public bool qteAttempted;
        public double serverTime, deadline;
        public List<PlayerState> players=new List<PlayerState>();
        public List<string> log=new List<string>();
        public QteState qte;
        public CastState cast;
        public List<TableReaction> tableReactions=new List<TableReaction>();
        public PendingAction pending;
        public List<int> winners=new List<int>();
        public List<CombatEvent> combatEvents=new List<CombatEvent>();
        public MatchState View(int seat,double now)
        {
            var v=(MatchState)MemberwiseClone();
            v.players=players.Select(p=>p.View(p.seat==seat)).ToList();
            v.log=new List<string>(log);v.winners=new List<int>(winners);
            // Letters and timer remain private; cast exposes only counts for the public orbs.
            v.qte=qte!=null&&phase=="qte"&&qte.owner==seat?qte.Copy():null;
            if(phase=="qte"&&(qte==null||qte.owner!=seat))v.deadline=0;
            v.cast=cast?.Copy();
            if(v.cast!=null&&qte!=null&&phase=="qte")
            {v.cast.qteLength=qte.sequence.Length;v.cast.qteProgress=qte.index;v.cast.qteMistakes=qte.mistakes;}
            v.combatEvents=combatEvents.Select(e=>e.Copy()).ToList();v.pending=pending==null?null:pending.Copy();
            v.tableReactions=tableReactions.Select(r=>r.Copy()).ToList();
            v.serverTime=now;
            return v;
        }
        public void RestoreViewPrivacy(int seat)
        {
            // Unity inline-class JSON may materialize empty objects for null fields.
            if(phase!="reveal"&&phase!="qte"){cast=null;pending=null;}
            if(phase!="qte"||cast==null||cast.owner!=seat)qte=null;
            if(phase=="qte"&&(cast==null||cast.owner!=seat))deadline=0;
            foreach(var p in players)if(p.seat!=seat)p.hand.Clear();
        }
    }
    [Serializable] public sealed class GameCommand
    {
        public int seq, slot=-1, targetSeat=-1;
        public float lookYaw,lookPitch;public int cameraMode=1;
        public string kind, cardUid="", unitUid="", targetUnit="", key="", phaseId="";
    }
    [Serializable] public sealed class WireMessage
    {
        public int protocol=4;
        public string kind, text, matchId;
        public GameCommand command;
        public MatchState state;
    }
    public sealed class CommandResult
    {
        public bool ok;
        public string message;
        public static CommandResult Yes() { return new CommandResult{ok=true,message=""}; }
        public static CommandResult No(string message) { return new CommandResult{ok=false,message=message}; }
    }
}
