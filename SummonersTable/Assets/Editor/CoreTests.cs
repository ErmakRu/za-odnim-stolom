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
        static CommandResult Send(GameEngine g,int seat,GameCommand cmd,double time=-1)
        {cmd.seq=++serial;var result=g.Submit(seat,cmd,time<0?g.State.serverTime:time);if(cmd.kind=="end"&&result.ok)FinishCombat(g);return result;}
        static void FinishCombat(GameEngine g)
        {int safety=30;while(g.State.phase=="combat"&&safety-->0)g.Tick(g.State.deadline+.001);Check(safety>0,"bounded combat animation phases");}
        static void Play(GameEngine g,int seat,string id,int target=1,string unit="",int slot=0,bool startQte=true)
        {var h=Give(g,seat,id);Check(Send(g,seat,new GameCommand{kind="play",cardUid=h.uid,targetSeat=target,targetUnit=unit,slot=slot}).ok,"play "+id);if(startQte)g.Tick(g.State.cast.revealUntil);}
        static void Success(GameEngine g)
        {
            int safety=20;
            while(g.State.qte!=null&&safety-->0)
            {var q=g.State.qte;Check(Send(g,q.owner,new GameCommand{kind="key",phaseId=q.id,key=q.sequence[q.index].ToString()}).ok,"QTE key");}
            Check(safety>0,"QTE completes");
        }
        static void Test(string name,Action body){body();results.Add("PASS "+name);Debug.Log("PASS "+name);}
        public static void Run()
        {
            asserts=0;serial=0;results.Clear();catalog=CardLibrary.LoadCatalog();catalog.Validate();
            Test("catalog / all faction roles / 3 x 30 cards",()=>{Check(catalog.cards.Count==30,"unique cards");Check(catalog.decks.All(d=>d.entries.Sum(e=>e.count)==30),"deck size");Check(catalog.decks.All(d=>d.entries.Where(e=>catalog.Card(e.cardId).kind=="reaction").Sum(e=>e.count)==3),"reactions reduced to 3 of 30");});
            Test("lobby appearance and bounded cosmetic look survive private wire views",()=>{
                var members=new[]{new LobbyMember{id="a",name="A",heroId="owl",outfit=6,palette=2},new LobbyMember{id="b",name="B",heroId="invalid",outfit=99,palette=-5}};
                var g=new GameEngine(catalog,members,42);Check(g.State.players[0].heroId=="owl"&&g.State.players[0].outfit==6&&g.State.players[0].palette==2,"selected outfit enters match");
                Check(g.State.players[1].heroId=="badger"&&g.State.players[1].outfit==7&&g.State.players[1].palette==0,"untrusted appearance sanitized");
                int hp=g.State.players[0].hp,turn=g.State.turnNumber;
                Check(g.Submit(1,new GameCommand{seq=1,kind="look",lookYaw=500,lookPitch=-500,cameraMode=2},0).ok,"observer can look around");
                Check(g.State.players[1].lookYaw==90&&g.State.players[1].lookPitch==-90&&g.State.turnNumber==turn&&g.State.players[0].hp==hp,"look bounded and cosmetic only");
                Check(!g.Submit(1,new GameCommand{seq=2,kind="look",lookYaw=float.NaN},0).ok,"nonfinite look rejected");
                Check(!g.Submit(1,new GameCommand{seq=3,kind="look",cameraMode=3},0).ok,"invalid camera rejected");
                var view=JsonUtility.FromJson<MatchState>(JsonUtility.ToJson(g.View(0,0)));view.RestoreViewPrivacy(0);
                Check(view.players[1].lookYaw==90&&view.players[0].heroId=="owl"&&view.players[1].hand.Count==0,"cosmetics survive JSON without exposing hands");
            });
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
                var g=New();Clean(g);Play(g,0,"C02");Success(g);Check(g.State.players[1].hp==30&&g.State.phase=="action"&&g.State.activeSeat==0&&g.State.players[0].units[0].deploying,"summon waits for end turn");Send(g,0,new GameCommand{kind="end"});Check(g.State.players[1].hp==27,"opening attack once");
                Check(g.State.players[0].units.Count==1&&g.State.players[0].units[0].exhausted,"summoned exhausted");Check(g.State.activeSeat==1,"explicit button ends turn");
            });
            Test("summon killed by retaliation never enters board",()=>{
                var g=New();Clean(g);var defender=Unit(g,1,"C03");Play(g,0,"C01",1,defender.uid);Success(g);Send(g,0,new GameCommand{kind="target",unitUid=g.State.players[0].units[0].uid,targetSeat=1,targetUnit=defender.uid});Send(g,0,new GameCommand{kind="end"});
                Check(g.State.players[0].units.Count==0,"dead summon absent");Check(defender.hp==1,"defender took attack");
            });
            Test("unassigned attacks choose enemy heroes, never creatures",()=>{
                for(int n=2;n<=4;n++)
                {var g=New(n);Clean(g);Unit(g,0,"C03");var u=Unit(g,1,"C01");Send(g,0,new GameCommand{kind="end"});
                    Check(u.hp==3,"unit ignored by automatic attack");Check(g.State.players.Skip(1).Sum(p=>p.hp)==30*(n-1)-4,"one enemy hero damaged");}
            });
            Test("three QTE misses transfer exact card and preserve manual turn",()=>{
                var g=New(4);Clean(g);Play(g,0,"C02");var q=g.State.qte;string id=q.cardUid;int recipient=q.recipient;
                for(int i=0;i<3;i++)Send(g,0,new GameCommand{kind="key",phaseId=q.id,key=q.sequence[q.index]=='A'?"S":"A"});
                Check(g.State.qte==null,"QTE failed");Check(g.State.players[recipient].hand.Any(h=>h.uid==id),"transferred to announced recipient");Check(g.State.activeSeat==0&&g.State.phase=="action","turn remains manual after failure");
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
            Test("two-second reveal, public progress, private keys and timer",()=>{
                var g=New(3);Clean(g);var reaction=Give(g,1,"R01");Play(g,0,"S01",1,"",0,false);string cast=g.State.cast.id;
                Check(g.State.phase=="reveal"&&g.State.cast.revealUntil==2,"two-second reveal");
                Check(g.View(0,0).qte==null&&g.View(1,0).qte==null,"no keys before reveal completes");
                Check(!Send(g,0,new GameCommand{kind="key",phaseId=cast,key="A"},1).ok,"early keys rejected");
                Check(Send(g,1,new GameCommand{kind="react",cardUid=reaction.uid,phaseId=cast,targetSeat=1},1.5).ok,"reaction during reveal");
                g.Tick(2);var q=g.State.qte;Send(g,0,new GameCommand{kind="key",phaseId=q.id,key=q.sequence[0].ToString()});
                var other=g.View(1,2);Check(other.qte==null&&other.deadline==0,"observer has no letters or deadline");
                Check(other.cast.qteLength==q.sequence.Length&&other.cast.qteProgress==1,"observer sees public progress count");
                Send(g,0,new GameCommand{kind="key",phaseId=q.id,key=q.sequence[1]=='A'?"S":"A"});
                Check(g.View(1,2).cast.qteMistakes==1,"public remaining attempts");
                Check(!Send(g,1,new GameCommand{kind="react",cardUid=reaction.uid,phaseId=cast,targetSeat=1}).ok,"one reaction per player");
                Success(g);Check(g.State.players[1].hp==29&&g.State.phase=="action","shield only for spell effect");
                Check(!Send(g,2,new GameCommand{kind="pass",phaseId=cast}).ok,"reaction window closed after success");
            });
            Test("copy and return react to summoning, never to attacks",()=>{
                var g=New(3);Clean(g);var copy=Give(g,1,"R02");var bounce=Give(g,2,"R03");var shield=Give(g,2,"R01");
                Play(g,0,"C02",-1,"",2);var cast=g.State.cast.id;
                Check(!Send(g,2,new GameCommand{kind="react",cardUid=shield.uid,phaseId=cast,targetSeat=0}).ok,"no defensive shield on summoning");
                Check(Send(g,1,new GameCommand{kind="react",cardUid=copy.uid,phaseId=cast,targetSeat=0}).ok,"copy accepted");
                Check(Send(g,2,new GameCommand{kind="react",cardUid=bounce.uid,phaseId=cast,targetSeat=0}).ok,"return accepted");
                Success(g);Check(g.State.players[0].units.Count==0&&g.State.players[0].hand.Any(h=>h.cardId=="C02"),"source returns to hand");
                Check(g.State.tableReactions.All(r=>r.resolved&&r.successful),"applied summon reactions animate success");
                Check(g.State.players[1].units.Count==1&&g.State.players[1].units[0].slot==2&&g.State.players[1].units[0].exhausted&&!g.State.players[1].units[0].deploying,"copy survives return, same slot, passive active");
                g=New();Clean(g);Unit(g,1,"C01",2);copy=Give(g,1,"R02");Play(g,0,"C02",-1,"",2);
                Check(!Send(g,1,new GameCommand{kind="react",cardUid=copy.uid,phaseId=g.State.cast.id,targetSeat=0}).ok,"copy requires free matching slot");
                g=New(3);Clean(g);var deny=Give(g,1,"R04");Play(g,0,"S03",-1);Send(g,1,new GameCommand{kind="react",cardUid=deny.uid,phaseId=g.State.cast.id,targetSeat=1});Success(g);
                Check(g.State.players[1].hp==30&&g.State.players[2].hp==28,"deny only own part of area spell");
            });
            Test("explicit slot, deferred target, sequential combat and end button",()=>{
                var g=New();Clean(g);var h=Give(g,0,"C02");
                Check(!Send(g,0,new GameCommand{kind="play",cardUid=h.uid,targetSeat=1}).ok&&g.State.players[0].hand.Contains(h),"missing slot does not consume card");
                Check(!MatchRules.ReadyToEnd(catalog,g.View(0,0),0),"playable hand prevents glow");
                Send(g,0,new GameCommand{kind="play",cardUid=h.uid,slot=0,targetSeat=1});g.Tick(2);Success(g);
                var unit=g.State.players[0].units[0];Check(!unit.targetAssigned&&unit.plannedSeat==-1,"pre-QTE target ignored for creature");
                Check(!MatchRules.ReadyToEnd(catalog,g.View(0,2),0),"unassigned arrow prevents glow");
                Send(g,0,new GameCommand{kind="target",unitUid=unit.uid,targetSeat=-1});Check(MatchRules.ReadyToEnd(catalog,g.View(0,2),0),"explicit center counts as assignment");
                var other=Unit(g,0,"C01",1);var result=g.Submit(0,new GameCommand{seq=++serial,kind="end"},2);
                Check(result.ok&&g.State.phase=="combat"&&other.targetAssigned&&other.plannedSeat==-1,"end fills missing arrows with center");
                Check(g.State.players[1].hp==30,"damage waits for effect impact");
                Check(!Send(g,1,new GameCommand{kind="react",phaseId=g.State.pending.id}).ok,"attacks have no reaction window");
                g.Tick(2.41);Check(g.State.players[1].hp==27&&g.State.phase=="combat","first hit arrives before second");
                FinishCombat(g);Check(g.State.players[1].hp==25&&g.State.activeSeat==1,"attacks finish in slot order");
            });
            Test("no reactions on board attacks; failed casts spend reactions",()=>{
                var g=New();Clean(g);Unit(g,0,"C03");var h=Give(g,1,"R01");Send(g,0,new GameCommand{kind="end"});
                Check(g.State.phase=="action"&&g.State.players[1].hp==26,"board attack resolves without interruption");
                Check(g.State.players[1].hand.Any(x=>x.uid==h.uid),"reaction not consumed by board attack");
                g=New();Clean(g);h=Give(g,1,"R03");Play(g,0,"C02");
                Send(g,1,new GameCommand{kind="react",cardUid=h.uid,phaseId=g.State.cast.id,targetSeat=0});
                g.Tick(g.State.qte.deadline+1);
                Check(g.State.players[1].hp==30&&g.State.players[0].units.Count==0,"failed cast no damage or summon");
                Check(!g.State.players[1].hand.Any(x=>x.uid==h.uid)&&g.State.tableReactions.Count==1,"played reaction spent and visible");
                Check(g.State.tableReactions[0].resolved&&!g.State.tableReactions[0].successful,"failed cast does not animate successful reaction");
            });
            Test("persistent attack plans and random center targeting",()=>{
                var g=New(3);Clean(g);var u=Unit(g,0,"C01");
                Check(Send(g,0,new GameCommand{kind="target",unitUid=u.uid,targetSeat=2}).ok,"assign target");
                Send(g,0,new GameCommand{kind="end"});Send(g,1,new GameCommand{kind="end"});Send(g,2,new GameCommand{kind="end"});
                Check(u.plannedSeat==2&&g.State.players[2].hp==28,"intention survives next turn");
                Check(Send(g,0,new GameCommand{kind="target",unitUid=u.uid,targetSeat=-1}).ok&&u.plannedSeat==-1,"center clears explicit target");
                var defender=Unit(g,1,"C02");Send(g,0,new GameCommand{kind="target",unitUid=u.uid,targetSeat=1,targetUnit=defender.uid});
                Play(g,0,"S08",1,defender.uid);Success(g);Check(u.plannedSeat==-1,"removed target normalizes to center");
                g=New(4);Clean(g);Unit(g,1,"C10");Play(g,0,"C02",-1);
                Check(g.State.cast.targetSeat==-1,"summon has no attack target before QTE");
                Check(g.State.pending.targets.All(t=>t.unit==""),"random summon can only hit a hero");
                Success(g);Check(g.State.players[0].units[0].plannedSeat==-1,"summoned random intention persists");
            });
            Test("disconnect during announcement clears cast and intentions",()=>{
                var g=New(4);Clean(g);var u=Unit(g,2,"C03");u.plannedSeat=0;
                Play(g,0,"C02",1,"",0,false);g.Disconnect(0,1);
                Check(g.State.cast==null&&g.State.qte==null&&g.State.pending==null,"disconnected cast removed");
                Check(g.State.activeSeat==1&&g.State.phase=="action"&&u.plannedSeat==-1,"turn and intentions recover");
            });
            Test("heal, draw, stun, swap, risk, bounce",()=>{
                var g=New();Clean(g);g.State.players[0].hp=28;Play(g,0,"S02",0);Success(g);Check(g.State.players[0].hp==30,"heal cap");
                Play(g,0,"S06",-1);Success(g);Check(g.State.players[0].hand.Count==2,"draw two");
                g=New();Clean(g);var u=Unit(g,1,"C02");Play(g,0,"S04",1,u.uid);Success(g);Check(u.skipAttacks==1,"stun set");Send(g,0,new GameCommand{kind="end"});Send(g,1,new GameCommand{kind="end"});Check(g.State.players[0].hp==30&&u.skipAttacks==0,"stun skips attack");
                g=New();Clean(g);var first=Give(g,0,"C01");var second=Give(g,1,"C02");Play(g,0,"S05");Success(g);Check(g.State.players[0].hand.Any(h=>h.uid==second.uid)&&g.State.players[1].hand.Any(h=>h.uid==first.uid),"swap physical cards");
                g=New();Clean(g);Play(g,0,"S07",-1);Success(g);Play(g,0,"C02");Check(g.State.qte.sequence.Length==6,"risk length");Success(g);Send(g,0,new GameCommand{kind="end"});Check(g.State.players[1].hp==24,"risk opening damage");
                g=New();Clean(g);u=Unit(g,1,"C02");Play(g,0,"S08",1,u.uid);Success(g);Check(g.State.players[1].units.Count==0&&g.State.players[1].hand.Any(h=>h.uid==u.uid),"bounce identity");
                var snapshot=g.State.history.Last(e=>e.kind=="effect");Check(snapshot.targets[0].slot==u.slot&&snapshot.targets[0].cardId==u.cardId,"bounce VFX retains removed unit slot and public card");
                g=New(3);Clean(g);var protect=Give(g,1,"R04");Play(g,0,"S03",-1);Send(g,1,new GameCommand{kind="react",cardUid=protect.uid,phaseId=g.State.cast.id,targetSeat=1});Success(g);
                snapshot=g.View(2,10).history.Last(e=>e.kind=="effect");Check(snapshot.targets.First(t=>t.seat==1).prevented&&!snapshot.targets.First(t=>t.seat==2).prevented,"public spell VFX skips precisely denied target");
            });
            Test("turn limits and bounded decision timers",()=>{
                var g=New();Clean(g);for(int i=0;i<3;i++){Play(g,0,"S02",0);Success(g);}var h=Give(g,0,"S06");Check(!Send(g,0,new GameCommand{kind="play",cardUid=h.uid}).ok,"fourth spell blocked");
                h=Give(g,0,"C02");Check(!Send(g,0,new GameCommand{kind="play",cardUid=h.uid,slot=0,targetSeat=1}).ok,"creature after two spells blocked");g.Tick(100);Check(g.State.activeSeat==1,"turn timeout");
                g=New();Clean(g);Play(g,0,"C01");g.Tick(100);Check(g.State.activeSeat==0&&g.State.phase=="action","QTE timeout preserves manual end");
            });
            Test("elimination scoring, one-round finish, host-independent disconnect",()=>{
                var g=New(3);Clean(g);g.State.players[1].hp=1;Play(g,0,"S01");Success(g);Check(!g.State.players[1].alive&&g.State.players[0].score==1,"kill points");
                g.State.players[2].hp=1;Play(g,0,"S01",2);Success(g);Check(g.State.phase=="matchEnd"&&g.State.players[0].score==5,"one-round match points");g.Tick(30);
                Check(g.State.round==1&&g.State.phase=="matchEnd","no automatic second round");Check(g.State.players[0].score==5,"score persists");
                g=New(3);Clean(g);
                g.Disconnect(1,31);Check(g.State.activeSeat!=1,"disconnect advances turn");g.Disconnect(2,31);Check(g.State.phase=="matchEnd","one connected ends match");
            });
            Test("draw, heal, damage and opening passive triggers",()=>{
                var g=New();Clean(g);Unit(g,0,"C01");Play(g,0,"S02",0);Success(g);Check(g.State.players[0].hand.Count==1,"raccoon first spell draw");
                Play(g,0,"S02",0);Success(g);Check(g.State.players[0].hand.Count==1,"raccoon only once per turn");
                g=New();Clean(g);Unit(g,0,"C07");Send(g,0,new GameCommand{kind="end"});Send(g,1,new GameCommand{kind="end"});Check(g.State.players[0].hand.Count==3,"normal plus accountant plus no-QTE draw");
                g=New();Clean(g);Unit(g,0,"C12");
                for(int attempt=0;attempt<2;attempt++)
                {Play(g,0,"S01");var q=g.State.qte;for(int i=0;i<3;i++)Send(g,0,new GameCommand{kind="key",phaseId=q.id,key=q.sequence[0]=='A'?"S":"A"});Check(g.State.players[0].hand.Count==1,"printer only first failure");}
                g=New();Clean(g);g.State.players[0].hp=20;Unit(g,0,"C08");Play(g,0,"C02",1,"",1);Success(g);Send(g,0,new GameCommand{kind="end"});Check(g.State.players[0].hp==22,"lifesteal opening and existing creature");
                g=New();Clean(g);Unit(g,1,"C06");Play(g,0,"S01");Success(g);Check(g.State.players[0].hp==29,"thorns");
                g=New(3);Clean(g);Unit(g,0,"C11");Play(g,0,"S03",-1);Success(g);Check(g.State.players[1].hp==27&&g.State.players[2].hp==27,"spell aura on AOE");
                g=New();Clean(g);Unit(g,1,"C13");g.State.players[0].hp=1;Play(g,0,"S01");Success(g);Check(!g.State.players[0].alive&&g.State.players[1].score==4,"spell tax kill credited to owner");
                g=New();Clean(g);g.State.players[0].hp=20;Unit(g,0,"C14");Unit(g,0,"C17",1);Send(g,0,new GameCommand{kind="end"});Send(g,1,new GameCommand{kind="end"});Check(g.State.players[0].hp==24,"turn healing");
                g=New();Clean(g);Unit(g,0,"C15");Play(g,0,"C02",1,"",1);Success(g);Send(g,0,new GameCommand{kind="end"});Check(g.State.players[1].hp==23,"opening aura and normal end attack");
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
                for(int n=0;n<4;n++)
                {
                    var view=g.View(n,g.State.serverTime);
                    var received=JsonUtility.FromJson<WireMessage>(JsonUtility.ToJson(new WireMessage{kind="state",state=view}));
                    received.state.RestoreViewPrivacy(n);
                    Check(received.protocol==WireMessage.CurrentProtocol&&received.protocol==6,"new network protocol");
                    Check(received.state.cast.qteLength==q.sequence.Length,"public progress survives wire");
                    Check(n==0?received.state.qte.sequence==q.sequence:received.state.qte==null&&received.state.deadline==0,"wire QTE is private");
                }
                Check(!Send(g,0,new GameCommand{kind="key",phaseId="old",key="A"}).ok,"old QTE id rejected");
                Check(!Send(g,1,new GameCommand{kind="key",phaseId=q.id,key="A"}).ok,"other player's key rejected");
                Check(!Send(g,0,new GameCommand{kind="key",phaseId=q.id,key=q.sequence[0].ToString()},q.deadline+1).ok,"late key rejected");
            });
            Test("public history snapshots damage ownership, dead targets and reactions",()=>{
                var g=New(4);Clean(g);var target=Unit(g,1,"C01");target.hp=1;
                Play(g,0,"S01",1,target.uid);Success(g);
                var hit=g.State.history.Last(e=>e.kind=="damage");
                Check(hit.actor==0&&hit.cardId=="S01"&&hit.turnSeat==0&&hit.targets[0].seat==1&&hit.targets[0].cardId=="C01"&&hit.targets[0].amount==1,"actual HP loss and both owners retained after death");
                Check(g.State.players[1].units.Count==0,"target removed");
                var view=g.View(2,0);view.history.Last(e=>e.kind=="damage").targets[0].cardId="changed";
                Check(hit.targets[0].cardId=="C01","history view is deep copy");
                var wire=JsonUtility.FromJson<WireMessage>(JsonUtility.ToJson(new WireMessage{kind="state",state=g.View(3,0)}));
                Check(wire.state.history.Last(e=>e.kind=="damage").targets[0].unit==target.uid,"history snapshot survives wire");
                g=New(4);Clean(g);var attacker=Unit(g,0,"C01");var defender=Unit(g,1,"C03");attacker.targetAssigned=true;attacker.plannedSeat=1;attacker.plannedUnit=defender.uid;
                Send(g,0,new GameCommand{kind="end"});
                Check(g.State.history.Any(e=>e.kind=="attack"&&e.actor==0&&e.cardId=="C01"&&e.targets[0].cardId=="C03"),"attack identifies both creatures");
                Check(g.State.history.Any(e=>e.kind=="damage"&&e.actor==1&&e.cardId=="C03"&&e.targets[0].seat==0&&e.targets[0].cardId=="C01"),"retaliation ownership retained");
                g=New();Clean(g);var reaction=Give(g,1,"R01");Play(g,0,"S01",1,"",0,false);
                Send(g,1,new GameCommand{kind="react",cardUid=reaction.uid,phaseId=g.State.cast.id,targetSeat=1});
                var r=g.State.history.Last(e=>e.kind=="reaction");Check(r.actor==1&&r.cardId=="R01"&&r.targets[0].seat==0&&r.targets[0].cardId=="S01","reaction names affected cast and caster");
            });
            Test("history archive deduplicates, bounds wire size and never logs private draws",()=>{
                var g=New(4);Check(g.State.history.All(e=>e.cardId==""&&e.targets.All(t=>t.cardId=="")),"starting hands never in history");
                var archive=new HistoryArchive();var state=g.View(0,0);state.history.Clear();
                for(int i=1;i<=160;i++)
                {
                    state.history.Add(new HistoryEntry{id=i,round=1,turn=1,turnSeat=0,actor=0,kind="damage",cardId="S03",detail="Урон предотвращён",targets=Enumerable.Range(0,4).Select(n=>new HistoryTarget{seat=n,cardId="C01",unit="unit-123456789",amount=4}).ToList()});
                    if(state.history.Count>80)state.history.RemoveAt(0);archive.Observe(state);archive.Observe(state);
                }
                Check(archive.Entries.Count==160&&state.history.Count==80,"archive retains old wire windows without duplicates");
                string json=JsonUtility.ToJson(new WireMessage{kind="state",state=state});
                Check(System.Text.Encoding.UTF8.GetByteCount(json)<65536,"history fits Steam packet including worst-case target lists");
                state.matchId="next";state.history.Clear();archive.Observe(state);Check(archive.Entries.Count==0,"new match clears history");
                for(int i=0;i<40;i++){g.State.players.ForEach(p=>p.hp=30);g.State.creaturePlayed=0;g.State.spellsPlayed=0;g.State.activeSeat=0;g.State.phase="action";Play(g,0,"S01");Success(g);}
                Check(g.State.history.Count==80&&g.State.history[0].id>1,"engine bounds public window");
            });
            Test("playability highlight follows limits, slots, targets and reaction window",()=>{
                var g=New();Clean(g);Func<string,bool> usable=id=>MatchRules.CanUse(catalog,g.View(0,0),0,catalog.Card(id));
                Check(usable("C01")&&usable("S01")&&!usable("R01"),"normal cards available, reactions closed");
                g.State.creaturePlayed=1;g.State.spellsPlayed=1;Check(!usable("C01")&&!usable("S01"),"turn limits disable glow");
                g.State.creaturePlayed=0;g.State.spellsPlayed=0;for(int i=0;i<5;i++)Unit(g,0,"C01",i);Check(!usable("C01"),"full board disables glow");
                g.State.players[0].connected=false;Check(!usable("S01"),"disconnected cannot play");g.State.players[0].connected=true;
                g=New();Clean(g);Give(g,1,"R01");Play(g,0,"S01",1,"",0,false);
                Check(MatchRules.CanUse(catalog,g.View(1,0),1,catalog.Card("R01"))&&!MatchRules.CanUse(catalog,g.View(1,0),1,catalog.Card("C01")),"only matching reaction glows during reveal");
                g.State.pending.responded.Add(1);Check(!MatchRules.CanUse(catalog,g.View(1,0),1,catalog.Card("R01")),"already responded disables reaction");
            });
            Test("rematch votes, changed decks and ordered match transitions",()=>{
                var g=New(3);Clean(g);Check(!Send(g,0,new GameCommand{kind="postMatch",choice="again"}).ok,"cannot vote during gameplay");g.State.phase="matchEnd";
                var members=g.State.players.Select(p=>new LobbyMember{id=p.id,deckId=p.deckId,ready=true,readyMatch="old"}).ToList();
                for(int i=0;i<2;i++)Check(Send(g,i,new GameCommand{kind="postMatch",choice="again"}).ok,"vote accepted");
                Check(!RematchRules.CanRestart(g.State,members),"waits for everybody");
                Check(Send(g,2,new GameCommand{kind="postMatch",choice="deck"}).ok,"deck choice accepted");Check(!RematchRules.CanRestart(g.State,members),"old ready flag cannot start new match");
                members[2].readyMatch=g.State.matchId;members[2].deckId="survive";Check(RematchRules.CanRestart(g.State,members),"deck chooser rejoins after current-match ready");
                members[2].ready=false;Check(!RematchRules.CanRestart(g.State,members),"changing outfit or deck withdraws ready");
                Send(g,2,new GameCommand{kind="postMatch",choice="again"});Check(RematchRules.CanRestart(g.State,members),"all replay votes start match");
                Check(!RematchRules.CanRestart(g.State,members.Take(1).ToList()),"no solo rematch");
                var next=New(3);var wire=new WireMessage{state=next.State,matchId=next.State.matchId,previousMatchId=g.State.matchId};Check(RematchRules.AcceptNext(g.State,wire),"finished match accepts authenticated successor");
                g.State.phase="action";Check(!RematchRules.AcceptNext(g.State,wire),"ongoing match cannot be replaced");
                Check(!RematchRules.AcceptNext(next.State,new WireMessage{state=g.State,matchId=g.State.matchId,previousMatchId=""}),"late old packet cannot roll back a rematch");
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
                if(s.phase=="reveal"){time=s.cast.revealUntil;g.Tick(time);}
                else if(s.phase=="qte")
                {var q=s.qte;Send(g,q.owner,new GameCommand{kind="key",phaseId=q.id,key=q.sequence[q.index].ToString()},time);}
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
            Check(steps<6000&&g.State.round==1,"match terminates, one round");Check(g.State.winners.Count>0,"winner exists");
        }
    }
}
