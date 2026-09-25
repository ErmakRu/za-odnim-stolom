using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace SummonersTable.Editor
{
    public static class CampaignTests
    {
        public static void Run()
        {
            var bundle=ConfigBundle.Read(ConfigAuthoring.Folder);var catalog=bundle.Catalog();var book=CampaignBook.Load(catalog);
            void Check(bool ok,string reason){if(!ok)throw new Exception("CAMPAIGN: "+reason);}
            Check(book.chapters.Length==17,"17 duels");
            var all=book.introduction.Concat(book.chapters.SelectMany(c=>c.before.Concat(c.after))).Concat(book.ending).ToArray();
            var source=JsonUtility.FromJson<SummonersTable.Story.StoryConfig>(File.ReadAllText("Assets/StreamingAssets/Config/story.json"));
            Check(all.SelectMany(f=>f.lines).Select(l=>l.sourceId).OrderBy(x=>x).SequenceEqual(source.scenes.SelectMany(s=>s.steps).Select(s=>s.id).OrderBy(x=>x)),"every source line exactly once");
            var invalid=ConfigBundle.Clone(book);invalid.chapters[0].opponentDeck="missing";bool rejected=false;try{invalid.Validate(catalog);}catch(FormatException){rejected=true;}Check(rejected,"unknown deck rejected");
            var prefab=AssetDatabase.LoadAssetAtPath<CampaignComicView>(CampaignAuthoring.Prefab);Check(prefab!=null&&prefab.left!=null&&prefab.right!=null&&prefab.next!=null&&prefab.media!=null,"reusable wired prefab");
            var view=Object.Instantiate(prefab);int advances=0;book.charactersPerSecond=0;view.Bind(book);view.nextAction=()=>advances++;
            try
            {
                foreach(var frame in all)foreach(var line in frame.lines)
                {
                    view.Present(frame,line,frame.title,"",true,false,"Начать бой");Canvas.ForceUpdateCanvases();
                    Check(view.body.text==line.text,"text presentation");
                    float required=view.body.cachedTextGeneratorForLayout.GetPreferredHeight(line.text,view.body.GetGenerationSettings(view.body.rectTransform.rect.size))/view.body.pixelsPerUnit;
                    Check(required<=view.body.rectTransform.rect.height+2,"text overflow at "+line.sourceId+": "+required);
                }
                view.next.onClick.Invoke();Check(advances==1,"next button fires once");
                book.charactersPerSecond=65;view.Present(all[0],all[0].lines[0],"","",false,false,"");view.next.onClick.Invoke();Check(advances==1&&!view.Typing,"first click reveals text");view.next.onClick.Invoke();Check(advances==2,"second click advances");
            }finally{Object.DestroyImmediate(view.gameObject);}
            string old=CampaignProgress.TestPath;CampaignProgress.TestPath=Path.GetFullPath("../tmp/campaign-tests/save.json");
            try{var save=new CampaignProgress{chapter=7,phase="after",deck=catalog.decks[1].id,frame=0,line=0};save.Save();var loaded=CampaignProgress.Read(book);Check(loaded.chapter==7&&loaded.phase=="after"&&loaded.deck==save.deck,"checkpoint roundtrip");}
            finally{CampaignProgress.TestPath=old;}
            int actions=0;
            foreach(var c in book.chapters)
            {
                var members=new[]{new LobbyMember{id="test-jester",name="Йорик",deckId=book.defaultDeck,isBot=true},new LobbyMember{id=c.id,name=c.opponentName,deckId=c.opponentDeck,isBot=true,heroId=c.opponentHero}};
                var g=new GameEngine(catalog,members,c.seed,0,new MatchOptions{mode=c.mode});var bots=ConfigBundle.Clone(bundle.bots);bots.autoRematch=false;var director=new BotDirector(g,bots,c.seed);double time=0;
                for(int i=0;i<40000&&g.State.phase!="matchEnd";i++){time+=.25;g.Tick(time);director.Tick(g,time);}
                Check(g.State.phase=="matchEnd","duel finishes "+c.id);Check(director.Rejected==0,"legal bot commands "+c.id+": "+director.LastError);actions+=director.Accepted;
            }
            Directory.CreateDirectory("../output/tests");string report=$"PASS: {book.chapters.Length} complete campaign duels; {all.Length} comic frames; {all.Sum(f=>f.lines.Length)} original source lines, no omissions/duplicates; every line fits; all assets resolve; click/typewriter; checkpoint reload; invalid deck rejection; {actions} legal bot commands, zero rejected.\n";
            File.WriteAllText("../output/tests/campaign.txt",report);Debug.Log(report);
        }
    }
}
