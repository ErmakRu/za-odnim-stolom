using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SummonersTable.Editor
{
    public static class CoreTests
    {
        static Catalog catalog;static int asserts,serial;static readonly List<string> results=new List<string>();
        static void Check(bool condition,string message){asserts++;if(!condition)throw new Exception("RULE TEST FAILED: "+message);}
        static GameEngine New(int count=2,int seed=7)
        {return new GameEngine(catalog,Enumerable.Range(0,count).Select(i=>new LobbyMember{id="test"+i,name="Test "+i,deckId=catalog.decks[i%3].id}).ToList(),seed);}
        static void Clean(GameEngine g)
        {foreach(var p in g.State.players){p.hand.Clear();p.units.Clear();p.hp=30;}g.State.phase="action";g.State.activeSeat=0;}
        static HandCard Give(GameEngine g,int seat,string id)
        {var h=new HandCard{uid="fixture"+(++serial),cardId=id};g.State.players[seat].hand.Add(h);return h;}
        static UnitState Unit(GameEngine g,int seat,string id,int slot=0)
        {var u=new UnitState{uid="unit"+(++serial),cardId=id,hp=catalog.Card(id).health,slot=slot};g.State.players[seat].units.Add(u);return u;}
        static CommandResult Send(GameEngine g,int seat,GameCommand cmd,double time=0)
        {cmd.seq=++serial;return g.Submit(seat,cmd,time);}
        static void Play(GameEngine g,int seat,string id,int target=1,string unit="",int slot=0)
        {var h=Give(g,seat,id);Check(Send(g,seat,new GameCommand{kind="play",cardUid=h.uid,targetSeat=target,targetUnit=unit,slot=slot}).ok,"play "+id);}
        static void Success(GameEngine g)
        {
            int safety=20;
            while(g.State.qte!=null&&safety-->0)
            {var q=g.State.qte;Check(Send(g,q.owner,new GameCommand{kind="key",phaseId=q.id,key=q.sequence[q.index].ToString()}).ok,"QTE key");}
            Check(safety>0,"QTE completes");
        }
        static void PassAll(GameEngine g)
        {
            int guard=30;while(g.State.phase=="reaction"&&guard-->0)
            {var a=g.State.pending;var p=g.State.players.First(x=>!a.responded.Contains(x.seat));Check(Send(g,p.seat,new GameCommand{kind="pass",phaseId=a.id}).ok,"reaction pass");}
            Check(guard>0,"reaction settles");
        }
        static void Test(string name,Action body){body();results.Add("PASS "+name);Debug.Log("PASS "+name);}
        public static void Run()
        {
            asserts=0;serial=0;results.Clear();catalog=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("Data/catalog").text);catalog.Validate();
            Test("catalog / all faction roles / 3 x 30 cards",()=>{Check(catalog.cards.Count==30,"unique cards");Check(catalog.decks.All(d=>d.entries.Sum(e=>e.count)==30),"deck size");});
            Test("2-4 seats, private hands, ownership and replay protection",()=>{
                for(int n=2;n<=4;n++)
                {
                    var g=New(n);var view=g.View(1,0);Check(view.players[0].hand.Count==0&&view.players[0].handCount==6,"hidden starting hand");
                    Check(view.players[1].hand.Count==5,"own hand visible");view.players[1].hand.Clear();Check(g.State.players[1].hand.Count==5,"view is independent");
                    Check(!Send(g,1,new GameCommand{kind="end"}).ok,"wrong turn");var command=new GameCommand{kind="play",cardUid=g.State.players[1].hand[0].uid};
                    Check(!Send(g,0,command).ok,"card ownership");Check(!g.Submit(0,command,0).ok,"replay");
                }
            });
            Test("creature first attack, enters board, does not attack twice",()=>{
                var g=New();Clean(g);Play(g,0,"C02");Success(g);Check(g.State.players[1].hp==27,"opening attack once");
                Check(g.State.players[0].units.Count==1&&g.State.players[0].units[0].exhausted,"summoned exhausted");Check(g.State.activeSeat==1,"summon ends turn");
            });
            Test("summon killed by retaliation never enters board",()=>{
                var g=New();Clean(g);var defender=Unit(g,1,"C03");Play(g,0,"C01",1,defender.uid);Success(g);
                Check(g.State.players[0].units.Count==0,"dead summon absent");Check(defender.hp==1,"defender took attack");
            });
            Test("unassigned attacks choose enemy heroes, never creatures",()=>{
                for(int n=2;n<=4;n++)
                {var g=New(n);Clean(g);Unit(g,0,"C03");var u=Unit(g,1,"C01");Send(g,0,new GameCommand{kind="end"});PassAll(g);
                    Check(u.hp==3,"unit ignored by automatic attack");Check(g.State.players.Skip(1).Sum(p=>p.hp)==30*(n-1)-4,"one enemy hero damaged");}
            });
            Test("three QTE misses transfer exact card and end creature turn",()=>{
                var g=New(4);Clean(g);Play(g,0,"C02");var q=g.State.qte;string id=q.cardUid;int recipient=q.recipient;
                for(int i=0;i<3;i++)Send(g,0,new GameCommand{kind="key",phaseId=q.id,key=q.sequence[q.index]=='A'?"S":"A"});
                Check(g.State.qte==null,"QTE failed");Check(g.State.players[recipient].hand.Any(h=>h.uid==id),"transferred to announced recipient");Check(g.State.activeSeat==1,"turn ended");
            });
            Test("all timing modifiers and first-miss forgiveness",()=>{
                var g=New();Clean(g);Unit(g,0,"C04");Unit(g,0,"C18",1);Unit(g,1,"C05");Unit(g,1,"C16",1);
                Play(g,0,"S01");var q=g.State.qte;Check(q.sequence.Length==4,"extra key");Check(Math.Abs(q.duration-16.6)<.01,"time bonus minus tax");
                double deadline=q.deadline;Send(g,0,new GameCommand{kind="key",phaseId=q.id,key=q.sequence[0]=='A'?"S":"A"});
                Check(q.mistakes==0&&q.forgiven==1&&q.deadline==deadline,"forgiven");Success(g);Check(g.State.players[1].hp==26,"spell damage");Check(g.State.activeSeat==0,"spell does not end turn");
            });
            Test("guard caps and aura excludes its owner",()=>{
                var g=New();Clean(g);Unit(g,1,"C10");Unit(g,1,"C02",1);Unit(g,1,"C09",2);Play(g,0,"S01");Success(g);Check(g.State.players[1].hp==29,"guard capped at 3");
                var goose=Unit(g,0,"C03");var raccoon=Unit(g,0,"C01",1);Check(g.Attack(0,goose)==4&&g.Attack(0,raccoon)==3,"attack aura other units only");
            });
            Test("reactions have no QTE, shared window, reduce and reflect",()=>{
                var g=New(3);Clean(g);var reaction=Give(g,1,"R01");var rescue=Give(g,2,"R02");Play(g,0,"S01");Success(g);
                Check(g.State.phase=="reaction"&&g.State.qte==null,"reaction window");var a=g.State.pending;
                Check(Send(g,1,new GameCommand{kind="react",cardUid=reaction.uid,phaseId=a.id,targetSeat=1}).ok,"reduce");
                Check(!Send(g,1,new GameCommand{kind="react",cardUid=reaction.uid,phaseId=a.id,targetSeat=1}).ok,"one reaction only");
                Check(Send(g,2,new GameCommand{kind="react",cardUid=rescue.uid,phaseId=a.id,targetSeat=1}).ok,"ally rescue");Check(g.State.players[1].hp==30,"combined reduction");
                var g2=New();Clean(g2);var reflect=Give(g2,1,"R03");Play(g2,0,"S01");Success(g2);
                Send(g2,1,new GameCommand{kind="react",cardUid=reflect.uid,phaseId=g2.State.pending.id,targetSeat=1});Check(g2.State.players[0].hp==28&&g2.State.players[1].hp==26,"reflect after damage");
            });
            Test("deny only protects its owner's part of area spell",()=>{
                var g=New(3);Clean(g);var h=Give(g,1,"R04");Play(g,0,"S03",-1);Success(g);
                Send(g,1,new GameCommand{kind="react",cardUid=h.uid,phaseId=g.State.pending.id,targetSeat=1});Check(g.State.players[1].hp==30&&g.State.players[2].hp==28,"one target denied");
            });
            Test("heal, draw, stun, swap, risk, bounce",()=>{
                var g=New();Clean(g);g.State.players[0].hp=28;Play(g,0,"S02",0);Success(g);Check(g.State.players[0].hp==30,"heal cap");
                Play(g,0,"S06",-1);Success(g);Check(g.State.players[0].hand.Count==2,"draw two");
                g=New();Clean(g);var u=Unit(g,1,"C02");Play(g,0,"S04",1,u.uid);Success(g);Check(u.skipAttacks==1,"stun set");Send(g,0,new GameCommand{kind="end"});Send(g,1,new GameCommand{kind="end"});Check(g.State.players[0].hp==30&&u.skipAttacks==0,"stun skips attack");
                g=New();Clean(g);var first=Give(g,0,"C01");var second=Give(g,1,"C02");Play(g,0,"S05");Success(g);Check(g.State.players[0].hand.Any(h=>h.uid==second.uid)&&g.State.players[1].hand.Any(h=>h.uid==first.uid),"swap physical cards");
                g=New();Clean(g);Play(g,0,"S07",-1);Success(g);Play(g,0,"C02");Check(g.State.qte.sequence.Length==6,"risk length");Success(g);Check(g.State.players[1].hp==24,"risk opening damage");
                g=New();Clean(g);u=Unit(g,1,"C02");Play(g,0,"S08",1,u.uid);Success(g);Check(g.State.players[1].units.Count==0&&g.State.players[1].hand.Any(h=>h.uid==u.uid),"bounce identity");
            });
            Test("turn limits and bounded decision timers",()=>{
                var g=New();Clean(g);for(int i=0;i<3;i++){Play(g,0,"S02",0);Success(g);}var h=Give(g,0,"S06");Check(!Send(g,0,new GameCommand{kind="play",cardUid=h.uid}).ok,"fourth spell blocked");
                h=Give(g,0,"C02");Check(!Send(g,0,new GameCommand{kind="play",cardUid=h.uid,slot=0,targetSeat=1}).ok,"creature after two spells blocked");g.Tick(100);Check(g.State.activeSeat==1,"turn timeout");
                g=New();Clean(g);Play(g,0,"C01");g.Tick(100);Check(g.State.activeSeat==1,"QTE timeout");
            });
            Test("elimination scoring, round reset, host-independent disconnect",()=>{
                var g=New(3);Clean(g);g.State.players[1].hp=1;Play(g,0,"S01");Success(g);Check(!g.State.players[1].alive&&g.State.players[0].score==1,"kill points");
                g.State.players[2].hp=1;Play(g,0,"S01",2);Success(g);Check(g.State.phase=="roundEnd"&&g.State.players[0].score==5,"round points");g.Tick(30);
                Check(g.State.round==2&&g.State.activeSeat==1&&g.State.players.All(p=>p.hp==30&&p.units.Count==0),"round resets and starter rotates");Check(g.State.players[0].score==5,"score persists");
                g.Disconnect(1,31);Check(g.State.activeSeat!=1,"disconnect advances turn");g.Disconnect(2,31);Check(g.State.phase=="matchEnd","one connected ends match");
            });
            Test("draw, heal, damage and opening passive triggers",()=>{
                var g=New();Clean(g);Unit(g,0,"C01");Play(g,0,"S02",0);Success(g);Check(g.State.players[0].hand.Count==1,"raccoon first spell draw");
                Play(g,0,"S02",0);Success(g);Check(g.State.players[0].hand.Count==1,"raccoon only once per turn");
                g=New();Clean(g);Unit(g,0,"C07");Send(g,0,new GameCommand{kind="end"});Send(g,1,new GameCommand{kind="end"});Check(g.State.players[0].hand.Count==3,"normal plus accountant plus no-QTE draw");
                g=New();Clean(g);Unit(g,0,"C12");
                for(int attempt=0;attempt<2;attempt++)
                {Play(g,0,"S01");var q=g.State.qte;for(int i=0;i<3;i++)Send(g,0,new GameCommand{kind="key",phaseId=q.id,key=q.sequence[0]=='A'?"S":"A"});Check(g.State.players[0].hand.Count==1,"printer only first failure");}
                g=New();Clean(g);g.State.players[0].hp=20;Unit(g,0,"C08");Play(g,0,"C02",1,"",1);Success(g);Check(g.State.players[0].hp==22,"lifesteal opening and existing creature");
                g=New();Clean(g);Unit(g,1,"C06");Play(g,0,"S01");Success(g);Check(g.State.players[0].hp==29,"thorns");
                g=New(3);Clean(g);Unit(g,0,"C11");Play(g,0,"S03",-1);Success(g);Check(g.State.players[1].hp==27&&g.State.players[2].hp==27,"spell aura on AOE");
                g=New();Clean(g);Unit(g,1,"C13");g.State.players[0].hp=1;Play(g,0,"S01");Success(g);Check(!g.State.players[0].alive&&g.State.players[1].score==4,"spell tax kill credited to owner");
                g=New();Clean(g);g.State.players[0].hp=20;Unit(g,0,"C14");Unit(g,0,"C17",1);Send(g,0,new GameCommand{kind="end"});Send(g,1,new GameCommand{kind="end"});Check(g.State.players[0].hp==24,"turn healing");
                g=New();Clean(g);Unit(g,0,"C15");Play(g,0,"C02",1,"",1);Success(g);Check(g.State.players[1].hp==23,"opening aura and normal end attack");
            });
            Test("wire serialization preserves private views and rejects stale phase commands",()=>{
                var g=New(4);for(int n=0;n<4;n++)
                {
                    var view=g.View(n,1.5);var roundtrip=JsonUtility.FromJson<WireMessage>(JsonUtility.ToJson(new WireMessage{kind="state",state=view,matchId=view.matchId}));
                    Check(roundtrip.state.players.Where(p=>p.seat!=n).All(p=>p.hand.Count==0),"wire has no opponent cards");
                    Check(roundtrip.state.players[n].hand.Count==g.State.players[n].hand.Count,"own cards survive serialization");
                    Check(roundtrip.matchId==g.State.matchId,"match identity preserved");
                }
                Clean(g);Play(g,0,"C01");var q=g.State.qte;
                Check(!Send(g,0,new GameCommand{kind="key",phaseId="old",key="A"}).ok,"old QTE id rejected");
                Check(!Send(g,1,new GameCommand{kind="key",phaseId=q.id,key="A"}).ok,"other player's key rejected");
                Check(!Send(g,0,new GameCommand{kind="key",phaseId=q.id,key=q.sequence[0].ToString()},q.deadline+1).ok,"late key rejected");
            });
            Test("full matches for 2, 3, 4 players across 24 deterministic seeds",()=>{
                for(int n=2;n<=4;n++)for(int seed=0;seed<8;seed++)Simulate(n,seed*97+13);
            });
            results.Add("Assertions: "+asserts);results.Add("UTC: "+DateTime.UtcNow.ToString("O"));
            Directory.CreateDirectory("../output/tests");File.WriteAllLines("../output/tests/core-tests.txt",results);
            Debug.Log("ALL_CORE_TESTS_PASSED assertions="+asserts);
        }
        static void Simulate(int count,int seed)
        {
            var g=New(count,seed);double time=0;int steps=0;
            while(g.State.phase!="matchEnd"&&steps++<6000)
            {
                var s=g.State;time+=.15;
                if(s.phase=="qte")
                {var q=s.qte;Send(g,q.owner,new GameCommand{kind="key",phaseId=q.id,key=q.sequence[q.index].ToString()},time);}
                else if(s.phase=="reaction")
                {var a=s.pending;int p=s.players.First(x=>!a.responded.Contains(x.seat)).seat;Send(g,p,new GameCommand{kind="pass",phaseId=a.id},time);}
                else if(s.phase=="roundEnd"){time+=20;g.Tick(time);}
                else if(s.phase=="action")
                {
                    var p=s.players[s.activeSeat];var enemy=s.players.FirstOrDefault(x=>x.alive&&x.seat!=p.seat);bool played=false;
                    foreach(var h in p.hand.ToList().OrderBy(h=>catalog.Card(h.cardId).kind=="creature"?0:1))
                    {
                        var c=catalog.Card(h.cardId);if(c.kind=="reaction")continue;
                        var target=new TargetRef(enemy?.seat??-1);int slot=Enumerable.Range(0,5).FirstOrDefault(x=>!p.units.Any(u=>u.slot==x));
                        if(c.target=="hero")target=new TargetRef(p.seat);
                        if(c.target=="enemyUnit"||c.target=="unit")
                        {var owner=s.players.FirstOrDefault(x=>x.alive&&x.seat!=p.seat&&x.units.Count>0);if(owner==null)continue;target=new TargetRef(owner.seat,owner.units[0].uid);}
                        var r=Send(g,p.seat,new GameCommand{kind="play",cardUid=h.uid,slot=slot,targetSeat=target.seat,targetUnit=target.unit},time);
                        if(r.ok){played=true;break;}
                    }
                    if(!played)Send(g,p.seat,new GameCommand{kind="end"},time);
                }
                else g.Tick(time);
                foreach(var p in s.players)
                {Check(p.hand.Count<=8&&p.units.Count<=5,"capacity invariant");Check(p.units.Select(u=>u.slot).Distinct().Count()==p.units.Count,"unique slots");Check(p.hp>=0&&p.hp<=30,"hp invariant");}
            }
            Check(steps<6000&&g.State.round==3,"match terminates, all 3 rounds");Check(g.State.winners.Count>0,"winner exists");
        }
    }
}
