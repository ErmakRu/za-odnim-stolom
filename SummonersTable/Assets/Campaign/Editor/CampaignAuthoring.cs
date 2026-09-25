using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace SummonersTable.Editor
{
    public static class CampaignAuthoring
    {
        public const string Root="Assets/Campaign",Scene=Root+"/Scenes/Campaign.unity",Prefab=Root+"/Resources/Campaign/CampaignComic.prefab";
        static readonly Color Gold=new Color(.96f,.75f,.39f),Ink=new Color(.06f,.075f,.09f),Paper=new Color(.96f,.92f,.83f);
        static Font Font=>Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        [MenuItem("Summoners Table/Campaign/Open campaign")]
        public static void Open(){if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;EditorSceneManager.OpenScene(Scene);}
        [MenuItem("Summoners Table/Campaign/Edit comic JSON")]
        public static void Edit(){Selection.activeObject=AssetDatabase.LoadMainAssetAtPath(Root+"/Resources/Campaign/campaign.json");EditorApplication.ExecuteMenuItem("Window/General/Inspector");}
        public static void Create()
        {
            Directory.CreateDirectory(Root+"/Scenes");Directory.CreateDirectory(Root+"/Resources/Campaign");AssetDatabase.Refresh();
            var media=AssetDatabase.LoadAssetAtPath<CampaignMedia>(Root+"/Resources/Campaign/Media.asset");
            if(media==null){media=ScriptableObject.CreateInstance<CampaignMedia>();AssetDatabase.CreateAsset(media,Root+"/Resources/Campaign/Media.asset");}
            media.sounds=AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/ThirdParty/Audio/Card_Game"}).Select(AssetDatabase.GUIDToAssetPath).Select(p=>new CampaignSound{id=Path.GetFileName(p),clip=AssetDatabase.LoadAssetAtPath<AudioClip>(p)}).ToArray();EditorUtility.SetDirty(media);
            if(!File.Exists(Prefab))BuildPrefab(media);
            AddMenuButton();
            if(!File.Exists(Scene))
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);new GameObject("Campaign entry · Press Play",typeof(CampaignEntry));EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Scene);
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();CampaignTests.Run();Debug.Log("CAMPAIGN_AUTHORED");
        }
        static RectTransform R(string name,Transform parent,float x,float y,float w,float h)
        {var g=new GameObject(name,typeof(RectTransform));var r=(RectTransform)g.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);return r;}
        static Image P(string name,Transform parent,float x,float y,float w,float h,Color color)
        {var a=R(name,parent,x,y,w,h).gameObject.AddComponent<Image>();a.color=color;a.raycastTarget=false;return a;}
        static RawImage Art(string name,Transform parent,float x,float y,float w,float h,string resource)
        {var a=R(name,parent,x,y,w,h).gameObject.AddComponent<RawImage>();a.texture=Resources.Load<Texture2D>(resource);a.raycastTarget=false;return a;}
        static Text T(string name,Transform parent,float x,float y,float w,float h,string text,int size,TextAnchor align=TextAnchor.MiddleCenter)
        {var t=R(name,parent,x,y,w,h).gameObject.AddComponent<Text>();t.font=Font;t.fontSize=size;t.text=text;t.color=Paper;t.alignment=align;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;return t;}
        static Button B(string name,Transform parent,float x,float y,float w,float h,string label)
        {var i=P(name,parent,x,y,w,h,Gold);i.raycastTarget=true;var b=i.gameObject.AddComponent<Button>();var t=T("Label",i.transform,0,0,w-12,h,label,23);t.color=Ink;return b;}
        static void BuildPrefab(CampaignMedia media)
        {
            var root=R("Campaign Comic",null,0,0,1600,1000);var canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=25;
            var scaler=root.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,1000);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;root.gameObject.AddComponent<GraphicRaycaster>();
            var view=root.gameObject.AddComponent<CampaignComicView>();view.media=media;
            view.ambience=root.gameObject.AddComponent<AudioSource>();view.ambience.playOnAwake=false;view.ambience.loop=true;
            view.sfx=root.gameObject.AddComponent<AudioSource>();view.sfx.playOnAwake=false;
            var comic=R("Comic",root,0,0,1600,1000);view.comic=comic.gameObject;
            P("Opaque backdrop",comic,0,0,5000,3000,Ink).raycastTarget=true;
            view.background=Art("Scene background",comic,0,0,1600,1000,"Art/board");
            P("Background shade",comic,0,0,1600,1000,new Color(.04f,.025f,.035f,.46f));
            P("Top rail",comic,0,449,1544,72,new Color(.04f,.045f,.055f,.92f));
            P("Top accent",comic,0,487,1544,3,Gold);view.title=T("Chapter title",comic,0,449,1480,52,"ПИР ХОХОТА",28);view.title.color=Gold;
            view.left=Art("Jester · left",comic,-460,104,500,620,"Art/Jester/jester_basic");
            view.rightBorder=P("Portrait frame",comic,460,104,482,482,Gold);
            view.right=Art("Opponent · right",comic,460,104,500,620,"Art/Others/mainKing");
            view.leftName=T("Jester name",comic,-460,-167,630,35,"Йорик",25);view.leftName.color=Gold;
            view.rightName=T("Opponent name",comic,460,-167,630,35,"",25);view.rightName.color=Gold;
            P("Dialogue frame",comic,0,-318,1504,266,Gold);P("Dialogue body",comic,0,-318,1496,258,new Color(.035f,.045f,.055f,.98f));
            view.speaker=T("Speaker",comic,-16,-214,1410,34,"",25,TextAnchor.MiddleLeft);view.speaker.color=Gold;view.speaker.fontStyle=FontStyle.Bold;
            view.body=T("Dialogue",comic,0,-317,1410,158,"",27,TextAnchor.UpperLeft);view.body.lineSpacing=1.10f;view.body.supportRichText=false;
            view.previous=B("Previous line",comic,-648,-417,154,43,"← Назад");
            view.menu=B("Menu",comic,-448,-417,214,43,"В меню");
            view.skip=B("Skip segment",comic,352,-417,256,43,"К поединку");
            view.next=B("Advance",comic,614,-417,236,43,"Дальше →");view.nextLabel=view.next.GetComponentInChildren<Text>();
            view.progress=T("Progress",comic,0,-477,1420,28,"",18);view.progress.color=new Color(.77f,.76f,.70f);
            var hub=R("Campaign hub",root,0,0,1600,1000);view.hub=hub.gameObject;
            P("Opaque hub backdrop",hub,0,0,5000,3000,Ink).raycastTarget=true;Art("Hub art",hub,0,0,1600,1000,"Art/menu");P("Hub tint",hub,0,0,1600,1000,new Color(.035f,.025f,.05f,.76f));
            Art("Jester",hub,-512,-38,475,610,"Art/Jester/jester_happy");
            view.hubTitle=T("Campaign title",hub,175,333,1120,90,"Пир Хохота",62);view.hubTitle.color=Gold;
            T("Subtitle",hub,175,258,970,50,"СЕМНАДЦАТЬ ДУЭЛЕЙ ЗА ТРОН",24);
            view.hubStatus=T("Checkpoint",hub,175,180,950,50,"",25);
            P("Deck frame",hub,175,21,870,220,new Color(.05f,.06f,.07f,.94f));view.deckLabel=T("Deck",hub,175,79,630,47,"",30);view.deckLabel.color=Gold;
            view.deckPrevious=B("Previous deck",hub,-200,77,64,48,"←");view.deckNext=B("Next deck",hub,550,77,64,48,"→");
            view.guide=T("Deck guide",hub,175,-12,776,104,"",22);
            view.resume=B("Continue campaign",hub,175,-177,610,62,"Продолжить путь");
            view.newGame=B("New campaign",hub,175,-264,610,62,"Начать заново");
            view.hubMenu=B("Main menu",hub,175,-351,610,62,"В главное меню");
            T("Controls",hub,0,-467,1460,36,"ЛКМ по кнопке · Пробел / Enter — дальше · Первый клик завершает появление текста",20);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject,Prefab);Object.DestroyImmediate(root.gameObject);
        }
        static void AddMenuButton()
        {
            const string path="Assets/Prefabs/MainMenuCanvas.prefab";var g=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var front=g.GetComponent<FrontEndCanvas>();
                if(!front.actions.Contains("campaign"))
                {
                    var source=front.buttons.First(b=>b!=null);var button=Object.Instantiate(source,source.transform.parent);button.name="Campaign";button.GetComponentInChildren<Text>().text="Кампания · Пир Хохота";
                    front.buttons=new[]{button}.Concat(front.buttons).ToArray();front.actions=new[]{"campaign"}.Concat(front.actions).ToArray();
                }
                for(int i=0;i<front.buttons.Length;i++)((RectTransform)front.buttons[i].transform).anchoredPosition=new Vector2(0,135-i*75);
                PrefabUtility.SaveAsPrefabAsset(g,path);
            }finally{PrefabUtility.UnloadPrefabContents(g);}
        }
    }
}
