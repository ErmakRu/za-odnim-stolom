using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace SummonersTable.Editor
{
    public static class LayeredCardsAuthoring
    {
        public const string Root="Assets/LayeredCards",Scene=Root+"/Scenes/LayeredCardsLab.unity";
        static readonly Color Paper=new Color(.045f,.07f,.10f),Gold=new Color(.86f,.71f,.43f),Ink=new Color(.91f,.90f,.85f);
        static Font Font=>Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        [MenuItem("Summoners Table/Layered Cards/Open preview scene")]
        public static void Open(){if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;EditorSceneManager.OpenScene(Scene);Selection.activeGameObject=Object.FindFirstObjectByType<LayeredCardsManager>().gameObject;}

        [MenuItem("Summoners Table/Layered Cards/Create or update prototypes")]
        public static void Create()
        {
            foreach(string dir in new[]{"Prefabs/Types","Prefabs/Cards","Scenes"})Directory.CreateDirectory(Root+"/"+dir);
            AssetDatabase.Refresh();
            foreach(string p in Directory.GetFiles(Root+"/Resources/LayeredCards/Layers","*.png",SearchOption.AllDirectories))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(p.Replace('\\','/'));
                importer.textureType=TextureImporterType.Default;importer.alphaSource=TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;
                importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;
                importer.SaveAndReimport();
            }
            ConfigAuthoring.CopyDefaults();var bundle=ConfigBundle.Read(ConfigAuthoring.Folder);
            if(!File.Exists(Root+"/Prefabs/LayeredCardBase.prefab"))CreateBase();
            foreach(string type in new[]{"Creature","Spell","Reaction"})
                if(!File.Exists(Root+"/Prefabs/Types/"+type+".prefab"))Variant(Root+"/Prefabs/LayeredCardBase.prefab",Root+"/Prefabs/Types/"+type+".prefab","");
            foreach(var art in bundle.layeredCards.cards)
            {
                var card=bundle.Catalog().cards.Single(c=>c.name==art.cardName);string path=Root+"/Prefabs/Cards/"+card.id+".prefab";
                if(!File.Exists(path))Variant(Root+"/Prefabs/Types/"+(card.kind=="creature"?"Creature":card.kind=="spell"?"Spell":"Reaction")+".prefab",path,card.name);
            }
            if(!File.Exists(Root+"/Prefabs/LayeredCardsManager.prefab")){var manager=new GameObject("Layered Cards Manager",typeof(LayeredCardsManager));PrefabUtility.SaveAsPrefabAsset(manager,Root+"/Prefabs/LayeredCardsManager.prefab");Object.DestroyImmediate(manager);}
            BuildScene(bundle);AssetDatabase.SaveAssets();Debug.Log("LAYERED_CARDS_CREATED");
        }
        static void Variant(string source,string target,string name)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(source));
            go.name=Path.GetFileNameWithoutExtension(target);go.GetComponent<LayeredCardView>().cardName=name;
            PrefabUtility.SaveAsPrefabAsset(go,target);Object.DestroyImmediate(go);
        }
        static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {
            var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(w,h);r.anchoredPosition=new Vector2(x,y);return r;
        }
        static Image Panel(string name,Transform parent,float x,float y,float w,float h,Color color)
        {var image=Rect(name,parent,x,y,w,h).gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;}
        static Text Label(string name,Transform parent,float x,float y,float w,float h,int size,TextAnchor alignment=TextAnchor.MiddleCenter)
        {var t=Rect(name,parent,x,y,w,h).gameObject.AddComponent<Text>();t.font=Font;t.fontSize=size;t.color=Ink;t.alignment=alignment;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;}
        static void CreateBase()
        {
            var r=Rect("LayeredCardBase",null,0,0,600,940);var view=r.gameObject.AddComponent<LayeredCardView>();
            Panel("Outer frame",r,0,0,600,940,Gold);Panel("Inner body",r,0,0,590,930,Paper);
            view.accent=Panel("Type accent",r,0,460,580,6,Gold);
            view.type=Label("Type",r,0,427,544,30,21);view.type.text="";
            view.title=Label("Name",r,0,380,548,66,39);view.title.fontStyle=FontStyle.Bold;view.title.resizeTextForBestFit=true;view.title.resizeTextMinSize=28;view.title.resizeTextMaxSize=39;
            Panel("Art frame",r,0,95,554,494,Gold);
            view.artwork=Rect("Three layer illustration",r,0,95,546,486).gameObject.AddComponent<RawImage>();view.artwork.raycastTarget=false;
            Panel("QTE backdrop",r,-161,311,212,32,new Color(.02f,.035f,.06f,.87f));
            view.qteLabel=Label("QTE count",r,-210,311,104,28,20);
            view.qteSymbols=new Image[20];
            for(int i=0;i<20;i++){var icon=Panel("QTE "+(i+1),r,-147+(i%10)*18,311-(i/10)*18,11,11,Gold);icon.transform.localRotation=Quaternion.Euler(0,0,45);icon.gameObject.SetActive(false);view.qteSymbols[i]=icon;}
            view.roleAccent=Panel("Role accent",r,-273,-179,4,32,Gold);
            view.role=Label("Faction and group",r,0,-179,528,34,23);
            view.stats=Label("Stats",r,0,-223,536,38,23);view.stats.fontStyle=FontStyle.Bold;
            Panel("Separator",r,0,-252,534,1,new Color(.7f,.62f,.45f,.4f));
            view.description=Label("Rules",r,0,-323,528,132,25,TextAnchor.UpperLeft);
            view.flavor=Label("Flavor",r,0,-422,526,52,21);view.flavor.fontStyle=FontStyle.Italic;view.flavor.color=new Color(.65f,.7f,.73f);
            PrefabUtility.SaveAsPrefabAsset(r.gameObject,Root+"/Prefabs/LayeredCardBase.prefab");Object.DestroyImmediate(r.gameObject);
        }
        static void BuildScene(ConfigBundle bundle)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("Standalone preview - prevents game bootstrap",typeof(AuthoringLab));
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/LayeredCardsManager.prefab"));
            var cam=new GameObject("Preview Camera").AddComponent<Camera>();cam.tag="MainCamera";cam.transform.position=new Vector3(0,0,-2.35f);
            cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.018f,.03f,.045f);cam.fieldOfView=31;cam.nearClipPlane=.1f;cam.farClipPlane=20;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            var root=Rect("Layered Cards Preview",null,0,0,1800,1200);var canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=cam;root.localScale=Vector3.one*.001f;
            var title=Label("Preview heading",root,0,574,1600,48,34);title.text="КАРТОЧКИ · ФОН И ЕДИНЫЙ ПЛАН";title.color=Gold;
            var subtitle=Label("Instructions",root,0,524,1600,30,20);subtitle.text="Удерживайте ЛКМ и двигайте мышью · Пробел — автоповорот";subtitle.color=new Color(.58f,.67f,.75f);
            var demo=root.gameObject.AddComponent<LayeredCardsDemo>();demo.cards=new LayeredCardView[2];
            string[] ids={"S01","C02"};
            for(int i=0;i<2;i++)
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Cards/"+ids[i]+".prefab"),root);
                var rect=(RectTransform)go.transform;rect.anchoredPosition=new Vector2(i==0?-360:360,-5);rect.localScale=Vector3.one;
                demo.cards[i]=go.GetComponent<LayeredCardView>();demo.cards[i].Apply(bundle);
            }
            demo.hint=Label("Angle",root,0,-530,1700,40,21);demo.hint.color=new Color(.67f,.73f,.8f);demo.ApplyLook();
            if(!File.Exists(Root+"/Prefabs/LayeredCardsPreview.prefab"))
            {var preview=PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject,Root+"/Prefabs/LayeredCardsPreview.prefab",InteractionMode.AutomatedAction);}
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Scene);
        }
        public static void Render()
        {
            EditorSceneManager.OpenScene(Scene);LayeredCardData.Reload();var demo=Object.FindFirstObjectByType<LayeredCardsDemo>();
            foreach(var card in demo.cards)card.Apply(LayeredCardData.Current);
            var camera=Object.FindFirstObjectByType<Camera>();camera.enabled=false;
            var target=new RenderTexture(1600,1080,24,RenderTextureFormat.ARGB32);target.antiAliasing=4;target.Create();camera.targetTexture=target;camera.aspect=1600f/1080;
            var args=Environment.GetCommandLineArgs();int folderIndex=Array.IndexOf(args,"--layered-output");
            string output=folderIndex>=0&&folderIndex+1<args.Length?args[folderIndex+1]:"../output/layered-cards";
            var frame=new Texture2D(1600,1080,TextureFormat.RGB24,false);string folder=Path.Combine(Path.GetFullPath(output),"frames");Directory.CreateDirectory(folder);
            bool video=args.Contains("--layered-video");int count=video?288:3;
            try
            {
                for(int i=0;i<count;i++)
                {
                    demo.Pose(video?i/24f:i==0?0:i==1?1.7f:5.4f);Canvas.ForceUpdateCanvases();
                    RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                    RenderTexture.active=target;frame.ReadPixels(new Rect(0,0,1600,1080),0,0);frame.Apply();
                    File.WriteAllBytes(Path.Combine(folder,"frame-"+i.ToString("0000")+".png"),frame.EncodeToPNG());
                    if(i%48==0)Debug.Log("LAYERED_FRAME "+i+"/"+count);
                }
                Debug.Log("LAYERED_RENDER_OK frames="+count);
            }
            finally{RenderTexture.active=null;Object.DestroyImmediate(frame);target.Release();Object.DestroyImmediate(target);}
        }
        public static void CreateAndPreview(){Create();LayeredCardsTests.Run();Render();}
        public static void RecordAndTestPlay(){LayeredCardsTests.Run();Render();LayeredCardsPlayTest.Run();}
        public static void CombinedPreview(){RefreshCaption();LayeredCardsTests.Run();LayeredMotionTests.Run();Render();}
        static void RefreshCaption()
        {
            string path=Root+"/Prefabs/LayeredCardsPreview.prefab";var prefab=PrefabUtility.LoadPrefabContents(path);
            try{prefab.transform.Find("Preview heading").GetComponent<Text>().text="КАРТОЧКИ · ФОН И ЕДИНЫЙ ПЛАН";
                prefab.GetComponent<LayeredCardsDemo>().ApplyLook();PrefabUtility.SaveAsPrefabAsset(prefab,path);}
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
        }
    }
}
