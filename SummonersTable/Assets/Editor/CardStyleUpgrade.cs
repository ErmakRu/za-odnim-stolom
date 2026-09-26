using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace SummonersTable.Editor
{
    public static class CardStyleUpgrade
    {
        const string Output="../output/layered-cards-v7";
        static void Place(Transform t,float x,float y,float w,float h)=>SharedPresentationMigration.Place(t,x,y,w,h);
        static Image Fill(string name,Transform parent)
        {
            var existing=parent.Find(name);if(existing!=null)return existing.GetComponent<Image>();
            var image=SharedPresentationMigration.Add<Image>(name,parent,0,0,100,40);image.raycastTarget=false;return image;
        }
        static RectTransform Badge(string name,LayeredCardView v,CardStatIcon.Shape iconShape,out Text value)
        {
            var old=v.transform.Find(name);if(old!=null){value=old.Find("Value").GetComponent<Text>();return (RectTransform)old;}
            var border=SharedPresentationMigration.Add<BeveledImage>(name,v.transform,0,0,144,64);border.corner=13;border.raycastTarget=false;border.color=new Color(.84f,.69f,.4f);
            var body=SharedPresentationMigration.Add<BeveledImage>("Inset",border.transform,0,0,139,59);body.corner=11;body.raycastTarget=false;body.color=new Color(.028f,.05f,.065f,.98f);
            var icon=SharedPresentationMigration.Add<CardStatIcon>(iconShape.ToString(),border.transform,-36,0,48,48);icon.shape=iconShape;icon.raycastTarget=false;icon.color=iconShape==CardStatIcon.Shape.Sword?new Color(1,.12f,.2f):new Color(.12f,.86f,.77f);
            value=SharedPresentationMigration.Label("Value",border.transform,27,0,75,60,50);value.fontStyle=FontStyle.Bold;value.color=Color.white;value.resizeTextForBestFit=true;value.resizeTextMinSize=34;value.resizeTextMaxSize=50;
            return border.rectTransform;
        }
        [MenuItem("Summoners Table/Cards/Apply edge-to-edge card style")]
        public static void Apply()
        {
            LayeredCardData.Reload();var root=PrefabUtility.LoadPrefabContents(SharedPresentationMigration.FacePath);
            try
            {
                var v=root.GetComponent<LayeredCardView>();v.rulesFontSize=46;v.limitsFontSize=34;v.artTop=397;v.maximumArtHeight=374;v.minimumArtHeight=196;v.rulesBottom=-440;
                Place(v.titleFill.transform,0,431,590,68);((BeveledImage)v.titleFill).corner=19;((BeveledImage)v.titleFill).bevelBottom=false;
                Place(v.title.transform,0,431,566,62);v.title.fontSize=36;v.title.resizeTextMaxSize=36;
                v.artFrame.gameObject.SetActive(false);Place(v.artwork.transform,0,210,590,374);
                v.flavor.gameObject.SetActive(false);root.transform.Find("QTE backdrop").gameObject.SetActive(false);
                v.qteLabel.fontSize=28;v.qteLabel.alignment=TextAnchor.MiddleRight;
                v.factionFill=Fill("Species chip",root.transform);v.factionFill.transform.SetAsLastSibling();v.faction.transform.SetAsLastSibling();
                v.roleFill=Fill("Class chip",root.transform);v.roleFill.transform.SetAsLastSibling();v.role.transform.SetAsLastSibling();
                foreach(var text in new[]{v.faction,v.role})
                {
                    text.fontSize=30;text.fontStyle=FontStyle.Normal;text.alignment=TextAnchor.MiddleCenter;text.resizeTextForBestFit=true;text.resizeTextMaxSize=30;text.resizeTextMinSize=24;
                    foreach(var shadow in text.GetComponents<Shadow>())Object.DestroyImmediate(shadow);
                }
                foreach(var text in new[]{v.description,v.restrictions}){text.rectTransform.sizeDelta=new Vector2(548,400);text.resizeTextForBestFit=false;}
                v.description.fontSize=46;v.restrictions.fontSize=34;v.restrictions.fontStyle=FontStyle.Italic;
                v.attackBadge=Badge("Attack badge",v,CardStatIcon.Shape.Sword,out v.attackValue);
                v.healthBadge=Badge("Health badge",v,CardStatIcon.Shape.Heart,out v.healthValue);
                foreach(var icon in root.GetComponentsInChildren<CardStatIcon>(true))if(icon.GetComponent<CanvasRenderer>()==null)icon.gameObject.AddComponent<CanvasRenderer>();
                v.stats.enabled=false;v.statsBackground.color=Color.clear;
                v.ApplyCard(LayeredCardData.Current.Catalog().Card("C08"),LayeredCardData.Current.Catalog(),null);v.cardName="";
                // The base is data-free; instances get every gameplay value from JSON.
                foreach(var text in new[]{v.title,v.description,v.restrictions,v.faction,v.role,v.stats,v.attackValue,v.healthValue,v.flavor})text.text="";
                PrefabUtility.SaveAsPrefabAsset(root,SharedPresentationMigration.FacePath);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
            const string slotsPath="Assets/Prefabs/CardTableCanvas.prefab";root=PrefabUtility.LoadPrefabContents(slotsPath);
            try{foreach(var slot in root.GetComponentsInChildren<CardDisplaySlot>(true))foreach(var graphic in slot.GetComponents<Graphic>())Object.DestroyImmediate(graphic);PrefabUtility.SaveAsPrefabAsset(root,slotsPath);}finally{PrefabUtility.UnloadPrefabContents(root);}
            const string previewPath="Assets/LayeredCards/Prefabs/LayeredCardsPreview.prefab";root=PrefabUtility.LoadPrefabContents(previewPath);
            try
            {
                root.GetComponent<LayeredCardsDemo>().previewQteCosts=Array.Empty<int>();
                root.transform.Find("Preview heading").GetComponent<Text>().text="НОВАЯ ОСНОВА · ПЕРЕЛИВАЮЩИЕСЯ КАРТЫ";
                root.transform.Find("Instructions").GetComponent<Text>().text="Вампир на диете  /  Медведь-обниматель  /  Огненный чих";
                var note=root.transform.Find("Cost preview note");if(note!=null)note.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root,previewPath);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("CARD_STYLE_APPLIED");
        }
        public static void Verify()
        {
            var bundle=LayeredCardData.Current;var catalog=bundle.Catalog();int faces=0;
            void Check(bool ok,string why){if(!ok)throw new Exception("CARD_STYLE: "+why);}
            foreach(bool limited in new[]{true,false})
            {
                CardPresentationContext.Apply(new MatchOptions{limitPower=limited});
                foreach(var card in catalog.cards)
                {
                    var view=Object.Instantiate(AssetDatabase.LoadAssetAtPath<CardView>("Assets/Prefabs/Editable/Cards/Instances/"+card.id+".prefab"));
                    try
                    {
                        view.Import(card,catalog);
                        foreach(var face in new[]{view.sharedFull,view.sharedCompact,view.sharedWorld})
                        {
                            Check(face.titleFill.rectTransform.rect.width==590&&face.artwork.rectTransform.rect.width==590,"full width "+card.id);
                            Check(!face.flavor.gameObject.activeSelf,"flavor hidden "+card.id);
                            Check(face.description.rectTransform.anchoredPosition.y-face.RulesHeight>=face.rulesBottom-1,"text fits "+card.id+" / "+limited);
                            Check(face.description.fontSize>=44&&face.restrictions.fontSize<face.description.fontSize,"text hierarchy "+card.id);
                            bool creature=card.kind=="creature";Check(face.attackBadge.gameObject.activeSelf==creature&&face.healthBadge.gameObject.activeSelf==creature,"stats visibility "+card.id);
                            Check(face.accent.gameObject.activeSelf==!creature&&face.type.gameObject.activeSelf==!creature,"spell/reaction type band "+card.id);
                            if(!creature)Check(face.accent.color==face.titleFill.color&&face.accent.rectTransform.rect.width==face.titleFill.rectTransform.rect.width,"full type band matches title "+card.id);
                            if(creature)
                            {
                                Check(face.attackValue.text==card.attack.ToString()&&face.healthValue.text==card.health.ToString(),"stat values "+card.id);
                                Check(face.GetComponentsInChildren<CardStatIcon>().Length==2&&face.GetComponentsInChildren<CardStatIcon>().All(i=>i.GetComponent<CanvasRenderer>()!=null),"icons render "+card.id);
                                Check(face.faction.transform.GetSiblingIndex()>face.factionFill.transform.GetSiblingIndex()&&face.role.transform.GetSiblingIndex()>face.roleFill.transform.GetSiblingIndex(),"chip labels render above fills "+card.id);
                                ColorUtility.TryParseHtmlString(catalog.factionColors.Single(c=>c.name==card.faction).hex,out var faction);ColorUtility.TryParseHtmlString(catalog.roleColors.Single(c=>c.name==card.role).hex,out var role);
                                Check(face.factionFill.color==faction&&face.roleFill.color==role,"JSON colours "+card.id);
                            }
                            var cube=face.qteSymbols[0].rectTransform;Check(Mathf.Abs(cube.anchoredPosition.x-cube.rect.width*.5f+295-14)<.01f,"half-square inset");
                            var label=face.qteLabel.rectTransform;Check(Mathf.Abs(295-label.anchoredPosition.x-label.rect.width*.5f-7)<.01f,"QTE right inset");
                            foreach(int cost in new[]{0,3,4,6,8,12,20}){face.SetQteCost(cost);Check(face.qteSymbols.Count(s=>s.gameObject.activeSelf)==cost&&face.qteSymbols.Where(s=>s.gameObject.activeSelf).All(s=>s.color==CardView.GetQteColor(cost)),"QTE colour row");}
                            faces++;
                        }
                    }finally{Object.DestroyImmediate(view.gameObject);}
                }
            }
            CardPresentationContext.Apply(new MatchOptions());
            var canvas=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CardTableCanvas.prefab");
            foreach(var slot in canvas.GetComponentsInChildren<CardDisplaySlot>(true))Check(slot.GetComponents<Graphic>().Length==0,"no slot backplate "+slot.name);
            Directory.CreateDirectory("../output/tests");File.WriteAllText("../output/tests/card-style-v7.txt",$"PASS {faces} card faces, all 30 cards in full/hand/world and both limit modes; edge-to-edge title/art; JSON chips; vector stats; text fits; hidden flavor; QTE inset and colours; no cast/reaction slot backplates.\n");
            Debug.Log("CARD_STYLE_VERIFY_PASS "+faces);
        }
        public static void RenderExamples()
        {
            Directory.CreateDirectory(Output);Capture("new-card-style.png",new[]{"C08","C02","S01"},false);Capture("reaction-overlay.png",new[]{"C03","R02"},true);
        }
        static void Capture(string file,string[] ids,bool overlay)
        {
            var cameraObject=new GameObject("Card style camera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.05f,.055f);camera.nearClipPlane=.01f;camera.farClipPlane=50;camera.transform.position=new Vector3(0,0,-10);camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            var target=new RenderTexture(1600,1080,24);target.Create();camera.targetTexture=target;camera.aspect=1600f/1080;
            var root=new GameObject("New cards",typeof(RectTransform),typeof(Canvas));var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            var previous=RenderTexture.active;var bundle=LayeredCardData.Current;var catalog=bundle.Catalog();
            try
            {
                for(int i=0;i<ids.Length;i++)
                {
                    if(overlay)
                    {
                        var rect=CardTableCanvas.Rect("Played card "+i,root.transform,new Vector2(i==0?-90:130,i==0?20:-90),new Vector2(430,750));var slot=rect.gameObject.AddComponent<CardDisplaySlot>();slot.Show(catalog.Card(ids[i]),catalog,null);rect.localScale=Vector3.one*(i==0?1:.72f);rect.localRotation=Quaternion.Euler(0,0,i==0?0:-20);
                    }
                    else
                    {
                        var face=Object.Instantiate(AssetDatabase.LoadAssetAtPath<LayeredCardView>(SharedPresentationMigration.FacePath),root.transform);face.runtimeMode=true;var card=catalog.Card(ids[i]);face.ApplyCard(card,catalog,bundle.layeredCards.Find(card.name));face.SetGameArt(true,Vector2.zero,null);
                        var rect=(RectTransform)face.transform;rect.anchoredPosition=new Vector2((i-1)*510,0);rect.localScale=Vector3.one*.80f;
                    }
                }
                foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
                Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});RenderTexture.active=target;
                var texture=new Texture2D(1600,1080,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,1080),0,0);texture.Apply();File.WriteAllBytes(Output+"/"+file,texture.EncodeToPNG());Object.DestroyImmediate(texture);
            }finally{Object.DestroyImmediate(root);camera.targetTexture=null;RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(cameraObject);}
        }
        public static void Run(){Apply();Verify();CardReadabilityUpgrade.Verify();CardCompositionReview.Verify();LayeredCardsTests.Run();LayeredMotionTests.Run();PresentationRevisionTests.Run();RenderExamples();LayeredCardsAuthoring.Render();Debug.Log("CARD_STYLE_COMPLETE");}
    }
}
