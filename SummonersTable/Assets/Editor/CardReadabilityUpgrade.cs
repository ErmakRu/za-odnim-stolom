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
    public static class CardReadabilityUpgrade
    {
        static void Place(Transform t,float x,float y,float w,float h)=>SharedPresentationMigration.Place(t,x,y,w,h);
        [MenuItem("Summoners Table/Cards/Apply readable card style")]
        public static void Run(){CardStyleUpgrade.Apply();Verify();Render();Debug.Log("CARD_READABILITY_PASS");}
        static void Shadow(Text text){var shadow=text.GetComponent<Shadow>()??text.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.9f);shadow.effectDistance=new Vector2(1,-1);}
        public static void Verify()
        {
            var b=ConfigBundle.Read(ConfigAuthoring.Folder);var c=b.Catalog();int faces=0;float lowest=99;
            void Check(bool ok,string why){if(!ok)throw new Exception("CARD READABILITY: "+why);}
            foreach(bool limited in new[]{true,false})
            {
                CardPresentationContext.Apply(new MatchOptions{limitPower=limited});
                foreach(var card in c.cards)
                {
                    var view=Object.Instantiate(AssetDatabase.LoadAssetAtPath<CardView>("Assets/Prefabs/Editable/Cards/Instances/"+card.id+".prefab"));
                    try
                    {
                        view.Import(card,c);
                        foreach(var face in new[]{view.sharedFull,view.sharedCompact,view.sharedWorld})
                        {
                            Check(face.faction!=null,"species reference "+card.id);Check(face.type.gameObject.activeSelf==(card.kind!="creature")&&face.accent.gameObject.activeSelf==(card.kind!="creature"),"spells and reactions have a full type band");
                            Check(face.titleFill.rectTransform.rect.width==590,"name matches body width");Check(!face.transform.Find("QTE backdrop").gameObject.activeSelf,"no QTE plate");
                            Check(face.description.fontSize>=44,"1.5x minimum text "+card.id);lowest=Mathf.Min(lowest,face.description.fontSize);
                            Check(face.description.rectTransform.anchoredPosition.y-face.RulesHeight>=face.rulesBottom-1,"all text fits "+card.id+" / "+limited+" / "+face.RulesHeight);
                            Check(face.artwork.rectTransform.rect.height>=140,"art remains visible");
                            if(card.kind=="creature"){ColorUtility.TryParseHtmlString(c.factionColors.Single(x=>x.name==card.faction).hex,out var a);ColorUtility.TryParseHtmlString(c.roleColors.Single(x=>x.name==card.role).hex,out var d);Check(face.faction.text==card.faction&&face.role.text==card.role&&face.factionFill.color==a&&face.roleFill.color==d,"linked species/class colour");}
                            foreach(int cost in new[]{0,3,4,5,6,7,8,9,12,20,2}){face.SetQteCost(cost);Check(face.qteSymbols.Count(x=>x.gameObject.activeSelf)==cost&&face.qteSymbols.Where(x=>x.gameObject.activeSelf).All(x=>x.color==CardView.GetQteColor(cost)),"all cubes recolour");Check(face.qteLabel.preferredWidth<=face.qteLabel.rectTransform.rect.width,"whole QTE label fits");}
                            faces++;
                        }
                    }finally{Object.DestroyImmediate(view.gameObject);}
                }
            }
            Check(CardView.GetQteColor(5)!=CardView.GetQteColor(6)&&CardView.GetQteColor(7)!=CardView.GetQteColor(8)&&CardView.GetQteColor(8)==CardView.GetQteColor(20),"new QTE boundaries");
            CardPresentationContext.Apply(new MatchOptions());
            var duplicate=ConfigBundle.Read(ConfigAuthoring.Folder);duplicate.cards.factionColors=duplicate.cards.factionColors.Concat(new[]{duplicate.cards.factionColors[0]}).ToArray();bool rejected=false;try{duplicate.Validate();}catch(FormatException){rejected=true;}Check(rejected,"duplicate species colour rejected");
            Directory.CreateDirectory("../output/tests");File.WriteAllText("../output/tests/card-readability.txt",$"PASS {faces} full/hand/world faces; all 30 cards, limited/unlimited rules fit; minimum rules font {lowest} (previous 29); shared species/class palettes; no creature type band or QTE plate; full spell/reaction type band; QTE boundaries 3/4/5/6/7/8/20 and full-row recolouring; duplicate palette names rejected.\n");
        }
        public static void Render()
        {
            var catalog=ConfigBundle.Read(ConfigAuthoring.Folder).Catalog();Directory.CreateDirectory("../output/card-readability");
            Capture(catalog,new[]{"C01","C02","S01","R02"},new[]{3,4,3,0},false,"cards-normal.png",.63f);
            Capture(catalog,new[]{"C01","C02","S01"},new[]{3,4,3},true,"cards-tutorial.png",.63f);
            Capture(catalog,new[]{"C01","C02","S01","C01"},new[]{3,5,6,8},true,"qte-colours-demo.png",.63f);
            Capture(catalog,new[]{"C01","C02","S01","R02"},new[]{3,4,3,0},false,"cards-hand-size.png",.30f);
        }
        static void Capture(Catalog catalog,string[] ids,int[] costs,bool tutorial,string file,float scale)
        {
            var cameraObject=new GameObject("Readability preview camera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.05f,.055f);camera.nearClipPlane=.01f;camera.farClipPlane=50;camera.transform.position=new Vector3(0,0,-10);camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            var target=new RenderTexture(1600,1000,24);target.Create();camera.targetTexture=target;
            var root=new GameObject("Card readability preview",typeof(RectTransform),typeof(Canvas));var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            var previous=RenderTexture.active;
            try
            {
                for(int i=0;i<ids.Length;i++)
                {
                    string tutorialPath="Assets/Campaign/Tutorial/Cards/"+ids[i]+".prefab";string path=tutorial&&File.Exists(tutorialPath)?tutorialPath:SharedPresentationMigration.FacePath;
                    var face=Object.Instantiate(AssetDatabase.LoadAssetAtPath<LayeredCardView>(path),root.transform);face.runtimeMode=true;face.ApplyCard(catalog.Card(ids[i]),catalog,null);face.SetGameArt(false,Vector2.zero,null);face.SetQteCost(costs[i]);
                    var rect=(RectTransform)face.transform;rect.anchoredPosition=new Vector2((i-(ids.Length-1)*.5f)*390,0);rect.localScale=Vector3.one*scale;
                }
                if(file=="qte-colours-demo.png")SharedPresentationMigration.Label("QTE demo caption",root.transform,0,380,1500,48,26).text="Примеры модификаторов стоимости: 3 / 5 / 6 / 8. Базовые цены карт не менялись.";
                foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
                Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});RenderTexture.active=target;
                var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes("../output/card-readability/"+file,image.EncodeToPNG());Object.DestroyImmediate(image);
            }finally{Object.DestroyImmediate(root);camera.targetTexture=null;RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(cameraObject);}
        }
    }
}
