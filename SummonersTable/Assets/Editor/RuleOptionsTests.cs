using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace SummonersTable.Editor
{
    public static class RuleOptionsTests
    {
        static int seq;static Catalog catalog;
        static void Check(bool ok,string label){if(!ok)throw new Exception("OPTIONS TEST: "+label);}
        static GameEngine New(bool commanders=false,bool limited=true,int count=2)
        {
            var g=new GameEngine(catalog,Enumerable.Range(0,count).Select(i=>new LobbyMember{id="options-"+i,name="Player "+i}).ToArray(),9,0,new MatchOptions{mode=commanders?MatchOptions.Commanders:MatchOptions.Wizards,limitPower=limited});
            foreach(var p in g.State.players){p.hand.Clear();p.units.Clear();p.hp=30;}return g;
        }
        static HandCard Give(GameEngine g,string id,int seat=0){var h=new HandCard{uid="options-"+(++seq),cardId=id};g.State.players[seat].hand.Add(h);return h;}
        static CommandResult Send(GameEngine g,int seat,GameCommand c){c.seq=++seq;return g.Submit(seat,c,g.State.serverTime);}
        static void Play(GameEngine g,string id,int slot=0,int target=1){var h=Give(g,id);Check(Send(g,0,new GameCommand{kind="play",cardUid=h.uid,slot=slot,targetSeat=target}).ok,"accepted cast "+id);}
        static void Success(GameEngine g)
        {
            if(g.State.phase=="reveal")g.Tick(g.State.cast.revealUntil);
            int left=100;while(g.State.qte!=null&&left-->0){var q=g.State.qte;Check(Send(g,q.owner,new GameCommand{kind="key",phaseId=q.id,key=q.sequence[q.index].ToString()}).ok,"valid QTE key");}Check(left>0,"ritual terminates");
        }
        static void Units(GameEngine g,int seat,string id,int count){for(int n=0;n<count;n++)g.State.players[seat].units.Add(new UnitState{uid="aura-"+(++seq),cardId=id,slot=n,hp=catalog.Card(id).health});}
        public static void Run()
        {
            seq=0;catalog=JsonUtility.FromJson<Catalog>(JsonUtility.ToJson(CardLibrary.LoadCatalog()));
            // Fixture provides the hypothetical QTE-2 creature in the user's 4+4+2 example.
            catalog.Card("C01").qte=2;
            var g=New(true);Play(g,"C02");Success(g);Play(g,"C02",1);Success(g);Play(g,"C01",2);Success(g);
            Check(g.State.qteSpent==10&&g.State.players[0].units.Count==3&&g.State.phase=="action","4+4+2 permits three creatures without ending turn");
            var extra=Give(g,"S01");Check(!MatchRules.CanUse(catalog,g.View(0,0),0,catalog.Card("S01"))&&!Send(g,0,new GameCommand{kind="play",cardUid=extra.uid,targetSeat=1}).ok,"UI and host reject budget overflow");
            Check(Send(g,0,new GameCommand{kind="end"}).ok,"end commanders turn");int safety=30;while(g.State.phase=="combat"&&safety-->0)g.Tick(g.State.deadline+.001);Check(g.State.qteSpent==0&&g.State.activeSeat==1,"budget resets only for next turn");
            g=New(true);var bad=Give(g,"C02");Check(!Send(g,0,new GameCommand{kind="play",cardUid=bad.uid,slot=-1}).ok&&g.State.qteSpent==0,"invalid slot costs nothing");
            Play(g,"C02");g.Tick(g.State.cast.revealUntil);var q=g.State.qte;g.Tick(q.deadline+.1);Check(g.State.qteSpent==4,"failed QTE is spent");
            g=New(true);Play(g,"S01");Success(g);Play(g,"C02");Success(g);Play(g,"S02",0,0);Success(g);Check(g.State.qteSpent==10,"spells and creatures share commanders budget");
            g=New(true);var response=Give(g,"R03",1);Play(g,"C02");Check(Send(g,1,new GameCommand{kind="react",cardUid=response.uid,phaseId=g.State.cast.id,targetSeat=0}).ok,"return reaction accepted");Success(g);Check(g.State.players[0].units.Count==0&&g.State.qteSpent==4,"returned summon still spends cost; reaction adds no cost");
            g=New();Play(g,"S01");Success(g);Play(g,"S01");Success(g);Check(!MatchRules.CanSpend(g.State,catalog.Card("C02"))&&MatchRules.CanSpend(g.State,catalog.Card("S01")),"second wizard spell locks mixed route");Play(g,"S01");Success(g);Check(!MatchRules.CanSpend(g.State,catalog.Card("S01")),"three wizard spells maximum");
            g=New();Play(g,"C02");Success(g);Play(g,"S01");Success(g);Check(!MatchRules.CanSpend(g.State,catalog.Card("S01"))&&!MatchRules.CanSpend(g.State,catalog.Card("C02")),"wizard creature plus one spell maximum");
            foreach(bool limited in new[]{true,false})
            {
                g=New(false,limited);Units(g,1,"C02",5);Play(g,"S01");Success(g);Check(g.State.players[1].hp==(limited?28:30),"five bears: reduction 2 or 5");
                g=New(false,limited);Units(g,0,"C03",5);Check(g.Attack(0,g.State.players[0].units[0])==catalog.Card("C03").attack+(limited?2:4),"attack aura excludes itself and obeys toggle");
                g=New(false,limited);Units(g,0,"C11",5);Play(g,"S01");Success(g);Check(g.State.players[1].hp==(limited?24:21),"spell power cap removed");
                g=New(false,limited,4);for(int n=1;n<4;n++)Units(g,n,"C05",5);Play(g,"S01");Check(g.State.qte.sequence.Length==(limited?5:18)&&g.State.qteSpent==3,"uncapped interference; printed QTE price");
                g=New(false,limited);Units(g,0,"C04",3);Play(g,"S01");g.Tick(g.State.cast.revealUntil);for(int i=0;i<3;i++){q=g.State.qte;Send(g,0,new GameCommand{kind="key",phaseId=q.id,key=q.sequence[q.index]=='A'?"S":"A"});}Check(g.State.qte.forgiven==(limited?1:3)&&g.State.qte.mistakes==(limited?2:0),"forgiveness stacks only without limit");
                g=New(false,limited);Units(g,1,"C13",5);Play(g,"S01");Success(g);Check(g.State.players[0].hp==(limited?28:25),"spell tax toggle");
            }
            var options=new MatchOptions{mode=MatchOptions.Commanders,cards3D=true,limitPower=false};
            g=new GameEngine(catalog,new[]{new LobbyMember{id="a"},new LobbyMember{id="b"}},1,0,options);options.limitPower=true;Check(!g.State.options.limitPower,"start snapshots lobby rules");
            var view=g.View(1,0);view.options.mode=MatchOptions.Wizards;Check(g.State.options.IsCommanders,"view cannot mutate host rules");
            var wire=JsonUtility.FromJson<WireMessage>(JsonUtility.ToJson(new WireMessage{state=g.View(1,0)}));Check(wire.protocol==10&&wire.state.options.IsCommanders&&wire.state.options.cards3D&&!wire.state.options.limitPower,"options survive private wire JSON");
            Check(!Send(g,0,new GameCommand{kind="options",choice="wizards"}).ok,"no in-match settings command");
            foreach(var card in catalog.cards)
            {
                Check(CardRulesText.For(card,new MatchOptions())==card.rules,"limited text unchanged "+card.id);
                string unlimited=CardRulesText.For(card,new MatchOptions{limitPower=false});Check(!unlimited.Contains("не выше +")&&!unlimited.Contains("Общая защита")&&!unlimited.Contains("Максимум 2"),"uncapped text "+card.id);
            }
            Check(CardRulesText.For(catalog.Card("S02"),new MatchOptions{limitPower=false}).Contains("не выше 30"),"HP ceiling is not a stacking cap");
            var baseCard=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Editable/Cards/CardBase.prefab");Check(baseCard.GetComponent<CardDepthVisual>()!=null,"depth inherited on card base");
            foreach(var card in Resources.Load<CardLibrary>("CardLibrary").cards)Check(card.GetComponent<CardDepthVisual>()!=null,"depth on all 30 variants");
            foreach(string name in new[]{"CardWindowUI","CardWindowWorld"}){var shader=Resources.Load<Shader>("Styles/"+name);Check(shader!=null&&!ShaderUtil.ShaderHasError(shader),"depth shader compiles "+name);}
            var world=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TableWorld.prefab").GetComponent<WorldHandFanSettings>();Check(world!=null&&world.cardSize.x>.65f&&world.sweep>=100,"authored larger physical fan");
            Directory.CreateDirectory("../output/tests");File.WriteAllText("../output/tests/rule-options-tests.txt","PASS commanders 4+4+2, mixed spending, rejection without charge, failure/return charging, free reactions, reset, wizard routes, capped/uncapped protection/attack/spell power/interference/forgiveness/tax, immutable lobby snapshots, protocol 9 privacy copy, no in-match mutation, dynamic descriptions, 30 inherited depth variants and compiled shaders.\n");Debug.Log("ALL_RULE_OPTIONS_TESTS_PASSED");
        }
    }
}
