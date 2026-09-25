using System;
using System.Linq;
using System.Collections.Generic;
namespace SummonersTable
{
    // Pure decision code: receives only this bot's private view, never the host's full state.
    public sealed class BotBrain
    {
        public static readonly string[] Conditions={"ownQte","reactionWindow","myAction","roundEnded","matchEnded"};
        public static readonly string[] Actions={"qteKey","reactOrPass","playBest","assignAttack","endTurn","nextRound","rematch"};
        enum Status { Failure,Success,Running }
        readonly Catalog catalog;readonly BotProfile profile;readonly Random random;readonly bool autoRematch;
        readonly Dictionary<string,BotNode> nodes;
        MatchState state;int seat;GameCommand command;int aimedTurn=-1;readonly HashSet<string> aimed=new HashSet<string>();
        public string LastNode {get;private set;}="";
        BotWeights W=>profile.weights;
        PlayerState Own=>state.players[seat];
        public BotBrain(Catalog catalog,BotProfile profile,bool autoRematch,Random random)
        {this.catalog=catalog;this.profile=profile;this.autoRematch=autoRematch;this.random=random;nodes=profile.nodes.ToDictionary(n=>n.id);}
        public GameCommand Decide(MatchState privateView,int ownSeat)
        {
            state=privateView;seat=ownSeat;command=null;LastNode="";
            if(state==null||seat<0||seat>=state.players.Count||!Own.connected)return null;
            if(aimedTurn!=state.turnNumber){aimedTurn=state.turnNumber;aimed.Clear();}
            Tick(profile.root);return command;
        }
        Status Tick(string id)
        {
            var node=nodes[id];
            if(node.type=="selector") {foreach(var child in node.children){var result=Tick(child);if(result!=Status.Failure)return result;}return Status.Failure;}
            if(node.type=="sequence") {foreach(var child in node.children){var result=Tick(child);if(result!=Status.Success)return result;}return Status.Success;}
            if(node.type=="condition")return Condition(node.operation)?Status.Success:Status.Failure;
            command=Act(node.operation);if(command==null)return Status.Failure;LastNode=id;return Status.Running;
        }
        bool Condition(string operation)=>operation switch
        {
            "ownQte"=>state.phase=="qte"&&state.qte?.owner==seat,
            "reactionWindow"=>(state.phase=="qte"||state.phase=="reveal")&&state.cast!=null&&state.pending!=null&&state.pending.source!=seat&&!state.pending.responded.Contains(seat)&&Own.alive,
            "myAction"=>state.phase=="action"&&state.activeSeat==seat&&Own.alive,
            "roundEnded"=>state.phase=="roundEnd",
            "matchEnded"=>state.phase=="matchEnd"&&autoRematch&&Own.postMatchChoice!="again",
            _=>false
        };
        GameCommand Act(string operation)
        {
            switch(operation)
            {
                case "qteKey":
                    if(!Condition("ownQte"))return null;var q=state.qte;const string keys="ASDFGHJ";char key=q.sequence[q.index];
                    if(random.NextDouble()>profile.timing.qteCorrectChance)key=keys[(keys.IndexOf(key)+1+random.Next(keys.Length-1))%keys.Length];
                    return new GameCommand{kind="key",phaseId=q.id,key=key.ToString()};
                case "reactOrPass":return Condition("reactionWindow")?Reaction():null;
                case "playBest":return Condition("myAction")?Play():null;
                case "assignAttack":return Condition("myAction")?Attack():null;
                case "endTurn":return Condition("myAction")?new GameCommand{kind="end"}:null;
                case "nextRound":return Condition("roundEnded")?new GameCommand{kind="nextRound"}:null;
                case "rematch":return Condition("matchEnded")?new GameCommand{kind="postMatch",choice="again"}:null;
                default:return null;
            }
        }
        bool Enemy(PlayerState p)=>p.seat!=seat&&p.alive&&p.connected&&p.hp>0;
        float Effect(CardDef card)=>(profile.effects.FirstOrDefault(e=>e.effect==card.effect)?.weight??0)*card.value;
        float UnitValue(UnitState u){var c=catalog.Card(u.cardId);return c.attack*W.attack+u.hp*W.health+Effect(c)*W.permanentEffect;}
        int Aura(PlayerState p,string effect,string except="")
        {return MatchRules.Stack(state,p.units.Where(u=>u.uid!=except&&u.hp>0&&!u.deploying&&catalog.Card(u.cardId).effect==effect).Sum(u=>catalog.Card(u.cardId).value),catalog.rules.Limit(effect,int.MaxValue));}
        float DamageScore(TargetRef target,int amount)
        {
            var p=state.players[target.seat];bool hero=string.IsNullOrEmpty(target.unit);var unit=hero?null:p.units.Find(u=>u.uid==target.unit);
            if(!hero&&unit==null)return 0;int hp=hero?p.hp:unit.hp;int damage=Math.Max(0,amount-(hero?Aura(p,"guard"):0));
            float value=Math.Min(hp,damage)*W.damage;
            if(damage>=hp)value+=hero?W.killHero:W.killUnit+UnitValue(unit)*W.unitThreat;
            if(hero&&damage>0)value+=W.heroPressure*(1f-(float)hp/catalog.rules.heroHp)-Aura(p,"thorns")*W.thornsPenalty;
            return value;
        }
        IEnumerable<TargetRef> Targets(CardDef c)
        {
            if(c.target=="self"||c.target=="none"){yield return new TargetRef(seat);yield break;}
            foreach(var p in state.players.Where(p=>p.alive&&p.connected&&p.hp>0))
            {
                bool enemy=Enemy(p);
                if(c.target=="hero"||enemy&&(c.target=="enemy"||c.target=="enemyHero"))yield return new TargetRef(p.seat);
                if(enemy&&(c.target=="enemy"||c.target=="enemyUnit"||c.target=="unit"))
                    foreach(var unit in p.units.Where(u=>u.hp>0))yield return new TargetRef(p.seat,unit.uid);
            }
        }
        sealed class Choice { public HandCard hand;public CardDef card;public TargetRef target;public float value; }
        Choice Evaluate(HandCard hand,MatchState budget)
        {
            var c=catalog.Card(hand.cardId);if(!MatchRules.CanPlay(catalog,budget,seat,c))return null;
            float value=0;TargetRef target=new TargetRef(seat);
            if(c.kind=="creature")value=W.creatureBase+c.attack*W.attack+c.health*W.health+Effect(c)*W.permanentEffect;
            else
            {
                float best=float.NegativeInfinity;
                foreach(var t in Targets(c))
                {
                    var p=state.players[t.seat];var u=p.units.Find(x=>x.uid==t.unit);float score=0;
                    switch(c.effect)
                    {
                        case "damage":score=DamageScore(t,c.value+Aura(Own,"spellPower"));break;
                        case "heal":if(t.seat==seat)score=Math.Min(catalog.rules.heroHp-Own.hp,c.value)*W.heal*(1+W.healthUrgency*(1f-(float)Own.hp/catalog.rules.heroHp));break;
                        case "areaDamage":score=state.players.Where(Enemy).Sum(e=>DamageScore(new TargetRef(e.seat),c.value+Aura(Own,"spellPower")));break;
                        case "stun":if(u!=null&&!u.exhausted&&u.skipAttacks==0)score=UnitValue(u)*W.stun;break;
                        case "bounce":if(u!=null&&t.seat!=seat)score=UnitValue(u)*W.bounce;break;
                        case "draw":score=Math.Min(c.value,Math.Min(Own.deckCount,Math.Max(0,catalog.rules.handLimit-(Own.hand.Count-1))))*W.draw;break;
                        case "swap":if(p.handCount>0&&Own.hand.Count>=2)score=W.swap*p.handCount;break;
                        case "riskBoost":
                            var after=After(budget,hand,c);
                            if(Own.hand.Any(h=>h.uid!=hand.uid&&catalog.Card(h.cardId).kind=="creature"&&MatchRules.CanPlay(catalog,after,seat,catalog.Card(h.cardId))))score=c.value*W.risk;
                            break;
                    }
                    if(score>best){best=score;target=t;}
                }
                if(float.IsNegativeInfinity(best))return null;value=best*W.spellMultiplier;
            }
            if(value<=0)return null;
            value=value/(1+MatchRules.Cost(c)*W.budgetEfficiency)-MatchRules.Cost(c)*W.costPenalty;
            return new Choice{hand=hand,card=c,target=target,value=value};
        }
        MatchState After(MatchState source,HandCard hand,CardDef card)
        {
            var after=source.View(seat,source.serverTime);foreach(var player in after.players)if(player.seat!=seat)player.handCount=source.players[player.seat].handCount;after.qteSpent+=MatchRules.Cost(card);
            if(card.kind=="creature"){after.creaturePlayed++;after.players[seat].units.Add(new UnitState{uid="bot-preview-"+hand.uid,cardId=card.id,hp=card.health,slot=-1});}else after.spellsPlayed++;
            after.players[seat].hand.RemoveAll(h=>h.uid==hand.uid);return after;
        }
        GameCommand Play()
        {
            Choice best=null;float score=W.minPlayScore;
            foreach(var hand in Own.hand)
            {
                var choice=Evaluate(hand,state);if(choice==null||choice.value<W.minPlayScore)continue;
                var after=After(state,hand,choice.card);
                float followup=Own.hand.Where(h=>h.uid!=hand.uid).Select(h=>Evaluate(h,after)?.value??0).DefaultIfEmpty(0).Max();
                float candidate=choice.value+followup*W.followupWeight+(float)random.NextDouble()*W.randomJitter;
                if(candidate>score){score=candidate;best=choice;}
            }
            if(best==null)return null;
            bool creature=best.card.kind=="creature";
            int slot=creature?Enumerable.Range(0,catalog.rules.boardSlots).First(i=>Own.units.All(u=>u.slot!=i)):-1;
            var target=best.target;
            // Random-centre targeting is an explicit tunable choice, never aimed at friendly creatures.
            bool randomTarget=!creature&&best.card.target.StartsWith("enemy")&&random.NextDouble()<W.randomTargetChance;
            return new GameCommand{kind="play",cardUid=best.hand.uid,slot=slot,targetSeat=randomTarget?-1:target.seat,targetUnit=randomTarget?"":target.unit};
        }
        GameCommand Attack()
        {
            var unit=Own.units.FirstOrDefault(u=>!aimed.Contains(u.uid));if(unit==null)return null;
            var target=new TargetRef(-1);float best=float.NegativeInfinity;
            int damage=catalog.Card(unit.cardId).attack+Aura(Own,"attackAura",unit.uid)+unit.openingBonus;
            if(random.NextDouble()>=W.randomTargetChance)
                foreach(var p in state.players.Where(Enemy))
                    foreach(var t in new[]{new TargetRef(p.seat)}.Concat(p.units.Where(u=>u.hp>0).Select(u=>new TargetRef(p.seat,u.uid))))
                    {float score=DamageScore(t,damage);if(score>best){target=t;best=score;}}
            aimed.Add(unit.uid);return new GameCommand{kind="target",unitUid=unit.uid,targetSeat=target.seat,targetUnit=target.unit};
        }
        GameCommand Reaction()
        {
            var pending=state.pending;GameCommand best=null;float score=W.minReactionScore;
            foreach(var h in Own.hand)
            {
                var c=catalog.Card(h.cardId);
                foreach(var target in pending.targets.Where(t=>MatchRules.CanReact(catalog,state,seat,c,t)))
                {
                    float value=0;var source=catalog.Card(pending.cardId);
                    switch(c.effect)
                    {
                        case "copySummon":value=(source.attack*W.attack+source.health*W.health+Effect(source))*W.reactionCopy;break;
                        case "returnSummon":value=(source.attack*W.attack+source.health*W.health+Effect(source))*W.reactionReturn;break;
                        case "reduce":case "rescue":case "reflect":if(target.seat==seat)value=Math.Min(pending.damage,c.value)*W.reactionDefense;break;
                        case "deny":if(target.seat==seat&&pending.kind!="heal")value=(pending.damage>0?pending.damage:source.value)*W.reactionDefense;break;
                    }
                    if(value>score){score=value;best=new GameCommand{kind="react",cardUid=h.uid,targetSeat=target.seat,targetUnit=target.unit,phaseId=pending.id};}
                }
            }
            return best??new GameCommand{kind="pass",phaseId=pending.id};
        }
    }
}
