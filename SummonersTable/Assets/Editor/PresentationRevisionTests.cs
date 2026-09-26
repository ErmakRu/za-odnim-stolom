using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace SummonersTable.Editor
{
    public static class PresentationRevisionTests
    {
        public static void Run()
        {
            void Check(bool ok,string message){if(!ok)throw new Exception("PRESENTATION REVISION: "+message);}
            var bundle=ConfigBundle.Read(ConfigAuthoring.Folder);var catalog=bundle.Catalog();var book=CampaignBook.Load(catalog);
            var frames=book.introduction.Concat(book.chapters.SelectMany(c=>c.before.Concat(c.after))).Concat(book.ending).ToArray();
            int pageCount=0;
            var prefab=AssetDatabase.LoadAssetAtPath<CampaignComicView>(CampaignAuthoring.Prefab);var view=Object.Instantiate(prefab);view.Bind(book);
            try
            {
                foreach(var frame in frames)foreach(var line in frame.lines)
                {
                    view.Present(frame,line,"Title","Counter",true,false,"Battle");
                    bool narrator=book.IsNarrator(line),hero=book.Character(line)?.type=="main";
                    Check(view.left.gameObject.activeSelf==(!narrator&&hero)&&view.right.gameObject.activeSelf==(!narrator&&!hero),"only current speaker: "+line.sourceId);
                    Check(view.namePanel.gameObject.activeSelf==!narrator,"narrator has no name plate");
                    var pages=ComicText.Split(line.text,100);Check(string.Join(" ",pages)==System.Text.RegularExpressions.Regex.Replace(line.text,@"\s+"," ").Trim(),"no text dropped "+line.sourceId);
                    foreach(var page in pages)Check(page.Length<=100||page.Skip(100).All(char.IsPunctuation),"page length "+line.sourceId);
                    for(int i=0;i<view.SegmentCount;i++){view.UseSegment(i);Check(view.body.text==""&&view.Typing,"every fragment types from zero");view.Reveal();Canvas.ForceUpdateCanvases();Check(view.body.preferredHeight<=view.body.rectTransform.rect.height+1,"text fits "+line.sourceId);pageCount++;}
                }
                var thought=new ComicLine{speaker="ШУТ",characterId="jester",kind="thought",text="Первая мысль. Следующая мысль."};view.Present(frames[0],thought,"","",false,false,"");
                Check(view.body.fontStyle==FontStyle.Italic&&view.FullText.StartsWith("«")&&view.FullText.EndsWith("»"),"thought style");
                view.PointerClick(1);view.PointerClick(2);Check(view.Segment==1&&view.Typing&&view.body.text=="","double click starts exactly next fragment");
                thought.rewardIcon="Art/card_back";view.Present(frames[0],thought,"","",false,false,"");Check(view.rewardRoot.activeSelf,"item icon appears");Check(view.rewardIcon.rectTransform.rect.width<=169&&view.rewardIcon.rectTransform.rect.height<=205,"item fits above its label");
                thought.rewardIcon="";view.Present(frames[0],thought,"","",false,false,"");Check(!view.rewardRoot.activeSelf,"item icon clears");
                foreach(var button in view.GetComponentsInChildren<Button>(true))Check(button.GetComponent<SharedButton>()!=null,"comic button uses common prefab: "+button.name);
            }finally{Object.DestroyImmediate(view.gameObject);}
            int cards=0;
            foreach(var card in catalog.cards)
            {
                var asset=AssetDatabase.LoadAssetAtPath<CardView>("Assets/Prefabs/Editable/Cards/Instances/"+card.id+".prefab");var v=Object.Instantiate(asset);
                try
                {
                    v.Import(card,catalog);v.Mode("compact");
                    foreach(var face in new[]{v.sharedFull,v.sharedCompact,v.sharedWorld})
                    {
                        Check(face!=null,"shared face "+card.id);Check(face.title.text==card.name,"name on top");
                        Check(face.type.text==catalog.typeColors.First(t=>t.id==card.kind).name.ToUpperInvariant(),"visible type");
                        Check(face.accent.rectTransform.rect.width==face.titleFill.rectTransform.rect.width,"equal band widths");
                        Check(face.accent.rectTransform.rect.height<=face.titleFill.rectTransform.rect.height,"type band not taller");
                        Check(face.description.text.Length>0,"rules even in hand "+card.id);
                        Check(face.stats.gameObject.activeSelf==(card.kind=="creature")&&face.statsBackground.gameObject.activeSelf==(card.kind=="creature"),"conditional stats");
                        Check(face.GetComponent<Mask>()!=null&&face.GetComponent<BeveledImage>()!=null,"styled card silhouette");
                        face.SetGameArt(false,Vector2.zero,null);var uv=face.artwork.uvRect;var texture=face.artwork.texture;
                        Check(Mathf.Abs(texture.width*uv.width/(texture.height*uv.height)-face.artwork.rectTransform.rect.width/face.artwork.rectTransform.rect.height)<.001f,"flat art preserves proportions "+card.id);
                        float total=face.description.preferredHeight+(face.restrictions.gameObject.activeSelf?face.restrictions.preferredHeight+18:0);Check(total<=302,"rules fit "+card.id+" "+total);
                    }
                    Check(v.sharedFull.title.text==v.sharedCompact.title.text&&v.sharedFull.description.text==v.sharedCompact.description.text,"same content in hand/inspection");cards++;
                }finally{Object.DestroyImmediate(v.gameObject);}
            }
            foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/Editable/UI","Assets/Campaign/Resources","Assets/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath).Distinct())
            {
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach(var screen in asset.GetComponentsInChildren<WidgetScreen>(true))Check(screen.bindings.All(b=>b.widget!=null),"widget bindings intact: "+path);
                foreach(var button in asset.GetComponentsInChildren<Button>(true))Check(button.GetComponent<SharedButton>()!=null,"common button: "+path+"/"+button.name);
            }
            foreach(var name in new[]{"MainMenuCanvas","LobbyCanvas"})
            {
                var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/"+name+".prefab");var front=root.GetComponent<FrontEndCanvas>();
                Check(front.buttons.All(b=>b!=null)&&front.buttons.Length==front.actions.Length,"all front-end actions wired "+name);
                if(front.lobby)Check(front.seatControls.Length==20&&front.seatControls.All(b=>b!=null),"all seat controls wired");
                foreach(var button in root.GetComponentsInChildren<Button>(true))Check(button.GetComponent<SharedButton>()!=null,"shared lobby/menu button "+button.name);
            }
            Directory.CreateDirectory("../output/tests");File.WriteAllText("../output/tests/presentation-revision.txt",$"PASS {frames.Sum(f=>f.lines.Length)} lines/{pageCount} fragments, speaker visibility/role colours/narrator, quoted italic thoughts, double-click, reward icon, {cards} cards with shared full/hand/world faces, visible descriptions, stats, type/name bands and styled corners; shared comic/lobby/menu buttons.\n");
            Debug.Log("PRESENTATION_REVISION_PASS");
        }
        public static void RenderCards(){Run();LayeredCardsTests.Run();LayeredCardsAuthoring.Render();}
    }
}
