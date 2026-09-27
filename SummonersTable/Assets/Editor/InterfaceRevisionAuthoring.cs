using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace SummonersTable.Editor
{
    public static class InterfaceRevisionAuthoring
    {
        static readonly Color Wood=new Color(.14f,.065f,.025f,.98f),Gold=new Color(.76f,.49f,.20f),Cream=new Color(1,.91f,.73f);
        static Font Font=>Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        static T Ensure<T>(GameObject go) where T:Component{var component=go.GetComponent<T>();return component!=null?component:go.AddComponent<T>();}
        static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size)
        {var r=parent.Find(name) as RectTransform;if(r==null)r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;r.localScale=Vector3.one;r.localRotation=Quaternion.identity;return r;}
        static void Place(RectTransform r,Vector2 pos,Vector2 size){r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;r.localScale=Vector3.one;}
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        static Text Text(string name,Transform parent,string value,Vector2 pos,Vector2 size,int font=24)
        {var t=Ensure<Text>(Rect(name,parent,pos,size).gameObject);t.font=Font;t.text=value;t.fontSize=font;t.color=Cream;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.resizeTextForBestFit=false;return t;}
        static TavernPanel Panel(string name,Transform parent,Vector2 pos,Vector2 size)
        {var panel=Ensure<TavernPanel>(Rect(name,parent,pos,size).gameObject);panel.color=Wood;panel.border=Gold;panel.corner=12;panel.inset=3;panel.raycastTarget=false;return panel;}
        static CardFoilBorder Foil(Transform parent,string name,Vector2 pos,Vector2 size,float thickness)
        {var border=Ensure<CardFoilBorder>(Rect(name,parent,pos,size).gameObject);border.material=Resources.Load<Material>("Styles/CardFoilBorder");border.color=Gold;border.thickness=thickness;border.raycastTarget=false;return border;}
        static Button Button(FrontEndCanvas front,string action)=>front.buttons[Array.IndexOf(front.actions,action)];
        static void SetButton(Button b,Vector2 pos,Vector2 size,int font)
        {Place((RectTransform)b.transform,pos,size);var label=b.GetComponentInChildren<Text>(true);label.fontSize=font;label.resizeTextForBestFit=false;label.color=Cream;var skin=b.GetComponent<TavernPanel>();if(skin!=null){skin.color=Wood;skin.border=Gold;}var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.18f,1.12f,1);colors.pressedColor=new Color(.82f,.74f,.6f);b.colors=colors;}
        public static void Menu(GameObject root)
        {
            var f=root.GetComponent<FrontEndCanvas>();if(f==null||f.lobby||f.homePage==null)return;
            Place(f.title.rectTransform,new Vector2(0,365),new Vector2(1460,85));f.title.fontSize=60;f.title.fontStyle=FontStyle.Bold;f.title.color=Cream;
            Place(f.subtitle.rectTransform,new Vector2(0,286),new Vector2(1380,52));f.subtitle.fontSize=25;f.subtitle.color=new Color(.86f,.71f,.48f);
            var play=Button(f,"play");SetButton(play,new Vector2(0,75),new Vector2(640,120),48);play.GetComponent<TavernPanel>().color=new Color(.40f,.19f,.055f);Foil(play.transform,"Play highlight",Vector2.zero,new Vector2(630,110),4);
            var icon=Ensure<RawImage>(Rect("Play icon",play.transform,new Vector2(-210,0),new Vector2(40,40)).gameObject);icon.texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/VFX/Motion Titles Pack/Sprites/Icons/Play.png");icon.color=Cream;icon.raycastTarget=false;
            string[] secondary={"cards","settings","quit"};for(int i=0;i<3;i++)SetButton(Button(f,secondary[i]),new Vector2((i-1)*350,-125),new Vector2(322,62),25);
            var modeActions=new[]{"campaign","steam","local","tutorial"};var titles=new[]{"Кампания","Играть онлайн","Играть локально","Обучение"};
            var descriptions=new[]{"История Шута и Пир Хохота","Поединок с друзьями в Steam","Друзья и боты за одним ПК","Сон Шута. Первые карты"};
            var arts=new[]{"Art/S07","Art/C12","Art/C01","Art/C02"};
            var icons=new[]{"Assets/ThirdParty/VFX/Motion Titles Pack/Sprites/Icons/Documentation.png","Assets/ThirdParty/VFX/Matthew Guz/Spell Area of Effect FREE/Textures/2-Sword  (1).png","Assets/Resources/Art/card_back.png","Assets/ThirdParty/VFX/Motion Titles Pack/Sprites/Icons/Quote.png"};
            for(int i=0;i<4;i++)
            {
                var b=Button(f,modeActions[i]);SetButton(b,new Vector2((i-1.5f)*338,-5),new Vector2(312,498),27);
                var label=b.GetComponentInChildren<Text>(true);label.text=titles[i];Place(label.rectTransform,new Vector2(0,-151),new Vector2(294,46));
                var art=Ensure<RawImage>(Rect("Mode illustration",b.transform,new Vector2(0,58),new Vector2(288,350)).gameObject);art.texture=Resources.Load<Texture2D>(arts[i]);art.material=Resources.Load<Material>("Styles/CardWindowUI");art.raycastTarget=false;
                Ensure<MenuCardIllustration>(b.gameObject).artwork=art;
                Foil(b.transform,"Illustration rim",new Vector2(0,58),new Vector2(290,352),3);Foil(b.transform,"Card rim",Vector2.zero,new Vector2(308,494),3);
                Panel("Icon seal",b.transform,new Vector2(0,-110),new Vector2(60,56));var emblem=Ensure<RawImage>(Rect("Mode icon",b.transform,new Vector2(0,-110),new Vector2(38,38)).gameObject);emblem.texture=AssetDatabase.LoadAssetAtPath<Texture2D>(icons[i]);emblem.color=Cream;emblem.raycastTarget=false;
                Text("Mode description",b.transform,descriptions[i],new Vector2(0,-207),new Vector2(276,62),20);
            }
            SetButton(Button(f,"home"),new Vector2(0,-350),new Vector2(300,58),25);
            var campaignPanel=Panel("Campaign choices",f.campaignPage.transform,new Vector2(0,-25),new Vector2(720,400));campaignPanel.transform.SetAsFirstSibling();
            Text("Campaign heading",f.campaignPage.transform,"ПИР ХОХОТА",new Vector2(0,116),new Vector2(600,48),30);
            SetButton(Button(f,"campaign-continue"),new Vector2(0,22),new Vector2(580,72),30);SetButton(Button(f,"campaign-new"),new Vector2(0,-77),new Vector2(580,72),30);
            Text("Save hint",f.campaignPage.transform,"",new Vector2(0,-154),new Vector2(620,36),20);
            var back=f.buttons.Select((b,i)=>new{b,a=f.actions[i]}).First(x=>x.a=="play"&&x.b.transform.IsChildOf(f.campaignPage.transform)).b;SetButton(back,new Vector2(0,-310),new Vector2(300,58),25);
            play.gameObject.SetActive(false);Button(f,"home").gameObject.SetActive(false);back.gameObject.SetActive(false);
            for(int i=0;i<3;i++)SetButton(Button(f,secondary[i]),new Vector2((i-1)*350,-350),new Vector2(322,62),25);
            var campaignButton=Button(f,"campaign");
            f.campaignPage.transform.SetParent(campaignButton.transform,false);
            Place((RectTransform)f.campaignPage.transform,new Vector2(0,58),new Vector2(288,350));
            Place(campaignPanel.rectTransform,Vector2.zero,new Vector2(288,350));
            var heading=f.campaignPage.transform.Find("Campaign heading").GetComponent<Text>();Place(heading.rectTransform,new Vector2(0,116),new Vector2(265,62));heading.fontSize=24;
            SetButton(Button(f,"campaign-continue"),new Vector2(0,33),new Vector2(254,64),24);
            SetButton(Button(f,"campaign-new"),new Vector2(0,-50),new Vector2(254,64),24);
            var hint=f.campaignPage.transform.Find("Save hint").GetComponent<Text>();hint.text="ПКМ или клик вне карты — назад";hint.fontSize=17;Place(hint.rectTransform,new Vector2(0,-132),new Vector2(258,56));
            f.campaignChoice=Ensure<CampaignCardChoice>(campaignButton.gameObject);f.campaignChoice.illustration=(RectTransform)campaignButton.transform.Find("Mode illustration");f.campaignChoice.choices=(RectTransform)f.campaignPage.transform;
            f.campaignPage.transform.SetAsLastSibling();
            f.homePage.SetActive(true);f.playPage.SetActive(true);f.campaignPage.SetActive(false);
        }
        static void Settings(GameObject root)
        {
            var background=root.transform.Find("Background").GetComponent<Image>();background.color=new Color(.05f,.023f,.009f,.80f);
            var art=Ensure<RawImage>(Rect("Tavern backdrop",root.transform,Vector2.zero,new Vector2(2000,1250)).gameObject);art.texture=Resources.Load<Texture2D>("UI/LobbyBackdrop");art.raycastTarget=false;Stretch(art.rectTransform);art.transform.SetAsFirstSibling();background.transform.SetSiblingIndex(1);
            var left=Panel("Sound panel",root.transform,new Vector2(-367,-28),new Vector2(730,730));left.transform.SetSiblingIndex(2);var right=Panel("Display panel",root.transform,new Vector2(367,-28),new Vector2(730,730));right.transform.SetSiblingIndex(3);
            Panel("Settings title plaque",root.transform,new Vector2(0,409),new Vector2(700,80)).transform.SetSiblingIndex(4);
            var title=root.transform.Find("title").GetComponent<Text>();Place(title.rectTransform,new Vector2(0,409),new Vector2(670,66));title.alignment=TextAnchor.MiddleCenter;title.fontSize=44;
            Text("Sound heading",root.transform,"ЗВУК",new Vector2(-365,284),new Vector2(600,46),29);Text("Display heading",root.transform,"ЭКРАН",new Vector2(365,284),new Vector2(600,46),29);
            string[] audio={"master","effects","ambience","voices","music"};for(int i=0;i<audio.Length;i++){Place((RectTransform)root.transform.Find(audio[i]+"Label"),new Vector2(-365,220-i*80),new Vector2(620,34));Place((RectTransform)root.transform.Find(audio[i]),new Vector2(-365,181-i*80),new Vector2(602,20));}
            string[] display={"screenMode","resolution","refreshRate","frameLimit"};for(int i=0;i<4;i++){Place((RectTransform)root.transform.Find("displayLabel"+i),new Vector2(365,223-i*83),new Vector2(620,30));Place((RectTransform)root.transform.Find(display[i]),new Vector2(365,188-i*83),new Vector2(620,44));}
            Place((RectTransform)root.transform.Find("applyDisplay"),new Vector2(365,-145),new Vector2(620,46));Place((RectTransform)root.transform.Find("displayHint"),new Vector2(365,-187),new Vector2(620,30));
            Place((RectTransform)root.transform.Find("ShaderChoice"),new Vector2(-365,-246),new Vector2(620,95));
            Place((RectTransform)root.transform.Find("colorLabel"),new Vector2(320,-246),new Vector2(530,30));Place((RectTransform)root.transform.Find("Color preview"),new Vector2(650,-246),new Vector2(60,32));
            string[] rgb={"red","green","blue"};for(int i=0;i<3;i++){Place((RectTransform)root.transform.Find(rgb[i]),new Vector2(140+i*215,-299),new Vector2(172,18));Place((RectTransform)root.transform.Find(rgb[i]+"Label"),new Vector2(140+i*215,-338),new Vector2(186,26));}
            Place((RectTransform)root.transform.Find("note"),new Vector2(-365,-344),new Vector2(620,58));
            root.transform.Find("ConfigControls").gameObject.SetActive(false);
            foreach(var label in root.GetComponentsInChildren<Text>(true))label.color=Cream;
            foreach(var slider in root.GetComponentsInChildren<Slider>(true))
            {
                foreach(var image in slider.GetComponentsInChildren<Image>(true))image.color=image.transform==slider.handleRect?Cream:image.transform==slider.fillRect?new Color(.73f,.43f,.15f):new Color(.055f,.024f,.008f);
                if(slider.handleRect!=null){var size=slider.handleRect.sizeDelta;size.x=16;slider.handleRect.sizeDelta=size;}
            }
            foreach(var dropdown in root.GetComponentsInChildren<Dropdown>(true))
            {dropdown.image.color=new Color(.23f,.12f,.045f);var border=Ensure<CardFoilBorder>(Rect("Carved edge",dropdown.transform,Vector2.zero,((RectTransform)dropdown.transform).sizeDelta).gameObject);Stretch(border.rectTransform);border.thickness=2;border.color=Gold;border.raycastTarget=false;foreach(var image in dropdown.template.GetComponentsInChildren<Image>(true))image.color=new Color(.20f,.095f,.033f);}
            foreach(var shader in root.GetComponentsInChildren<ShaderChoiceView>(true)){shader.normal=Wood;shader.selected=new Color(.55f,.31f,.09f);}
            SetButton(root.transform.Find("resume").GetComponent<Button>(),new Vector2(-236,-433),new Vector2(420,60),25);SetButton(root.transform.Find("leave").GetComponent<Button>(),new Vector2(236,-433),new Vector2(420,60),25);
        }
        static void Tutorial(TutorialPanel p)
        {
            var canvas=p.GetComponent<Canvas>();canvas.sortingOrder=40;
            p.callout=(RectTransform)p.instruction.transform.parent;Place(p.callout,new Vector2(0,200),new Vector2(560,190));var panel=p.callout.GetComponent<TavernPanel>();panel.color=Wood;panel.border=Gold;
            Place(p.instruction.rectTransform,new Vector2(0,27),new Vector2(518,110));p.instruction.fontSize=23;p.instruction.color=Cream;
            SetButton(p.proceed,new Vector2(-135,-64),new Vector2(252,46),23);SetButton(p.retry,new Vector2(135,-64),new Vector2(244,46),23);
            p.shade=Ensure<FocusShade>(Rect("Tutorial focus shade",p.transform,Vector2.zero,Vector2.zero).gameObject);Stretch(p.shade.rectTransform);p.shade.tutorial=true;p.shade.strength=.79f;p.shade.transform.SetAsFirstSibling();
            p.secondaryCallout=Panel("Second target comment",p.transform,Vector2.zero,new Vector2(340,92)).rectTransform;p.secondaryLabel=Text("Comment",p.secondaryCallout,"",Vector2.zero,new Vector2(310,70),21);
            p.secondaryCallout.gameObject.SetActive(false);p.highlight.gameObject.SetActive(false);p.gameObject.SetActive(false);
        }
        static void Edit(string path,Action<GameObject> action){var root=PrefabUtility.LoadPrefabContents(path);try{action(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
        public static string Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Unsaved scene: "+SceneManager.GetSceneAt(i).path);
            Edit("Assets/Prefabs/MainMenuCanvas.prefab",Menu);Edit("Assets/Prefabs/Editable/UI/SettingsPanel.prefab",Settings);
            string vignettePath="Assets/Prefabs/Editable/UI/BattleVignette.prefab";var vg=new GameObject("Battle vignette",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));vg.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;vg.GetComponent<Canvas>().sortingOrder=-5;var shade=Ensure<FocusShade>(Rect("Soft edges",vg.transform,Vector2.zero,Vector2.zero).gameObject);Stretch(shade.rectTransform);shade.strength=.46f;vg.SetActive(false);PrefabUtility.SaveAsPrefabAsset(vg,vignettePath);Object.DestroyImmediate(vg);
            var previous=SceneManager.GetActiveScene();int scenes=0;
            foreach(string name in new[]{"MainMenu","Lobby","Match","PresentationLab"})
            {
                string path="Assets/Scenes/"+name+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                foreach(var front in scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<FrontEndCanvas>(true)))if(!front.lobby)Menu(front.gameObject);
                foreach(var settings in scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<UserSettingsView>(true)))Settings(settings.gameObject);
                foreach(var tutorial in scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<TutorialPanel>(true)))Tutorial(tutorial);
                foreach(var ui in scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<PrefabInterface>(true)))
                {if(ui.battleVignette==null){var obj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(vignettePath),scene);ui.battleVignette=obj.GetComponentInChildren<FocusShade>(true);}}
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);scenes++;if(opened)EditorSceneManager.CloseScene(scene,true);
            }
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);AssetDatabase.SaveAssets();return "Updated menu/settings/tutorial/vignette in "+scenes+" saved scenes";
        }
    }
}
