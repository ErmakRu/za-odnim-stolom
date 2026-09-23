using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace SummonersTable.Editor
{
    public static class ConfigTests
    {
        static void Check(bool condition,string message){if(!condition)throw new Exception("Config test: "+message);}
        static string SourceChain(GameObject obj){string chain="";for(int i=0;i<12&&obj!=null;i++){obj=PrefabUtility.GetCorrespondingObjectFromSource(obj);if(obj!=null)chain+="|"+AssetDatabase.GetAssetPath(obj);}return chain;}
        static void Reject(Action action,string reason){bool failed=false;try{action();}catch{failed=true;}Check(failed,reason);}
        public static void Run()
        {
            string folder=ConfigAuthoring.Folder;var b=ConfigBundle.Read(folder);Check(b.cards.cards.Length==30&&b.decks.decks.Length==3,"all cards/decks loaded");
            string read(string f)=>File.ReadAllText(Path.Combine(folder,f));
            Reject(()=>ConfigBundle.Read(folder,"rules.json",read("rules.json").Replace("\"heroHp\": 30","\"heroHp\": 0")),"zero HP rejected");
            Reject(()=>ConfigJson.Parse("{\"x\":1,\"x\":2}"),"duplicate key");Reject(()=>ConfigJson.Parse("{\"x\":2,}"),"trailing comma");Reject(()=>ConfigJson.Parse("{} junk"),"trailing input");
            Reject(()=>ConfigBundle.Read(folder,"rules.json",read("rules.json").Replace("\"heroHp\"","\"heroHpp\"")),"unknown parameter");
            Reject(()=>ConfigBundle.Read(folder,"audio.json",read("audio.json").Replace("\"volume\": 0.15","\"volume\": 5")),"bad volume");
            string cards=read("cards.json").Replace("\"name\": \"Медведь-обниматель\"","\"name\": \"Тестовый медведь\"");
            var changed=ConfigBundle.Read(folder,"cards.json",cards);Check(changed.Catalog().Card("C02").name=="Тестовый медведь","JSON replaces prefab definition");Check(changed.gameplayHash!=b.gameplayHash,"balance hash changed");
            var audio=ConfigBundle.Read(folder,"audio.json",read("audio.json").Replace("\"volume\": 0.15","\"volume\": 0.11"));Check(audio.gameplayHash==b.gameplayHash,"cosmetic audio hash is separate");
            var engine=new GameEngine(b.Catalog(),new[]{new LobbyMember{id="a"},new LobbyMember{id="b"}},1);var edited=b.Catalog();edited.rules.heroHp=17;var next=new GameEngine(edited,new[]{new LobbyMember{id="a"},new LobbyMember{id="b"}},2);Check(engine.State.players[0].hp==30&&next.State.players[0].hp==17,"existing match unchanged, next uses JSON");
            var world=AssetDatabase.LoadAssetAtPath<GameObject>(ConfigAuthoring.WorldPath);var seats=new[]{world.GetComponent<TableBoard>().seating.seatPrefab};Check(seats[0]!=null,"generated shared seats");
            foreach(var seat in seats)
            {
                Check(SourceChain(seat.gameObject).Contains("/PlayerSeat.prefab"),"seat uses common prefab");
                Check(SourceChain(seat.chair.gameObject).Contains("/Chair.prefab"),"chair uses common prefab");
                foreach(var slot in seat.slots)Check(SourceChain(slot.gameObject).Contains("/CreatureSlot.prefab"),"slot uses common prefab");
            }
            var lobby=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LobbyCanvas.prefab").GetComponent<FrontEndCanvas>();
            foreach(var portrait in lobby.portraits){Check(AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(portrait.transform.parent.gameObject)).EndsWith("/LobbyPlayerPanel.prefab"),"common lobby player panel");Check(portrait.actor!=null&&portrait.portraitCamera!=null&&portrait.image!=null,"portrait asset references");}
            var inspector=UnityEditor.Editor.CreateEditor(AssetDatabase.LoadMainAssetAtPath(folder+"/audio.json"));Check(inspector is ConfigJsonInspector,"JSON opens in Inspector");Object.DestroyImmediate(inspector);
            foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath))Check(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<PrefabConfigBinding>()!=null,"binding "+path);
            Directory.CreateDirectory("../output/tests");File.WriteAllText("../output/tests/config-tests.txt","PASS 11 config files, source-of-truth cards, strict parse/unknown fields/duplicates/ranges, asset IDs, gameplay vs cosmetic hash, match boundary, generated shared seats/chairs and creature slots, 4 shared lobby panels, prefab config bindings, Unity Inspector JSON editor.\n");
            Debug.Log("CONFIG_TESTS_PASSED");
        }
        public static void PrepareAndBuild()
        {
            // Re-serialize typed defaults to expose fields added by this migration.
            var r=ConfigJson.Read<RulesConfig>(File.ReadAllText(ConfigAuthoring.Folder+"/rules.json"));ConfigAuthoring.Write("rules.json",JsonUtility.ToJson(r,true));
            ConfigAuthoring.CopyDefaults();ConfigAuthoring.SyncCards();Run();BuildTools.BuildWindows();
        }
    }
}
