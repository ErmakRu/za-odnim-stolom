using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace SummonersTable.Editor
{
    public static class LayeredCardsTests
    {
        static int count;
        static void Check(bool ok,string why){count++;if(!ok)throw new Exception("Layered cards test: "+why);}
        public static void Run()
        {
            count=0;var bundle=ConfigBundle.Read(ConfigAuthoring.Folder);
            var basePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(LayeredCardsAuthoring.Root+"/Prefabs/LayeredCardBase.prefab");
            Check(basePrefab.GetComponent<LayeredCardView>().cardName=="","base stores no card name/data");
            Check(basePrefab.GetComponent<CardView>()==null,"base does not embed legacy CardDef");
            foreach(string id in new[]{"S01","C02","C08"})
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(LayeredCardsAuthoring.Root+"/Prefabs/Cards/"+id+".prefab");
                var parent=PrefabUtility.GetCorrespondingObjectFromSource(prefab);Check(parent!=null&&AssetDatabase.GetAssetPath(parent).Contains("/Types/"),"card inherits type");
                Check(PrefabUtility.GetCorrespondingObjectFromSource(parent)==basePrefab,"type inherits base");
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);var view=go.GetComponent<LayeredCardView>();
                try
                {
                    view.Apply(bundle);var card=bundle.Catalog().Card(id);
                    CardRulesText.Split(card,CardPresentationContext.Options,out var rules,out var limits);
                    Check(view.title.text==card.name&&view.description.text==rules&&view.restrictions.text==limits&&view.flavor.text==card.flavor,"text comes from cards.json");
                    Check(view.qteSymbols.Count(s=>s.gameObject.activeSelf)==card.qte,"QTE symbols count");
                    Check(view.ArtMaterial.shader.isSupported,"UI shader supported");
                    view.SetLook(new Vector2(.2f,-.3f));var masked=view.artwork.materialForRendering;
                    Check(masked.GetVector("_Subject").z>0&&masked.GetTexture("_RearTex")!=null,"styled corner mask preserves layer settings");
                    Check(Mathf.Abs(masked.GetVector("_ViewOffset").x-.2f)<.001f,"masked art follows card movement");
                    var changed=ConfigBundle.Read(ConfigAuthoring.Folder);changed.cards.cards.First(c=>c.id==id).qte=7;
                    changed.cards.cards.First(c=>c.id==id).rules="Правило из JSON";view.Apply(changed);
                    Check(view.qteSymbols.Count(s=>s.gameObject.activeSelf)==7&&view.description.text=="Правило из JSON","data can change without prefab edits");
                }finally{Object.DestroyImmediate(go);}
            }
            var draft=ConfigJson.Read<LayeredCardsConfig>(File.ReadAllText(ConfigAuthoring.Folder+"/layered-cards.json"));draft.cards[0].subject.rear.offsetX=.21f;
            var cosmetic=ConfigBundle.Read(ConfigAuthoring.Folder,"layered-cards.json",JsonUtility.ToJson(draft));Check(cosmetic.gameplayHash==bundle.gameplayHash,"cosmetic edits do not change gameplay hash");
            string testFolder=Path.GetFullPath("../tmp/layered-config-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(testFolder);
            foreach(string file in ConfigBundle.Files)File.Copy(Path.Combine(ConfigAuthoring.Folder,file),Path.Combine(testFolder,file));
            string oldRules=File.ReadAllText(Path.Combine(testFolder,"rules.json"));
            ManagerStorage.SaveSection("layered-cards.json",JsonUtility.ToJson(draft,true),testFolder);
            Check(ConfigBundle.Read(testFolder).layeredCards.cards[0].subject.rear.offsetX==.21f,"Inspector save pipeline persists sliders");
            var main=ConfigJson.Object(ConfigJson.Parse(File.ReadAllText(Path.Combine(testFolder,"main.json"))));
            Check(ConfigJson.Object(main["managers"]).ContainsKey("layered-cards")&&File.ReadAllText(Path.Combine(testFolder,"rules.json"))==oldRules,"save updates main and leaves balance intact");
            draft.cards[0].subject.rear.zoom=0;Reject(()=>draft.Validate(bundle.Catalog()),"zero zoom rejected");
            string saved=File.ReadAllText(Path.Combine(testFolder,"layered-cards.json"));Reject(()=>ManagerStorage.SaveSection("layered-cards.json",JsonUtility.ToJson(draft),testFolder),"invalid save rejected");
            Check(File.ReadAllText(Path.Combine(testFolder,"layered-cards.json"))==saved,"invalid save is atomic");draft.cards[0].subject.rear.zoom=1;
            draft.cards[0].cardName="Missing card";Reject(()=>draft.Validate(bundle.Catalog()),"unknown name rejected");
            var oldOptions=CardPresentationContext.Options.Copy();
            try
            {
                ConfigRuntime.LoadInitial();CardPresentationContext.Apply(new MatchOptions{cards3D=true});
                var legacy=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Editable/Cards/Instances/S01.prefab"));
                try
                {
                    var view=legacy.GetComponent<CardView>();view.Import(bundle.Catalog().Card("S01"),bundle.Catalog());view.Mode("full");
                    var visual=legacy.GetComponent<CardDepthVisual>();visual.SendMessage("Awake");var old=view.fullArtwork.material;
                    visual.SendMessage("LateUpdate");Check(view.fullArtwork.material.shader.name=="SummonersTable/Layered Card UI","game full/hand uses combined planes");
                    view.Mode("world");visual.SendMessage("LateUpdate");Check(view.sharedWorld.ArtMaterial.shader.name=="SummonersTable/Layered Card UI","world uses same face and combined planes");view.Mode("full");
                    CardPresentationContext.Apply(new MatchOptions{cards3D=false});visual.SendMessage("LateUpdate");Check(view.fullArtwork.material==old,"3D off restores original material");
                }finally{Object.DestroyImmediate(legacy);}
            }finally{CardPresentationContext.Apply(oldOptions);}
            Directory.CreateDirectory("../output/tests");File.WriteAllText("../output/tests/layered-cards.txt","PASS "+count+" checks: base/type/card prefab inheritance; no CardDef on base; name lookup; dynamic text/stats/QTE; strict JSON; cosmetic hash; existing UI/world combined-plane integration and restoration.\n");
            Debug.Log("LAYERED_TESTS_PASS "+count);
        }
        static void Reject(Action action,string why){bool rejected=false;try{action();}catch(FormatException){rejected=true;}Check(rejected,why);}
    }
}
