using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace SummonersTable.Editor
{
    // Explicit authoring operation; never called by Play or by the build pipeline.
    public static class RequestedGamePolish
    {
        static readonly string ButtonClip="Assets/ThirdParty/Audio/Card_Game/User_Interface/Buttons/Card_Game_UI_Button_Silly_Thunk_01.wav";
        static void Edit(string path,Action<GameObject> action){var root=PrefabUtility.LoadPrefabContents(path);try{action(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
        static void Sound(GameObject root)
        {
            foreach(var button in root.GetComponentsInChildren<Button>(true))
            {
                var feel=button.GetComponent<ButtonFeel>()??button.gameObject.AddComponent<ButtonFeel>();
                feel.clickSound=AssetDatabase.LoadAssetAtPath<AudioClip>(ButtonClip);feel.hoverSound=null;feel.soundVolume=.10f;
                var source=button.GetComponent<AudioSource>()??button.gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;
                var filter=button.GetComponent<AudioLowPassFilter>();if(filter==null)filter=button.gameObject.AddComponent<AudioLowPassFilter>();if(filter!=null)filter.cutoffFrequency=1700;
            }
        }
        static GameObject Group(Transform parent,string name){var r=CardTableCanvas.Rect(name,parent,Vector2.zero,new Vector2(1600,1000));return r.gameObject;}
        static void Menu(GameObject root)
        {
            var front=root.GetComponent<FrontEndCanvas>();if(front==null||front.lobby)return;
            if(front.homePage!=null){Sound(root);return;}
            var old=Group(root.transform,"Legacy menu controls — retained");foreach(var button in front.buttons)if(button!=null)button.transform.SetParent(old.transform,true);old.SetActive(false);
            front.homePage=Group(root.transform,"Home");front.playPage=Group(root.transform,"Play modes");front.campaignPage=Group(root.transform,"Campaign menu");
            var buttons=new List<Button>();var actions=new List<string>();
            void Add(GameObject group,string action,string label,int row){var b=SharedButton.Create(group.transform,action,label,new Vector2(0,140-row*86),new Vector2(540,64));buttons.Add(b);actions.Add(action);if(action=="campaign-continue")front.continueCampaign=b;}
            Add(front.homePage,"play","Играть",0);Add(front.homePage,"cards","Колоды и карты",1);Add(front.homePage,"settings","Настройки",2);Add(front.homePage,"quit","Выход",3);
            Add(front.playPage,"campaign","Кампания",0);Add(front.playPage,"steam","Играть онлайн",1);Add(front.playPage,"local","Играть локально",2);Add(front.playPage,"tutorial","Обучение",3);Add(front.playPage,"home","Назад",4);
            Add(front.campaignPage,"campaign-continue","Продолжить",0);Add(front.campaignPage,"campaign-new","Новая игра",1);Add(front.campaignPage,"play","Назад",3);
            front.buttons=buttons.ToArray();front.actions=actions.ToArray();front.playPage.SetActive(false);front.campaignPage.SetActive(false);Sound(root);
        }
        static void Tutorial(Scene scene)
        {
            if(scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TutorialPanel>(true)).Any())return;
            var root=new GameObject("Tutorial — editable Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(TutorialPanel));SceneManager.MoveGameObjectToScene(root,scene);
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=35;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,1000);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var panel=CardTableCanvas.Rect("Lesson",root.transform,new Vector2(0,310),new Vector2(760,174));var skin=panel.gameObject.AddComponent<TavernPanel>();skin.color=new Color(.07f,.13f,.13f,.98f);
            var lesson=root.GetComponent<TutorialPanel>();var text=CardTableCanvas.Rect("Instruction",panel,new Vector2(0,29),new Vector2(716,98)).gameObject.AddComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=22;text.alignment=TextAnchor.MiddleCenter;text.color=new Color(1,.96f,.87f);text.raycastTarget=false;lesson.instruction=text;
            lesson.proceed=SharedButton.Create(panel,"Continue","Продолжить",new Vector2(-150,-54),new Vector2(270,48));lesson.retry=SharedButton.Create(panel,"Retry","Повторить",new Vector2(150,-54),new Vector2(240,48));
            lesson.highlight=CardTableCanvas.Rect("Health highlight",root.transform,new Vector2(-630,-335),new Vector2(300,150));var border=lesson.highlight.gameObject.AddComponent<CardFoilBorder>();border.color=new Color(1,.8f,.2f);border.thickness=3;border.raycastTarget=false;Sound(root);root.SetActive(false);
        }
        static void Frames(GameObject root,Material material)
        {
            var face=root.GetComponent<LayeredCardView>();if(face==null)return;
            CardFoilBorder Border(string name,Vector2 size)
            {
                var existing=root.transform.Find(name);var rect=existing as RectTransform??CardTableCanvas.Rect(name,root.transform,Vector2.zero,size);
                var border=rect.GetComponent<CardFoilBorder>()??rect.gameObject.AddComponent<CardFoilBorder>();border.material=material;border.color=new Color(.72f,.51f,.23f,1);border.thickness=4;border.raycastTarget=false;rect.SetAsLastSibling();return border;
            }
            var outer=Border("Foil outer contour",new Vector2(600,940));outer.rectTransform.sizeDelta=new Vector2(600,940);
            var inner=Border("Foil illustration contour",face.artwork.rectTransform.sizeDelta);inner.rectTransform.anchoredPosition=face.artwork.rectTransform.anchoredPosition;face.artFrame=inner.rectTransform;
        }
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before authoring");
            var previous=SceneManager.GetActiveScene();
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Unsaved scene: "+SceneManager.GetSceneAt(i).path);
            Edit("Assets/Resources/UI/CommonButton.prefab",Sound);Edit("Assets/Prefabs/MainMenuCanvas.prefab",Menu);
            const string foilPath="Assets/Resources/Styles/CardFoilBorder.mat";var foil=AssetDatabase.LoadAssetAtPath<Material>(foilPath);if(foil==null){foil=new Material(Shader.Find("SummonersTable/Card Foil Border"));AssetDatabase.CreateAsset(foil,foilPath);}
            Edit(SharedPresentationMigration.FacePath,r=>Frames(r,foil));
            var registry=Resources.Load<ConfigAssets>("ConfigAssets");var entries=registry.entries.ToList();
            string Register(string path,string kind){var obj=AssetDatabase.LoadMainAssetAtPath(path);if(obj==null)throw new InvalidOperationException(path);var id="asset:"+AssetDatabase.AssetPathToGUID(path);if(!entries.Any(e=>e.id==id))entries.Add(new AssetRef{id=id,path=path,kind=kind,asset=obj});return id;}
            string trail=Register("Assets/ThirdParty/VFX/Vefects/Trails VFX URP/VFX/Materials/M_VFX_URP_Trail_Water_02.mat","material");
            string success=Register("Assets/ThirdParty/Audio/Card_Game/User_Interface/Buttons/Card_Game_UI_Button_Light_Magic_02.wav","audio");
            registry.entries=entries.ToArray();EditorUtility.SetDirty(registry);
            foreach(string directory in new[]{"Assets/StreamingAssets/Config","Assets/Resources/ConfigDefaults"})
            {
                var p=directory+"/interface.json";var config=JsonUtility.FromJson<InterfaceConfig>(File.ReadAllText(p));config.arrow.worldMaterial=trail;config.arrow.uiMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Styles/TargetTrailUI.mat")!=null?Register("Assets/Resources/Styles/TargetTrailUI.mat","material"):trail;config.arrow.width=16;File.WriteAllText(p,JsonUtility.ToJson(config,true));
                p=directory+"/audio.json";var audio=JsonUtility.FromJson<AudioConfig>(File.ReadAllText(p));var cue=audio.cues.First(c=>c.action=="qte.correct");cue.sounds=new[]{new SoundVariant{clip=success,volume=.13f,pitch=1.1f}};File.WriteAllText(p,JsonUtility.ToJson(audio,true));
            }
            var scenes=new List<Scene>();var opened=new List<Scene>();
            foreach(string name in new[]{"MainMenu","Lobby","Match"}){string path="Assets/Scenes/"+name+".unity";var scene=SceneManager.GetSceneByPath(path);if(!scene.IsValid()||!scene.isLoaded){scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);opened.Add(scene);}scenes.Add(scene);}
            // Each entry is self-contained. Copy saved roots once; no scenes load during gameplay.
            var donors=scenes.SelectMany(s=>s.GetRootGameObjects()).Where(g=>g.GetComponent<GameApp>()==null&&g.GetComponent<UnityEngine.EventSystems.EventSystem>()==null).GroupBy(g=>g.name).Select(g=>g.First()).ToArray();
            foreach(var scene in scenes)
            {
                SceneManager.SetActiveScene(scene);
                foreach(var donor in donors)if(!scene.GetRootGameObjects().Any(g=>g.name==donor.name)){var clone=Object.Instantiate(donor);clone.name=donor.name;SceneManager.MoveGameObjectToScene(clone,scene);}
                foreach(var root in scene.GetRootGameObjects()){var front=root.GetComponent<FrontEndCanvas>();if(front!=null&&front.lobby)continue;if(front!=null)Menu(root);Sound(root);}
                var board=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TableBoard>(true)).Single();
                var background=scene.GetRootGameObjects().FirstOrDefault(g=>g.GetComponent<MenuBackdropCamera>()!=null);
                if(background==null){background=new GameObject("Menu and startup camera",typeof(Camera),typeof(MenuBackdropCamera));SceneManager.MoveGameObjectToScene(background,scene);}
                var camera=background.GetComponent<Camera>();camera.cullingMask=0;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.045f,.055f);camera.depth=-100;camera.enabled=true;background.GetComponent<MenuBackdropCamera>().board=board;
                Tutorial(scene);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            foreach(var s in opened)EditorSceneManager.CloseScene(s,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            var media=AssetDatabase.FindAssets("t:CampaignMedia").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<CampaignMedia>).FirstOrDefault();
            if(media!=null)
            {
                var sounds=media.sounds.ToList();var opening=JsonUtility.FromJson<SummonersTable.Story.StoryConfig>(Resources.Load<TextAsset>("Campaign/approved-opening").text);
                foreach(string id in opening.scenes.SelectMany(s=>s.steps).SelectMany(s=>new[]{s.audio.bgm,s.audio.sfx}).Where(s=>!string.IsNullOrEmpty(s)&&s!="Mute"&&!s.StartsWith("action:")).Distinct())
                {
                    if(sounds.Any(s=>s.id==id))continue;string stem=Path.GetFileNameWithoutExtension(id);
                    var path=AssetDatabase.FindAssets(stem+" t:AudioClip").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p=>Path.GetFileNameWithoutExtension(p).EndsWith(stem,StringComparison.OrdinalIgnoreCase));
                    if(path!=null)sounds.Add(new CampaignSound{id=id,clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path)});
                }
                media.sounds=sounds.ToArray();EditorUtility.SetDirty(media);
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("REQUESTED_POLISH_AUTHORED");
        }
    }
}
