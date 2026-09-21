using System;
using System.Collections.Generic;
using System.Linq;

namespace SummonersTable
{
    // The host is the only writer. UI and transports send validated commands.
    public sealed class GameEngine
    {
        public readonly Catalog Catalog;
        public readonly MatchState State=new MatchState();
        readonly Random random;
        readonly Dictionary<int,List<string>> decks=new Dictionary<int,List<string>>();
        readonly Dictionary<int,int> sequences=new Dictionary<int,int>();
        readonly Dictionary<int,int> killers=new Dictionary<int,int>();
        readonly Dictionary<int,Reaction> reactions=new Dictionary<int,Reaction>();
        readonly Queue<string> combat=new Queue<string>();
        readonly HashSet<int> roundAcknowledged=new HashSet<int>();
        int uid, successfulSpells;
        bool failDrawUsed;
        double now, actionRemaining;
        sealed class Reaction { public int player;public string effect, name;public int value;public TargetRef target; }

        public GameEngine(Catalog catalog,IList<LobbyMember> members,int seed,double time=0)
        {
            Catalog=catalog;catalog.Validate();random=new Random(seed);now=time;
            if(members.Count<2||members.Count>4)throw new ArgumentException("Need 2-4 players.");
            if(members.Select(m=>m.id).Distinct().Count()!=members.Count)throw new ArgumentException("Duplicate player ID.");
            State.matchId=Guid.NewGuid().ToString("N");State.version=catalog.version;
            for(int i=0;i<members.Count;i++)
            {
                var m=members[i];if(Catalog.Deck(m.deckId)==null)throw new ArgumentException("Unknown deck.");
                State.players.Add(new PlayerState{seat=i,id=m.id,name=m.name,deckId=m.deckId});
                sequences[i]=0;
            }
            State.round=1;StartRound();
        }
        public MatchState View(int seat,double time) { return State.View(seat,time); }
        string NextId() { return (++uid).ToString(); }
        PlayerState P(int seat) { return seat>=0&&seat<State.players.Count?State.players[seat]:null; }
        bool Live(int seat) { var p=P(seat);return p!=null&&p.alive&&p.connected&&p.hp>0; }
        CardDef Def(UnitState u) { return Catalog.Card(u.cardId); }
        UnitState Unit(int seat,string id) { return P(seat)?.units.Find(u=>u.uid==id); }
        bool Hero(TargetRef t) { return string.IsNullOrEmpty(t.unit); }
        bool Valid(TargetRef t) { return t!=null&&Live(t.seat)&&(Hero(t)||Unit(t.seat,t.unit)!=null); }
        bool Same(TargetRef a,TargetRef b) { return a.seat==b.seat&&(a.unit??"")==(b.unit??""); }
        int Sum(int seat,string effect,int cap=int.MaxValue,string except="")
        {
            if(!Live(seat))return 0;
            return Math.Min(cap,P(seat).units.Where(u=>u.hp>0&&u.uid!=except&&Def(u).effect==effect).Sum(u=>Def(u).value));
        }
        int EnemySum(int seat,string effect,int cap)
        { return Math.Min(cap,State.players.Where(p=>p.seat!=seat&&Live(p.seat)).Sum(p=>Sum(p.seat,effect))); }
        public int Attack(int owner,UnitState u) { return Def(u).attack+Sum(owner,"attackAura",2,u.uid); }
        void Log(string text)
        {
            State.lastEvent=text;State.log.Add(text);
            if(State.log.Count>60)State.log.RemoveAt(0);
            State.revision++;
        }
        void StartRound()
        {
            State.qte=null;State.pending=null;State.phase="action";State.winners.Clear();
            combat.Clear();reactions.Clear();roundAcknowledged.Clear();killers.Clear();
            foreach(var p in State.players)
            {
                p.hp=p.connected?Catalog.rules.heroHp:0;p.alive=p.connected;p.fatigue=0;
                p.hand.Clear();p.units.Clear();
                var pile=Catalog.Deck(p.deckId).entries.SelectMany(e=>Enumerable.Repeat(e.cardId,e.count)).ToList();
                for(int i=pile.Count-1;i>0;i--){int j=random.Next(i+1);var t=pile[i];pile[i]=pile[j];pile[j]=t;}
                decks[p.seat]=pile;p.deckCount=pile.Count;
                if(p.connected)Draw(p.seat,Catalog.rules.startingHand);
            }
            Log("РАУНД "+State.round+" / "+Catalog.rules.rounds);
            if(CheckOutcome())return;
            State.activeSeat=(State.round-1)%State.players.Count;
            while(!Live(State.activeSeat))State.activeSeat=(State.activeSeat+1)%State.players.Count;
            BeginTurn();
        }
        void BeginTurn()
        {
            State.turnNumber++;State.creaturePlayed=0;State.spellsPlayed=0;State.qteAttempted=false;
            State.riskBonus=0;successfulSpells=0;failDrawUsed=false;killers.Clear();
            State.phase="action";State.deadline=now+Catalog.rules.turnSeconds;actionRemaining=Catalog.rules.turnSeconds;
            foreach(var u in P(State.activeSeat).units){u.exhausted=false;u.plannedSeat=-1;u.plannedUnit="";}
            Log("Ход "+State.turnNumber+": "+P(State.activeSeat).name);
            Draw(State.activeSeat,1);
            if(Live(State.activeSeat))
            {
                Heal(State.activeSeat,Sum(State.activeSeat,"turnHeal",4));
                Draw(State.activeSeat,Sum(State.activeSeat,"turnDraw",2));
            }
            if(CheckOutcome())return;
            if(!Live(State.activeSeat))AdvanceTurn();
        }
        void Draw(int seat,int count)
        {
            var p=P(seat);
            for(int i=0;i<count&&p.hp>0;i++)
            {
                if(decks[seat].Count==0)
                {
                    int amount=++p.fatigue;p.hp=Math.Max(0,p.hp-amount);Log(p.name+": усталость -"+amount+" HP");continue;
                }
                var id=decks[seat][0];decks[seat].RemoveAt(0);p.deckCount=decks[seat].Count;
                Give(seat,new HandCard{uid=NextId(),cardId=id},false);
            }
        }
        void Give(int seat,HandCard card,bool announce)
        {
            var p=P(seat);
            if(p.hand.Count>=Catalog.rules.handLimit)Log(p.name+": рука полна, "+Catalog.Card(card.cardId).name+" сгорает");
            else {p.hand.Add(card);if(announce)Log(p.name+" получает «"+Catalog.Card(card.cardId).name+"»");}
            p.handCount=p.hand.Count;
        }
        void Heal(int seat,int value)
        {
            if(value<=0||!Live(seat))return;var p=P(seat);int old=p.hp;
            p.hp=Math.Min(Catalog.rules.heroHp,p.hp+value);
            if(p.hp>old)Log(p.name+": +"+(p.hp-old)+" HP");
        }
        TargetRef RandomHero(int source)
        {
            var list=State.players.Where(p=>p.seat!=source&&Live(p.seat)).ToList();
            return list.Count==0?null:new TargetRef(list[random.Next(list.Count)].seat);
        }
        bool TargetAllowed(int source,CardDef card,TargetRef t)
        {
            if(card.target=="none"||card.target=="self")return true;
            if(!Valid(t))return false;
            switch(card.target)
            {
                case "enemy":return t.seat!=source;
                case "enemyHero":return Hero(t)&&t.seat!=source;
                case "enemyUnit":return !Hero(t)&&t.seat!=source;
                case "hero":return Hero(t);
                case "unit":return !Hero(t);
                default:return false;
            }
        }
        public CommandResult Submit(int seat,GameCommand cmd,double time)
        {
            now=time;State.serverTime=time;
            if(P(seat)==null||cmd==null)return CommandResult.No("Неизвестный игрок.");
            if(cmd.seq<=sequences[seat])return CommandResult.No("Повторная команда.");
            sequences[seat]=cmd.seq;
            Tick(time);
            if(cmd.kind=="leave"){Disconnect(seat,time);return CommandResult.Yes();}
            if(cmd.kind=="nextRound")
            {
                if(State.phase!="roundEnd"||!P(seat).connected)return CommandResult.No("Раунд ещё не завершён.");
                roundAcknowledged.Add(seat);State.revision++;
                if(State.players.Where(p=>p.connected).All(p=>roundAcknowledged.Contains(p.seat)))NextRound();
                return CommandResult.Yes();
            }
            if(!Live(seat))return CommandResult.No("Вы наблюдаете за раундом.");
            CommandResult result;
            switch(cmd.kind)
            {
                case "play":result=Play(seat,cmd);break;
                case "key":result=Key(seat,cmd);break;
                case "react":result=React(seat,cmd);break;
                case "pass":result=Pass(seat,cmd);break;
                case "target":result=Plan(seat,cmd);break;
                case "end":
                    if(State.phase!="action"||State.activeSeat!=seat)return CommandResult.No("Сейчас нельзя завершить ход.");
                    EndTurn();result=CommandResult.Yes();break;
                default:result=CommandResult.No("Неизвестное действие.");break;
            }
            if(result.ok)State.revision++;
            return result;
        }
        CommandResult Plan(int seat,GameCommand c)
        {
            if(State.phase!="action"||State.activeSeat!=seat)return CommandResult.No("Сейчас нельзя назначать цели.");
            var u=Unit(seat,c.unitUid);if(u==null||u.exhausted)return CommandResult.No("Существо не готово.");
            if(c.targetSeat==-1){u.plannedSeat=-1;u.plannedUnit="";return CommandResult.Yes();}
            var t=new TargetRef(c.targetSeat,c.targetUnit);
            if(!Valid(t)||t.seat==seat)return CommandResult.No("Нужна вражеская цель.");
            u.plannedSeat=t.seat;u.plannedUnit=t.unit;return CommandResult.Yes();
        }
        CommandResult Play(int seat,GameCommand cmd)
        {
            if(State.phase!="action"||State.activeSeat!=seat)return CommandResult.No("Сейчас не ваша фаза действий.");
            var p=P(seat);var hc=p.hand.Find(h=>h.uid==cmd.cardUid);
            if(hc==null)return CommandResult.No("Карты нет в вашей руке.");
            var card=Catalog.Card(hc.cardId);if(card.kind=="reaction")return CommandResult.No("Реакция ждёт подходящего события.");
            bool creature=card.kind=="creature";
            if(creature&&(State.creaturePlayed>0||State.spellsPlayed>1))return CommandResult.No("Лимит призыва на этот ход исчерпан.");
            if(!creature&&State.spellsPlayed>=(State.creaturePlayed>0?1:3))return CommandResult.No("Лимит заклинаний исчерпан.");
            if(creature&&(cmd.slot<0||cmd.slot>=Catalog.rules.boardSlots||p.units.Any(u=>u.slot==cmd.slot)))return CommandResult.No("Выберите свободную ячейку.");
            var target=new TargetRef(cmd.targetSeat,cmd.targetUnit);
            if(creature&&cmd.targetSeat<0)target=RandomHero(seat);
            if(!TargetAllowed(seat,card,target))return CommandResult.No("Выберите допустимую цель.");
            if(card.effect=="swap"&&(p.hand.Count<2||P(target.seat).hand.Count==0))return CommandResult.No("Для обмена у обоих должна оставаться карта.");
            actionRemaining=Math.Max(0,State.deadline-now);
            int bonus=creature?State.riskBonus:0;
            if(creature){State.creaturePlayed++;State.riskBonus=0;}else State.spellsPlayed++;
            p.hand.Remove(hc);p.handCount=p.hand.Count;State.qteAttempted=true;
            int length=card.qte+EnemySum(seat,"qteExtra",2)+(bonus>0?2:0);
            length=Math.Max(2,Math.Min(11,length));
            const string keys="ASDFGHJ";string sequence="";
            while(sequence.Length<length)
            {
                char k=keys[random.Next(keys.Length)];
                if(sequence.Length>=2&&sequence[sequence.Length-1]==k&&sequence[sequence.Length-2]==k)continue;
                sequence+=k;
            }
            double duration=Math.Max(6,10+(length-2)*3.3+Sum(seat,"timeBonus",4)-EnemySum(seat,"timeTax",4));
            State.qte=new QteState{id=NextId(),cardId=card.id,cardUid=hc.uid,owner=seat,
                recipient=RandomHero(seat).seat,targetSeat=target?.seat??seat,targetUnit=target?.unit??"",slot=cmd.slot,
                sequence=sequence,duration=duration,deadline=now+duration,openingBonus=bonus};
            State.phase="qte";State.deadline=State.qte.deadline;
            Log(p.name+" начинает ритуал «"+card.name+"»");return CommandResult.Yes();
        }
        CommandResult Key(int seat,GameCommand cmd)
        {
            var q=State.qte;
            if(State.phase!="qte"||q==null||q.owner!=seat||cmd.phaseId!=q.id)return CommandResult.No("Эта попытка QTE уже недоступна.");
            if(now>=q.deadline){FailQte();return CommandResult.No("Время QTE истекло.");}
            if(cmd.key==null||cmd.key.Length!=1||!"ASDFGHJ".Contains(cmd.key))return CommandResult.No("Недопустимая клавиша.");
            if(q.sequence[q.index]==cmd.key[0])q.index++;
            else if(q.forgiven==0&&Sum(seat,"forgive",1)>0){q.forgiven++;Log("Техподдержка простила промах.");}
            else {q.mistakes++;q.deadline-=2;State.deadline=q.deadline;}
            if(q.mistakes>=Catalog.rules.qteMistakes||now>=q.deadline)FailQte();
            else if(q.index==q.sequence.Length)CompleteQte();
            return CommandResult.Yes();
        }
        void FailQte()
        {
            var q=State.qte;if(q==null)return;State.qte=null;
            int recipient=Live(q.recipient)?q.recipient:(RandomHero(q.owner)?.seat??-1);
            Log("Срыв ритуала «"+Catalog.Card(q.cardId).name+"»!");
            if(recipient>=0)Give(recipient,new HandCard{uid=q.cardUid,cardId=q.cardId},true);
            if(!failDrawUsed){failDrawUsed=true;Draw(q.owner,Sum(q.owner,"failDraw",2));}
            if(CheckOutcome())return;
            if(Catalog.Card(q.cardId).kind=="creature"||!Live(q.owner))EndTurn();else ResumeAction();
        }
        void CompleteQte()
        {
            var q=State.qte;State.qte=null;var c=Catalog.Card(q.cardId);
            Log("Ритуал удался: "+c.name);
            var a=new PendingAction{id=NextId(),source=q.owner,cardId=q.cardId,slot=q.slot,unitUid=q.cardUid,
                kind=c.kind=="creature"?"opening":c.effect,continuation=c.kind=="creature"?"summonEnd":"spellContinue",
                summonHp=c.health,damage=c.kind=="creature"?c.attack+Sum(q.owner,"attackAura",2)+Sum(q.owner,"openingPower",2)+q.openingBonus:c.value,
                label=c.name};
            if(c.effect=="damage"||c.effect=="areaDamage")a.damage+=Sum(q.owner,"spellPower",2);
            if(c.effect=="areaDamage")a.targets=State.players.Where(p=>p.seat!=q.owner&&Live(p.seat)).Select(p=>new TargetRef(p.seat)).ToList();
            else if(c.target!="none"&&c.target!="self")a.targets.Add(new TargetRef(q.targetSeat,q.targetUnit));
            if(a.kind=="opening"&&(a.targets.Count==0||!Valid(a.targets[0])))
            {a.targets.Clear();var t=RandomHero(q.owner);if(t!=null)a.targets.Add(t);}
            OpenAction(a);
        }
        bool Damaging(PendingAction a) { return a.kind=="opening"||a.kind=="combat"||a.kind=="damage"||a.kind=="areaDamage"; }
        public bool CanReact(int seat,CardDef card,TargetRef target)
        {
            var a=State.pending;
            if(a==null||!Live(seat)||card==null||card.kind!="reaction"||target==null||!a.targets.Any(t=>Same(t,target)))return false;
            bool damage=Damaging(a);bool own=target.seat==seat;
            switch(card.effect)
            {
                case "reduce":case "reflect":return damage&&own&&a.source!=seat;
                case "rescue":return damage;
                case "deny":return own&&a.source!=seat&&Catalog.Card(a.cardId).kind=="spell"&&
                    (damage||a.kind=="stun"||a.kind=="swap"||a.kind=="bounce"||a.kind=="heal");
                default:return false;
            }
        }
        void OpenAction(PendingAction action)
        {
            State.pending=action;reactions.Clear();State.phase="reaction";State.deadline=now+Catalog.rules.reactionSeconds;
            foreach(var p in State.players)
                if(!Live(p.seat)||!p.hand.Any(h=>action.targets.Any(t=>CanReact(p.seat,Catalog.Card(h.cardId),t))))action.responded.Add(p.seat);
            Log("Объявлено: "+action.label);
            if(State.players.All(p=>action.responded.Contains(p.seat)))ResolveAction();
        }
        CommandResult React(int seat,GameCommand cmd)
        {
            var a=State.pending;
            if(State.phase!="reaction"||a==null||cmd.phaseId!=a.id||a.responded.Contains(seat))return CommandResult.No("Окно реакции закрыто.");
            var h=P(seat).hand.Find(c=>c.uid==cmd.cardUid);var t=new TargetRef(cmd.targetSeat,cmd.targetUnit);
            if(h==null||!CanReact(seat,Catalog.Card(h.cardId),t))return CommandResult.No("Эта реакция не подходит к выбранной цели.");
            var card=Catalog.Card(h.cardId);P(seat).hand.Remove(h);P(seat).handCount=P(seat).hand.Count;
            reactions[seat]=new Reaction{player=seat,effect=card.effect,name=card.name,value=card.value,target=t};a.responded.Add(seat);
            Log(P(seat).name+" подготовил реакцию");
            if(State.players.All(p=>a.responded.Contains(p.seat)))ResolveAction();return CommandResult.Yes();
        }
        CommandResult Pass(int seat,GameCommand cmd)
        {
            var a=State.pending;if(State.phase!="reaction"||a==null||cmd.phaseId!=a.id)return CommandResult.No("Нет окна реакции.");
            if(!a.responded.Contains(seat))a.responded.Add(seat);
            if(State.players.All(p=>a.responded.Contains(p.seat)))ResolveAction();return CommandResult.Yes();
        }
        bool Denied(TargetRef t) { return reactions.Values.Any(r=>r.effect=="deny"&&Same(r.target,t)); }
        int Reduction(TargetRef t) { return reactions.Values.Where(r=>(r.effect=="reduce"||r.effect=="rescue")&&Same(r.target,t)).Sum(r=>r.value); }
        void RawHeroDamage(int seat,int amount,int source)
        {
            if(!Live(seat)||amount<=0)return;
            var p=P(seat);p.hp=Math.Max(0,p.hp-amount);
            if(p.hp==0&&source>=0&&source!=seat)killers[seat]=source;
        }
        int Damage(TargetRef t,int amount,int source,bool attack)
        {
            if(!Valid(t)||Denied(t))return 0;
            int n=Math.Max(0,amount-Reduction(t));
            if(Hero(t))
            {
                n=Math.Max(0,n-Sum(t.seat,"guard",3));int thorns=Sum(t.seat,"thorns",2);
                RawHeroDamage(t.seat,n,source);
                if(n>0)
                {
                    Log(P(t.seat).name+": -"+n+" HP");
                    if(source!=t.seat)RawHeroDamage(source,thorns,t.seat);
                    if(attack)Heal(source,Sum(source,"lifesteal",2));
                }
            }
            else {var u=Unit(t.seat,t.unit);u.hp-=n;Log(Catalog.Card(u.cardId).name+": -"+n+" HP");}
            if(n>0)
                foreach(var r in reactions.Values.Where(r=>r.effect=="reflect"&&Same(r.target,t)))RawHeroDamage(source,r.value,r.player);
            return n;
        }
        void ResolveAction()
        {
            var a=State.pending;if(a==null)return;
            State.phase="resolving";
            foreach(var r in reactions.Values.OrderBy(r=>r.player))Log(P(r.player).name+": реакция «"+r.name+"»");
            var c=Catalog.Card(a.cardId);
            if(a.kind=="opening"||a.kind=="combat")
            {
                UnitState attacker=a.kind=="combat"?Unit(a.source,a.unitUid):null;
                if(a.kind=="opening"||attacker!=null)
                {
                    var t=a.targets.FirstOrDefault();
                    if(!Valid(t))t=RandomHero(a.source);
                    if(t!=null)
                    {
                        var defender=Hero(t)?null:Unit(t.seat,t.unit);
                        int retaliation=defender==null?0:Attack(t.seat,defender);
                        Damage(t,a.kind=="combat"?Attack(a.source,attacker):a.damage,a.source,true);
                        if(a.kind=="opening")a.summonHp-=retaliation;else attacker.hp-=retaliation;
                    }
                    if(attacker!=null)attacker.exhausted=true;
                    if(a.kind=="opening"&&a.summonHp>0&&Live(a.source)&&!P(a.source).units.Any(u=>u.slot==a.slot))
                    {
                        P(a.source).units.Add(new UnitState{uid=a.unitUid,cardId=a.cardId,slot=a.slot,hp=a.summonHp,exhausted=true});
                        Log(c.name+" выходит на стол");
                    }
                }
            }
            else if(a.kind=="damage"||a.kind=="areaDamage")
                foreach(var t in a.targets)Damage(t,a.damage,a.source,false);
            else if(a.kind=="heal") {var t=a.targets.FirstOrDefault();if(Valid(t)&&!Denied(t))Heal(t.seat,c.value);}
            else if(a.kind=="draw")Draw(a.source,c.value);
            else if(a.kind=="riskBoost")State.riskBonus=c.value;
            else if(a.kind=="stun")
            {
                var t=a.targets.FirstOrDefault();if(Valid(t)&&!Denied(t))Unit(t.seat,t.unit).skipAttacks=Math.Max(1,Unit(t.seat,t.unit).skipAttacks);
            }
            else if(a.kind=="bounce")
            {
                var t=a.targets.FirstOrDefault();if(Valid(t)&&!Denied(t))
                {var u=Unit(t.seat,t.unit);P(t.seat).units.Remove(u);Give(t.seat,new HandCard{uid=u.uid,cardId=u.cardId},true);}
            }
            else if(a.kind=="swap")
            {
                var t=a.targets.FirstOrDefault();var source=P(a.source);
                if(Valid(t)&&!Denied(t)&&source.hand.Count>0&&P(t.seat).hand.Count>0)
                {
                    var dest=P(t.seat);int i=random.Next(source.hand.Count),j=random.Next(dest.hand.Count);
                    var temp=source.hand[i];source.hand[i]=dest.hand[j];dest.hand[j]=temp;Log("Карты поменялись карманами.");
                }
            }
            if(a.continuation=="spellContinue")
            {
                successfulSpells++;
                if(successfulSpells==1&&Live(a.source))Draw(a.source,Sum(a.source,"spellDraw",2));
                int taxLeft=2;
                foreach(var owner in State.players.Where(p=>p.seat!=a.source&&Live(p.seat)))
                {
                    int tax=Math.Min(taxLeft,Sum(owner.seat,"spellTax"));taxLeft-=tax;
                    RawHeroDamage(a.source,tax,owner.seat);
                    if(taxLeft==0)break;
                }
            }
            foreach(var p in State.players)
            {
                foreach(var dead in p.units.Where(u=>u.hp<=0).ToList()){p.units.Remove(dead);Log(Catalog.Card(dead.cardId).name+" покидает стол");}
            }
            State.pending=null;reactions.Clear();
            if(CheckOutcome())return;
            if(a.continuation=="summonEnd")EndTurn();
            else if(a.continuation=="combatNext")NextAttack();
            else if(!Live(State.activeSeat))EndTurn();
            else ResumeAction();
        }
        void ResumeAction() { State.phase="action";State.deadline=now+Math.Max(1,actionRemaining);State.revision++; }
        void EndTurn()
        {
            State.qte=null;State.pending=null;State.phase="combat";combat.Clear();
            if(Live(State.activeSeat))
                foreach(var u in P(State.activeSeat).units.OrderBy(u=>u.slot).Where(u=>!u.exhausted))combat.Enqueue(u.uid);
            NextAttack();
        }
        void NextAttack()
        {
            if(CheckOutcome())return;
            while(combat.Count>0&&Live(State.activeSeat))
            {
                var u=Unit(State.activeSeat,combat.Dequeue());if(u==null||u.exhausted)continue;
                if(u.skipAttacks>0){u.skipAttacks--;u.exhausted=true;Log(Def(u).name+": технический перерыв");continue;}
                var t=new TargetRef(u.plannedSeat,u.plannedUnit);if(!Valid(t)||t.seat==State.activeSeat)t=RandomHero(State.activeSeat);
                if(t==null)break;
                var a=new PendingAction{id=NextId(),source=State.activeSeat,cardId=u.cardId,unitUid=u.uid,kind="combat",
                    continuation="combatNext",damage=Attack(State.activeSeat,u),label=Def(u).name+" атакует "+P(t.seat).name};
                a.targets.Add(t);OpenAction(a);return;
            }
            if(Live(State.activeSeat)&&!State.qteAttempted)Draw(State.activeSeat,1);
            if(!CheckOutcome())AdvanceTurn();
        }
        void AdvanceTurn()
        {
            for(int i=0;i<State.players.Count;i++)
            {
                State.activeSeat=(State.activeSeat+1)%State.players.Count;
                if(Live(State.activeSeat)){BeginTurn();return;}
            }
            CheckOutcome();
        }
        bool CheckOutcome()
        {
            if(State.phase=="roundEnd"||State.phase=="matchEnd")return true;
            var previouslyAlive=State.players.Where(p=>p.alive&&p.connected).Select(p=>p.seat).ToList();
            foreach(var p in State.players.Where(p=>p.alive&&(p.hp<=0||!p.connected)))
            {
                p.alive=false;p.units.Clear();
                if(killers.TryGetValue(p.seat,out int killer)&&P(killer)!=null)P(killer).score+=Catalog.rules.eliminationPoints;
                Log(p.name+" выбывает из раунда");
            }
            killers.Clear();
            var alive=State.players.Where(p=>Live(p.seat)).ToList();if(alive.Count>1)return false;
            State.qte=null;State.pending=null;combat.Clear();reactions.Clear();
            if(alive.Count==1)
            {
                alive[0].score+=Catalog.rules.roundWinPoints;State.result=alive[0].name+" выигрывает раунд (+3)";
            }
            else
            {
                foreach(int s in previouslyAlive)P(s).score++;
                State.result="Ничья в раунде: оставшимся участникам +1";
            }
            Log(State.result);
            if(State.round>=Catalog.rules.rounds||State.players.Count(p=>p.connected)<2)FinishMatch();
            else{State.phase="roundEnd";State.deadline=now+18;roundAcknowledged.Clear();}
            return true;
        }
        void FinishMatch()
        {
            State.phase="matchEnd";int max=State.players.Max(p=>p.score);
            State.winners=State.players.Where(p=>p.score==max).Select(p=>p.seat).ToList();
            State.result="Победа: "+string.Join(", ",State.winners.Select(s=>P(s).name))+" | "+max+" очков";Log(State.result);
        }
        void NextRound() { State.round++;StartRound(); }
        public void Tick(double time)
        {
            now=time;State.serverTime=time;
            if(State.phase=="qte"&&State.qte!=null&&now>=State.qte.deadline)FailQte();
            else if(State.phase=="reaction"&&now>=State.deadline)ResolveAction();
            else if(State.phase=="action"&&now>=State.deadline){Log("Время хода истекло");EndTurn();}
            else if(State.phase=="roundEnd"&&now>=State.deadline)NextRound();
        }
        public void Disconnect(int seat,double time)
        {
            now=time;var p=P(seat);if(p==null||!p.connected)return;
            p.connected=false;p.hp=0;Log(p.name+" покинул матч");
            if(State.phase=="matchEnd")return;
            if(State.phase=="roundEnd")
            {
                if(State.players.Count(x=>x.connected)<2)FinishMatch();return;
            }
            if(CheckOutcome())return;
            if(State.activeSeat==seat){State.qte=null;State.pending=null;combat.Clear();AdvanceTurn();}
            else if(State.pending!=null)
            {
                if(!State.pending.responded.Contains(seat))State.pending.responded.Add(seat);
                if(State.players.All(x=>State.pending.responded.Contains(x.seat)))ResolveAction();
            }
        }
    }
}
