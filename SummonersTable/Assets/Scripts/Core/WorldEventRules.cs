using System;
using System.Collections.Generic;
using System.Linq;
namespace SummonersTable
{
    [Serializable]public sealed class ActiveWorldEvent {public int instanceId,eventId,expiresTurn;public ActiveWorldEvent Copy()=>(ActiveWorldEvent)MemberwiseClone();}
    [Serializable]public sealed class WorldEventNotice {public int serial,eventId,seat,slot=-1;public string unit="";public WorldEventNotice Copy()=>(WorldEventNotice)MemberwiseClone();}
    public sealed partial class GameEngine
    {
        int worldSerial;
        double MatchTurnSeconds=>Catalog.world?.timeChanges==true&&Catalog.world.playersTurnTime>0?Catalog.world.playersTurnTime:Catalog.rules.turnSeconds;
        double MatchRevealSeconds=>Catalog.world?.timeChanges==true&&Catalog.world.revealTime>0?Catalog.world.revealTime:Catalog.rules.revealSeconds;
        void InitializeLocation()
        {
            var world=Catalog.world;if(world==null)return;
            if(State.players.Count<world.playerRange.x||State.players.Count>world.playerRange.y)throw new ArgumentException("Location player range does not include this match");
            State.locationName=world.name;
            State.matchDeadline=world.timeChanges&&world.gameTimer>0?now+world.gameTimer:0;
        }
        void WorldTurn()
        {
            State.worldEvents.RemoveAll(e=>e.expiresTurn<=State.turnNumber);
            if(Catalog.world==null||Catalog.events==null)return;
            foreach(var rule in Catalog.world.events)
            {
                if(!rule.enabled||(State.turnNumber-1)%rule.everyTurns!=0||State.worldEvents.Any(e=>e.instanceId==rule.id)||random.NextDouble()*100>=rule.chance)continue;
                int duration=rule.randomDuration?random.Next(rule.durationRange.x,rule.durationRange.y+1):rule.duration;
                State.worldEvents.Add(new ActiveWorldEvent{instanceId=rule.id,eventId=rule.eventId,expiresTurn=State.turnNumber+duration});
                var def=Catalog.events.events.First(e=>e.id==rule.eventId);Log("Событие: "+def.name+" · "+duration+" хода");History("world",State.activeSeat,"",null,def.name+" — "+def.description);
            }
            foreach(var active in State.worldEvents.ToArray())
            {
                var def=Catalog.events.events.First(e=>e.id==active.eventId);
                if(def.mechanic==WorldEventMechanic.TurnHeroHeal){Heal(State.activeSeat,def.value);WorldNotice(def,State.activeSeat,null);}
                else if(def.mechanic==WorldEventMechanic.TurnHeroDamage){RawHeroDamage(State.activeSeat,def.value,-1,"",def.name);WorldNotice(def,State.activeSeat,null);}
            }
        }
        void WorldQteSymbol(int index)
        {
            if(Catalog.events==null)return;
            foreach(var active in State.worldEvents)
            {
                var def=Catalog.events.events.First(e=>e.id==active.eventId);
                if(def.mechanic!=WorldEventMechanic.QteUnitDamage||index%def.everySymbols!=0)continue;
                var candidates=State.players.Where(p=>Live(p.seat)).SelectMany(p=>p.units.Where(u=>u.hp>0).Select(u=>(player:p,unit:u))).ToArray();if(candidates.Length==0)continue;
                var chosen=candidates[random.Next(candidates.Length)];int damage=Math.Min(chosen.unit.hp,def.value);
                HistoryDamage(-1,"",new TargetRef(chosen.player.seat,chosen.unit.uid),damage,def.name);chosen.unit.hp-=damage;
                WorldNotice(def,chosen.player.seat,chosen.unit);
                if(chosen.unit.hp<=0)chosen.player.units.Remove(chosen.unit);NormalizePlans();
            }
        }
        void WorldNotice(WorldEventDef def,int seat,UnitState unit)
        {
            State.worldNotices.Add(new WorldEventNotice{serial=++worldSerial,eventId=def.id,seat=seat,slot=unit?.slot??-1,unit=unit?.uid??""});
            if(State.worldNotices.Count>32)State.worldNotices.RemoveAt(0);
        }
        bool LocationTimedOut()
        {
            if(State.matchDeadline<=0||now<State.matchDeadline||State.phase=="matchEnd")return false;
            State.qte=null;State.cast=null;State.pending=null;combat.Clear();reactions.Clear();
            var live=State.players.Where(p=>Live(p.seat)).ToArray();
            if(live.Length>0){int hp=live.Max(p=>p.hp);foreach(var p in live.Where(p=>p.hp==hp))p.score+=Catalog.rules.roundWinPoints;}
            FinishMatch();State.result="Время партии истекло. "+State.result;return true;
        }
    }
}
