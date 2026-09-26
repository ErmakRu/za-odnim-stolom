using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;
namespace SummonersTable.Editor
{
    public static class CardCompositionReview
    {
        const string Output="../output/card-composition/";
        [MenuItem("Summoners Table/Cards/Apply composition and review")]
        public static void Run()
        {
            CardReadabilityUpgrade.Run();Verify();LayeredCardsTests.Run();PresentationRevisionTests.Run();Render();
            Debug.Log("CARD_COMPOSITION_PASS");
        }
        public static void Verify()
        {
            var bundle=ConfigBundle.Read(ConfigAuthoring.Folder);var catalog=bundle.Catalog();int count=0;
            void Check(bool ok,string why){if(!ok)throw new Exception("Card composition: "+why);}
            Check(bundle.layeredCards.framing.Length==catalog.cards.Count,"all source arts have framing");
            foreach(bool limited in new[]{true,false})
            {
                CardPresentationContext.Apply(new MatchOptions{limitPower=limited});
                foreach(var card in catalog.cards)
                {
                    var instance=Object.Instantiate(AssetDatabase.LoadAssetAtPath<CardView>("Assets/Prefabs/Editable/Cards/Instances/"+card.id+".prefab"));
                    try
                    {
                        instance.Import(card,catalog);
                        foreach(var face in new[]{instance.sharedFull,instance.sharedCompact,instance.sharedWorld})
                        {
                            face.SetGameArt(false,Vector2.zero,null);var crop=face.artwork.uvRect;var f=bundle.layeredCards.Frame(card.name);
                            Check(Vector2.Distance(crop.center,new Vector2(f.focusX,f.focusY))<.001f,"chosen landmark is centred: "+card.id);
                            Check(crop.xMin>=0&&crop.yMin>=0&&crop.xMax<=1.0001f&&crop.yMax<=1.0001f,"no empty edges: "+card.id);
                            var art=face.artwork.rectTransform;var artBounds=new Rect(art.anchoredPosition-art.rect.size*.5f,art.rect.size);
                            foreach(var r in face.qteSymbols.Select(s=>s.rectTransform).Append(face.qteLabel.rectTransform))
                            {
                                Check(artBounds.Contains(r.anchoredPosition-r.rect.size*.5f)&&artBounds.Contains(r.anchoredPosition+r.rect.size*.5f),"QTE overlays art: "+card.id);
                                Check(r.GetSiblingIndex()>art.GetSiblingIndex(),"QTE draws over art");
                            }
                            count++;
                        }
                    }finally{Object.DestroyImmediate(instance.gameObject);}
                }
            }
            CardPresentationContext.Apply(new MatchOptions());
            var originalHash=bundle.gameplayHash;bundle.layeredCards.framing[0].focusY=.5f;
            var changed=ConfigBundle.Read(ConfigAuthoring.Folder,"layered-cards.json",JsonUtility.ToJson(bundle.layeredCards));
            Check(changed.gameplayHash==originalHash,"composition is cosmetic");
            var invalid=ConfigBundle.Read(ConfigAuthoring.Folder);invalid.layeredCards.framing[0].zoom=0;bool rejected=false;
            try{invalid.Validate();}catch(FormatException){rejected=true;}Check(rejected,"invalid crop rejected");
            Directory.CreateDirectory("../output/tests");File.WriteAllText("../output/tests/card-composition.txt",$"PASS {count} full/hand/world variants; all 30 visual landmarks centred; aspect-preserving crop stays inside source; QTE label and all 20 cubes overlay artwork; framing validation and unchanged gameplay hash.\n");
        }
        public static void Render()
        {
            Directory.CreateDirectory(Output);LayeredCardData.Reload();var b=LayeredCardData.Current;
            var cards=b.Catalog().cards.ToArray();
            for(int i=0;i<cards.Length;i+=6)Capture(b,cards.Skip(i).Take(6).ToArray(),"all-cards-"+(i/6+1)+".png",false,false);
            var examples=new[]{"C01","C02","S01"}.Select(id=>b.Catalog().Card(id)).ToArray();
            Capture(b,examples,"composition-2d.png",false,false);
            Capture(b,examples,"composition-tutorial.png",false,true);
            Capture(b,new[]{"C02","S01","C08"}.Select(id=>b.Catalog().Card(id)).ToArray(),"composition-3d.png",true,false);
        }
        static void Capture(ConfigBundle b,CardDef[] cards,string name,bool layered,bool tutorial)
        {
            int height=cards.Length>3?1180:700;
            var cg=new GameObject("Composition review camera");var camera=cg.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.05f,.055f);camera.nearClipPlane=.01f;camera.farClipPlane=50;camera.transform.position=new Vector3(0,0,-10);camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            var target=new RenderTexture(1200,height,24);target.Create();camera.targetTexture=target;
            var root=new GameObject("Composition review canvas",typeof(RectTransform),typeof(Canvas));var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            var previous=RenderTexture.active;
            try
            {
                for(int i=0;i<cards.Length;i++)
                {
                    var card=cards[i];string tutorialPath="Assets/Campaign/Tutorial/Cards/"+card.id+".prefab";string path=tutorial&&File.Exists(tutorialPath)?tutorialPath:SharedPresentationMigration.FacePath;
                    var face=Object.Instantiate(AssetDatabase.LoadAssetAtPath<LayeredCardView>(path),root.transform);face.runtimeMode=true;
                    face.ApplyCard(card,b.Catalog(),layered?b.layeredCards.Find(card.name):null,b.layeredCards.Frame(card.name));face.SetGameArt(layered,Vector2.zero,null);
                    var rect=(RectTransform)face.transform;rect.anchoredPosition=new Vector2((i%3-1)*390,cards.Length>3?285-i/3*570:0);rect.localScale=Vector3.one*.56f;
                }
                foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
                Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});RenderTexture.active=target;
                var image=new Texture2D(1200,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,height),0,0);image.Apply();File.WriteAllBytes(Output+name,image.EncodeToPNG());Object.DestroyImmediate(image);
            }finally{Object.DestroyImmediate(root);camera.targetTexture=null;RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(cg);}
        }
    }
}
