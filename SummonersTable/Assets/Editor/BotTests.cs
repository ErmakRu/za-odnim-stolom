using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
namespace SummonersTable.Editor
{
    public static class BotTests
    {
        static int checks,serial;static Catalog catalog;static BotsConfig config;
        static void Check(bool ok,string text){checks++;if(!ok)throw new Exception("BOT TEST: "+text);}
        static void Reject(Action action,string label){bool failed=false;try{action();}catch(FormatException){failed=true;}Check(failed,label);}
        static GameEngine New(string mode,int count=2,int seed=7)
        {return new GameEngine(catalog,Enumerable.Range(0,count).Select(i=>new LobbyMember{id="bot-"+i,isBot=true,name="Bot "+i,deckId=catalog.decks[i%3].id}).ToArray(),seed,0,new MatchOptions{mode=mode});}
        static void Clean(GameEngine engine){foreach(var p in engine.State.players){p.hand.Clear();p.units.Clear();p.hp=catalog.rules.heroHp;}engine.State.activeSeat=0;engine.State.phase="action";}
        static HandCard Give(GameEngine engine,int seat,string id){var h=new HandCard{uid="test-"+(++serial),cardId=id};engine.State.players[seat].hand.Add(h);return h;}
        static BotBrain Brain(string mode,BotsConfig settings=null)=>new BotBrain(catalog,(settings??config).Profile(mode),true,new System.Random(4));
        public static void Run()
        {
            checks=serial=0;var bundle=ConfigBundle.Read(ConfigAuthoring.Folder);catalog=bundle.Catalog();config=bundle.bots;
            config.Validate();Check(config.profiles.Length==2,"mode profiles");
            var invalid=ConfigBundle.Clone(config);invalid.profiles[0].nodes[0].children=new[]{"root"};Reject(()=>invalid.Validate(),"cycle rejection");
            invalid=ConfigBundle.Clone(config);invalid.profiles[0].timing.qteMin=0;Reject(()=>invalid.Validate(),"nonzero pacing");
            invalid=ConfigBundle.Clone(config);invalid.profiles[1].nodes.Last().operation="cheat";Reject(()=>invalid.Validate(),"unknown operation");
            invalid=ConfigBundle.Clone(config);invalid.defaultLocalBotSeats=new[]{0};Reject(()=>invalid.Validate(),"human seat protected");
            var changed=ConfigBundle.Clone(config);changed.profiles[0].weights.attack+=1;
            Check(ConfigBundle.Read(ConfigAuthoring.Folder,"bots.json",JsonUtility.ToJson(changed)).gameplayHash==bundle.gameplayHash,"host tuning separate from gameplay hash");
            foreach(string mode in new[]{MatchOptions.Wizards,MatchOptions.Commanders})
            {
                var g=New(mode);Clean(g);var spell=Give(g,0,"S01");Give(g,0,"C02");Give(g,1,"R01");g.State.players[1].hp=1;
                var view=g.View(0,0);Check(view.players[1].hand.Count==0&&view.players[1].handCount==1,"private view");
                var command=Brain(mode).Decide(view,0);Check(command?.kind=="play"&&command.cardUid==spell.uid&&command.targetSeat==1,"lethal spell priority "+mode);
                command.seq=1;Check(g.Submit(0,command,0).ok,"valid lethal command");Check(g.View(1,0).qte==null,"reveal hides sequence");
                g.Tick(g.State.cast.revealUntil);Check(g.View(1,g.State.serverTime).qte==null,"opponent QTE private");
                var key=Brain(mode).Decide(g.View(0,g.State.serverTime),0);Check(key?.kind=="key","own QTE action");
                g=New(mode);Clean(g);Give(g,0,"C02");Give(g,0,"S01");
                if(mode==MatchOptions.Wizards){g.State.spellsPlayed=2;command=Brain(mode).Decide(g.View(0,0),0);Check(command?.kind=="play"&&catalog.Card(g.State.players[0].hand.Find(h=>h.uid==command.cardUid).cardId).kind=="spell","two spells forbid creature");}
                else{g.State.qteSpent=9;command=Brain(mode).Decide(g.View(0,0),0);Check(command?.kind=="end","remaining QTE budget respected");}
                g=New(mode);Clean(g);Give(g,0,"S02");g.State.players[0].hp=10;g.State.players[1].hp=1;command=Brain(mode).Decide(g.View(0,0),0);Check(command.targetSeat==0,"heal self, not enemy");
                g=New(mode);Clean(g);var unit=new UnitState{uid="own",cardId="C03",hp=4,slot=0};g.State.players[0].units.Add(unit);
                var brain=Brain(mode);command=brain.Decide(g.View(0,0),0);Check(command?.kind=="target"&&command.targetSeat!=0,"assign hostile attack");command.seq=1;Check(g.Submit(0,command,0).ok,"valid target");Check(brain.Decide(g.View(0,0),0)?.kind=="end","finish after target");
                g=New(mode);Clean(g);var summoned=Give(g,1,"C02");Give(g,0,"R02");g.State.activeSeat=1;
                Check(g.Submit(1,new GameCommand{seq=1,kind="play",cardUid=summoned.uid,slot=2},0).ok,"summon for reaction");
                command=Brain(mode).Decide(g.View(0,0),0);Check(command?.kind=="react","copy during reveal");command.seq=1;Check(g.Submit(0,command,0).ok,"legal reaction");
                g=New(mode);Clean(g);summoned=Give(g,1,"C02");Give(g,0,"R02");g.State.players[0].units.Add(new UnitState{uid="occupied",cardId="C03",hp=4,slot=2});g.State.activeSeat=1;
                Check(g.Submit(1,new GameCommand{seq=1,kind="play",cardUid=summoned.uid,slot=2},0).ok,"occupied reaction fixture");
                command=Brain(mode).Decide(g.View(0,0),0);Check(command==null||command.kind=="pass","no copy into occupied slot");
                var editable=ConfigBundle.Clone(config);var profile=editable.Profile(mode);var choices=profile.nodes.Single(n=>n.id=="turn.options");choices.children=new[]{"end","aim","play"};editable.Validate();
                g=New(mode);Clean(g);Give(g,0,"S01");Check(Brain(mode,editable).Decide(g.View(0,0),0).kind=="end","JSON reorders behavior");
            }
            var humans=new[]{new LobbyMember{id="123",name="Human"}};
            var payload=JsonUtility.ToJson(new LobbyBots{members=new List<LobbyMember>{new LobbyMember{id="bot-a",deckId="noise",heroId="badger"},new LobbyMember{id="123",deckId="noise",heroId="badger"},new LobbyMember{id="bot-b",deckId="noise",heroId="badger"}}});
            var bots=SteamSession.ReadBots(payload,humans,2,catalog,new MatchOptions(),null);Check(bots.Count==1&&bots[0].isBot&&bots[0].ready,"Steam bot metadata capacity and identity");
            Check(RematchRules.Ready(null,bots[0],"finished"),"new bot ready after roster change");
            int matches=0,totalRejected=0,totalAccepted=0;
            foreach(string mode in new[]{MatchOptions.Wizards,MatchOptions.Commanders})
            foreach(int count in new[]{2,3,4})
            for(int seed=1;seed<=4;seed++)
            {
                var game=New(mode,count,seed);game.State.options.limitPower=seed%2==1;var director=new BotDirector(game,ConfigBundle.Clone(config),seed);double now=0;
                for(int step=0;step<40000&&game.State.phase!="matchEnd";step++)
                {
                    now+=.2;game.Tick(now);director.Tick(game,now);
                    Check(mode==MatchOptions.Commanders?game.State.qteSpent<=catalog.rules.commandersQte:game.State.creaturePlayed<=catalog.rules.wizardCreatures&&game.State.spellsPlayed<=(game.State.creaturePlayed>0?catalog.rules.wizardMixedSpells:catalog.rules.wizardSpells),"turn budget invariant");
                    foreach(var p in game.State.players)Check(p.units.Count<=catalog.rules.boardSlots&&p.units.Select(u=>u.slot).Distinct().Count()==p.units.Count,"slot invariant");
                }
                Check(game.State.phase=="matchEnd","full match finishes "+mode+"/"+count+"/"+seed);Check(director.Rejected==0,"no rejected commands: "+director.LastError);
                for(int i=0;i<20;i++){now+=.2;director.Tick(game,now);}
                Check(game.State.players.All(p=>p.postMatchChoice=="again"),"bots vote rematch including defeated seats");
                totalAccepted+=director.Accepted;totalRejected+=director.Rejected;matches++;
            }
            Directory.CreateDirectory("../output/tests");string report=$"PASS {checks} assertions; {matches} complete 2/3/4-seat bot matches, both modes, power limits on/off; {totalAccepted} accepted bot commands, {totalRejected} rejected. Privacy, JSON tree reorder/validation, legal reactions, lethal/heal targeting, budgets, slots, rematch and Steam lobby metadata. No Player build.\n";
            File.WriteAllText("../output/tests/bots-tests.txt",report);Debug.Log(report);
        }
    }
}