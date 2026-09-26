using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace SummonersTable.Editor
{
    public static class LayeredCardsGalleryAuthoring
    {
        public const string Scene="Assets/LayeredCards/Scenes/LayeredCardsGallery.unity";
        const string Root="Assets/LayeredCards";
        const string Output="../output/layered-cards-all";
        static readonly Color Ink=new Color(.87f,.92f,.94f);

        [MenuItem("Summoners Table/Layered Cards/Open full gallery (30 cards)")]
        public static void Open()
        {
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(Scene);
        }
        [MenuItem("Summoners Table/Layered Cards/Rebuild full gallery")]
        public static void Rebuild()
        {
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            Build();
        }
        public static void Build()
        {
            AssetDatabase.Refresh();
            foreach(string file in Directory.GetFiles(Root+"/Resources/LayeredCards/Layers","*.png",SearchOption.AllDirectories))
            {
                var path=file.Replace('\\','/');var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                // Existing reference textures retain their exact settings.
                if(!path.EndsWith("/subject.png")&&!path.EndsWith("/background.png"))continue;
                importer.textureType=TextureImporterType.Default;importer.alphaSource=TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;
                importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.maxTextureSize=2048;importer.SaveAndReimport();
            }
            var bundle=ConfigBundle.Read(ConfigAuthoring.Folder);
            if(bundle.layeredCards.cards.Length!=bundle.Catalog().cards.Count)throw new Exception("Every card needs layered artwork before building gallery.");
            foreach(var card in bundle.Catalog().cards)
            {
                string path=Root+"/Prefabs/Cards/"+card.id+".prefab";
                if(File.Exists(path))continue;
                string type=card.kind=="creature"?"Creature":card.kind=="spell"?"Spell":"Reaction";
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Types/"+type+".prefab"));
                instance.name=card.id;instance.GetComponent<LayeredCardView>().cardName=card.name;
                PrefabUtility.SaveAsPrefabAsset(instance,path);Object.DestroyImmediate(instance);
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("Standalone gallery - no game bootstrap",typeof(AuthoringLab));
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/LayeredCardsManager.prefab"));
            new GameObject("Event System",typeof(EventSystem),typeof(StandaloneInputModule));
            var camera=new GameObject("Gallery Camera").AddComponent<Camera>();camera.tag="MainCamera";
            camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.018f,.03f,.042f);camera.nearClipPlane=.1f;camera.farClipPlane=100;
            camera.fieldOfView=18; // Gentle perspective keeps edge cards inside the screen at full tilt.
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            var root=Rect("All Layered Cards",null,0,0,1920,1200);
            var canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=5;
            var scaler=root.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1200);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            root.gameObject.AddComponent<GraphicRaycaster>();
            var gallery=root.gameObject.AddComponent<LayeredCardsGallery>();
            Label(root,"Heading","КОЛЛЕКЦИЯ · ПЕРЕЛИВАЮЩИЕСЯ КАРТЫ",0,552,1800,48,34).color=new Color(.88f,.73f,.46f);
            gallery.filters=new Button[4];string[] labels={"Все · 30","Существа · 18","Заклинания · 8","Реакции · 4"};
            for(int i=0;i<4;i++)gallery.filters[i]=Button(root,"Filter "+i,labels[i],-718+i*205,484,194,46);
            gallery.modes=new Button[4];string[] modes={"1  Карточка","2  Фон","3  Персонаж","4  Оригинал"};
            for(int i=0;i<4;i++)gallery.modes[i]=Button(root,"Mode "+i,modes[i],155+i*207,484,196,46);
            gallery.slots=new LayeredCardView[3];gallery.identifiers=new Text[3];
            var first=bundle.Catalog().cards.Take(3).ToArray();
            for(int i=0;i<3;i++)
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Cards/"+first[i].id+".prefab"),root);
                go.name="Card slot "+(i+1);var rect=(RectTransform)go.transform;rect.anchoredPosition=new Vector2((i-1)*610,-6);rect.localScale=Vector3.one*.87f;
                gallery.slots[i]=go.GetComponent<LayeredCardView>();
                gallery.identifiers[i]=Label(root,"ID "+i,"",(i-1)*610,-451,400,30,18);gallery.identifiers[i].color=new Color(.45f,.58f,.64f);
                foreach(var g in go.GetComponentsInChildren<Graphic>(true))g.raycastTarget=false;
            }
            gallery.previous=Button(root,"Previous","← Назад",-760,-514,240,50);
            gallery.next=Button(root,"Next","Далее →",-232,-514,240,50);
            gallery.pageLabel=Label(root,"Page","",-496,-514,270,50,23);
            gallery.rotation=Button(root,"Rotation","",160,-514,258,50);
            gallery.reset=Button(root,"Reset","Сброс наклона",441,-514,256,50);
            gallery.reload=Button(root,"Reload","Перечитать JSON",732,-514,288,50);
            gallery.statusLabel=Label(root,"Status","",0,-577,1840,35,19);gallery.statusLabel.color=new Color(.5f,.63f,.7f);
            gallery.Initialize(bundle);gallery.ResetPose();gallery.autoRotate=true;
            foreach(var face in gallery.slots)face.runtimeMode=false;
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Scene);
            AssetDatabase.SaveAssets();Debug.Log("LAYERED_GALLERY_CREATED");
        }
        static RectTransform Rect(string name,Transform parent,float x,float y,float width,float height)
        {
            var go=new GameObject(name,typeof(RectTransform));var rect=(RectTransform)go.transform;rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(width,height);rect.anchoredPosition=new Vector2(x,y);return rect;
        }
        static Text Label(Transform parent,string name,string value,float x,float y,float width,float height,int size)
        {
            var label=Rect(name,parent,x,y,width,height).gameObject.AddComponent<Text>();label.text=value;
            label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=size;label.alignment=TextAnchor.MiddleCenter;
            label.color=Ink;label.raycastTarget=false;return label;
        }
        static Button Button(Transform parent,string name,string title,float x,float y,float width,float height)
        {
            var rect=Rect(name,parent,x,y,width,height);var image=rect.gameObject.AddComponent<Image>();image.color=new Color(.085f,.14f,.18f);
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;
            button.navigation=new Navigation{mode=Navigation.Mode.None};
            Label(rect,"Label",title,0,0,width-12,height,22);return button;
        }
        public static void BuildAndVerify(){Build();Verify();Capture();Debug.Log("LAYERED_GALLERY_ALL_PASS");}
        public static void Verify()
        {
            var bundle=ConfigBundle.Read(ConfigAuthoring.Folder);var catalog=bundle.Catalog();int checks=0;
            void Check(bool condition,string message){checks++;if(!condition)throw new Exception("Layered gallery: "+message);}
            Check(bundle.layeredCards.cards.Length==catalog.cards.Count,"coverage");
            var defaults=ConfigJson.Read<LayeredCardsConfig>(File.ReadAllText("Assets/Resources/ConfigDefaults/layered-cards.json"));
            Check(JsonUtility.ToJson(defaults)==JsonUtility.ToJson(bundle.layeredCards),"fallback config matches");
            foreach(var card in catalog.cards)
            {
                var art=bundle.layeredCards.Find(card.name);Check(art!=null,"missing "+card.id);
                Check(art.background.texture!=art.subject.rear.texture,"separate plates "+card.id);
                var path=Root+"/Prefabs/Cards/"+card.id+".prefab";
                var prefab=AssetDatabase.LoadAssetAtPath<LayeredCardView>(path);Check(prefab!=null,"prefab "+card.id);
                var instance=Object.Instantiate(prefab);
                try
                {
                    instance.Apply(bundle);Check(instance.title.text==card.name,"name "+card.id);
                    Check(instance.ArtMaterial!=null&&instance.ArtMaterial.shader.isSupported,"UI shader "+card.id);
                    Check(instance.ArtMaterial.GetVector("_ForegroundInfo").w==(art.subject.singleSource?1:0),"single alpha composition "+card.id);
                    instance.SetLook(new Vector2(-1,1));Check(instance.artwork.materialForRendering.GetVector("_ViewOffset").x<-.9f,"masked parallax "+card.id);
                    instance.SetPreviewLayer(2);Check(instance.artwork.materialForRendering.GetFloat("_PreviewLayer")==2,"subject inspection "+card.id);
                    instance.SetGameArt(false,Vector2.zero,null);Check(instance.artwork.material==instance.artwork.defaultMaterial,"flat toggle "+card.id);
                }finally{Object.DestroyImmediate(instance.gameObject);}
            }
            EditorSceneManager.OpenScene(Scene);var gallery=Object.FindFirstObjectByType<LayeredCardsGallery>();gallery.Initialize(bundle);
            Check(Object.FindFirstObjectByType<AuthoringLab>()!=null&&Object.FindFirstObjectByType<GameApp>()==null,"standalone scene");
            int[] expected={30,18,8,4};
            for(int f=0;f<4;f++)
            {
                gallery.SetFilter(f);Check(gallery.FilteredCount==expected[f],"filter "+f);
                var names=new System.Collections.Generic.HashSet<string>();
                for(int p=0;p<gallery.PageCount;p++)
                {
                    gallery.ShowPage(p);
                    foreach(var face in gallery.slots.Where(c=>c.gameObject.activeSelf))Check(names.Add(face.cardName),"duplicate page card");
                    for(int mode=0;mode<4;mode++)gallery.SetMode(mode);
                }
                Check(names.Count==expected[f],"all filtered cards reachable");
                gallery.ShowPage(-1);Check(gallery.Page==0&&!gallery.previous.interactable,"first boundary");
                gallery.ShowPage(999);Check(gallery.Page==gallery.PageCount-1&&!gallery.next.interactable,"last boundary");
            }
            gallery.SetFilter(0);gallery.SetMode(0);gallery.ResetPose();
            Directory.CreateDirectory(Output);File.WriteAllText(Output+"/verification.txt",$"PASS {checks} checks: all 30 separate layer sets, prefabs, JSON/default parity, shader support, masked parallax, single alpha compositing, flat fallback, all pages and filters, four inspection modes, standalone scene.\n");
            Debug.Log("LAYERED_GALLERY_VERIFIED "+checks);
        }
        public static void Capture()
        {
            EditorSceneManager.OpenScene(Scene);var gallery=Object.FindFirstObjectByType<LayeredCardsGallery>();gallery.Initialize(ConfigBundle.Read(ConfigAuthoring.Folder));
            var camera=Object.FindFirstObjectByType<Camera>();camera.enabled=false;camera.aspect=1.6f;
            var scaler=gallery.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;scaler.scaleFactor=1;
            var target=new RenderTexture(1920,1200,24,RenderTextureFormat.ARGB32);target.antiAliasing=4;target.Create();camera.targetTexture=target;
            var frame=new Texture2D(1920,1200,TextureFormat.RGB24,false);var active=RenderTexture.active;
            Directory.CreateDirectory(Output);
            try
            {
                for(int p=0;p<gallery.PageCount;p++)
                {
                    gallery.ShowPage(p);gallery.SetMode(0);gallery.ResetPose();Save("page-"+(p+1).ToString("00")+".png");
                    gallery.look=new Vector2(1,.7f);gallery.ApplyLook();Save("tilt-"+(p+1).ToString("00")+".png");
                }
                gallery.ShowPage(5);gallery.SetMode(2);gallery.ResetPose();Save("subjects.png");
                gallery.SetMode(1);Save("backgrounds.png");
                gallery.ShowPage(0);gallery.SetMode(0);
                foreach(var look in new[]{new Vector2(-1,-1),new Vector2(-1,1),new Vector2(1,-1),new Vector2(1,1)})
                {
                    gallery.look=look;gallery.ApplyLook();Canvas.ForceUpdateCanvases();
                    foreach(var face in gallery.slots)
                    {
                        var corners=new Vector3[4];((RectTransform)face.transform).GetWorldCorners(corners);
                        foreach(var corner in corners)
                        {
                            var point=camera.WorldToScreenPoint(corner);
                            if(point.x<0||point.x>1920||point.y<130||point.y>1040)throw new Exception("Extreme tilt clips card or overlaps controls: "+face.cardName);
                        }
                    }
                }
                gallery.look=new Vector2(-1,-1);gallery.ApplyLook();Save("extreme-negative.png");
                File.WriteAllText(Output+"/viewport-verification.txt","PASS: all three card rectangles remain inside the viewport and clear of controls at all four extreme manual tilts.\n");
                void Save(string filename)
                {
                    Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                    RenderTexture.active=target;frame.ReadPixels(new Rect(0,0,1920,1200),0,0);frame.Apply();File.WriteAllBytes(Output+"/"+filename,frame.EncodeToPNG());
                }
            }
            finally{RenderTexture.active=active;camera.targetTexture=null;Object.DestroyImmediate(frame);target.Release();Object.DestroyImmediate(target);}
            Debug.Log("LAYERED_GALLERY_CAPTURED");
        }
    }
}
