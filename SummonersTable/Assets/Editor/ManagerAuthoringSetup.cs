using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace SummonersTable.Editor
{
    public static class ManagerAuthoringSetup
    {
        const string Root="Assets/Prefabs/Editable/",Folder="Assets/StreamingAssets/Config";
        static GameObject Asset(string path)=>AssetDatabase.LoadAssetAtPath<GameObject>(path);
        public static void Run()
        {
            Backup();MigrateAudio();MigrateLocation();CreateAnimationDefaults();CreateInterfaceDefaults();
            CreateAudioRig();ManagerSettingsPrefab.Create();AddSettingsButton();CreateManagerPrefabs();AttachWorldManager();CreateScenes();
            ConfigAuthoring.BuildAssetRegistry();ConfigAuthoring.CopyDefaults();ConfigAuthoring.SyncCards();AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            ConfigBundle.Read(Folder);
            foreach(var manager in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/Managers"}).Select(AssetDatabase.GUIDToAssetPath).Select(p=>Asset(p).GetComponent<AuthoringManager>()))ManagerStorage.Save(manager);
            EditorSceneManager.OpenScene("Assets/Scenes/Authoring/LocationLab.unity");Debug.Log("MANAGER_AUTHORING_READY");
        }
        static void Backup()
        {
            string folder="../output/backups/managers-v0.7";if(Directory.Exists(folder))return;Directory.CreateDirectory(folder);
            foreach(string file in Directory.GetFiles(Folder,"*.json"))File.Copy(file,Path.Combine(folder,Path.GetFileName(file)),true);
            foreach(string path in new[]{ConfigAuthoring.WorldPath,Root+"World/PlayerSeat.prefab",Root+"UI/SettingsPanel.prefab","Assets/Prefabs/HeroSittingPlace.prefab"})File.Copy(path,Path.Combine(folder,Path.GetFileName(path)),true);
        }
        static void MigrateAudio()
        {
            var json=ConfigJson.Object(ConfigJson.Parse(File.ReadAllText(Folder+"/audio.json")));var cues=(List<object>)json["cues"];
            foreach(var item in cues)
            {
                var cue=ConfigJson.Object(item);if(cue.ContainsKey("sounds"))continue;string action=(string)cue["action"];
                var sound=new SoundVariant{clip=(string)cue["clip"],volume=Convert.ToSingle(cue["volume"]),pitch=Convert.ToSingle(cue["pitch"]),delay=Convert.ToSingle(cue["delay"]),randomPitch=action=="card.hover"||action=="qte.correct"};
                cue.Remove("clip");cue.Remove("volume");cue.Remove("pitch");cue.Remove("delay");cue["bus"]=action=="ambience"?1:0;cue["sounds"]=new List<object>{ConfigJson.Parse(JsonUtility.ToJson(sound))};
            }
            ConfigAuthoring.Write("audio.json",ConfigAuthoring.Pretty(ConfigJson.Write(json)));
        }
        static void MigrateLocation()
        {
            string seatPath=Root+"World/PlayerSeat.prefab";var seat=PrefabUtility.LoadPrefabContents(seatPath);var view=seat.GetComponent<PlayerSeatView>();
            if(view.body==null)
            {
                Vector3 pivot=new Vector3(0,0,-8.48f);foreach(Transform child in seat.transform)child.localPosition-=pivot;
                view.body=new GameObject("Body — кресло, персонаж, рука, UI").transform;view.body.SetParent(seat.transform,false);
                view.chair.SetParent(view.body,true);view.avatar.transform.SetParent(view.body,true);
                Transform Anchor(string name,Vector3 position){var a=new GameObject(name).transform;a.SetParent(view.body,false);a.localPosition=position;return a;}
                view.handAnchor=Anchor("Hand anchor",new Vector3(3.1f,7.35f,.8f));view.nameAnchor=Anchor("Name anchor",new Vector3(0,12.8f,-.2f));view.healthAnchor=Anchor("Health anchor",new Vector3(0,6.28f,2.12f));
                view.fan=view.handAnchor.gameObject.AddComponent<WorldHandFanSettings>();view.fan.anchor=view.handAnchor;
                var canvas=new GameObject("Player UI",typeof(RectTransform),typeof(Canvas));canvas.transform.SetParent(view.body,false);canvas.transform.localScale=Vector3.one*.018f;((RectTransform)canvas.transform).sizeDelta=new Vector2(1600,1000);canvas.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;canvas.GetComponent<Canvas>().sortingOrder=8;
                var panel=(GameObject)PrefabUtility.InstantiatePrefab(Asset(Root+"UI/PlayerStatus.prefab"),canvas.transform);view.status=panel.GetComponent<PlayerStatusView>();view.Assign(0);
                foreach(var r in view.status.healthAnchor.GetComponentsInChildren<RectTransform>(true))if(r!=view.status.healthAnchor){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=Vector2.one*2;r.offsetMax=-Vector2.one*2;}
                var shader=view.chair.GetComponent<ShaderStyleTarget>()??view.chair.gameObject.AddComponent<ShaderStyleTarget>();shader.targets=view.chair.GetComponentsInChildren<Renderer>(true);
                PrefabUtility.SaveAsPrefabAsset(seat,seatPath);
            }
            PrefabUtility.UnloadPrefabContents(seat);
            string alias="Assets/Prefabs/HeroSittingPlace.prefab";
            var sitting=(GameObject)PrefabUtility.InstantiatePrefab(Asset(seatPath));sitting.name="HeroSittingPlace";PrefabUtility.SaveAsPrefabAsset(sitting,alias);Object.DestroyImmediate(sitting);
            var world=PrefabUtility.LoadPrefabContents(ConfigAuthoring.WorldPath);var board=world.GetComponent<TableBoard>();
            if(board.seating==null)
            {
                Directory.CreateDirectory("Assets/Prefabs/Locations");AssetDatabase.Refresh();
                var environment=Object.Instantiate(board.authoredEnvironment.gameObject);environment.name="TavernInterior";environment.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                var style=environment.GetComponent<ShaderStyleTarget>()??environment.AddComponent<ShaderStyleTarget>();style.targets=environment.GetComponentsInChildren<Renderer>(true);
                environment.AddComponent<PrefabConfigBinding>().configId="TavernInterior";
                var interior=PrefabUtility.SaveAsPrefabAsset(environment,"Assets/Prefabs/Locations/TavernInterior.prefab");Object.DestroyImmediate(environment);
                Object.DestroyImmediate(board.authoredEnvironment.gameObject);board.authoredEnvironment=((GameObject)PrefabUtility.InstantiatePrefab(interior,world.transform)).transform;board.interiorId=ManagerAssets.Register(interior,"interior");
                foreach(var layout in board.layouts)if(layout!=null)Object.DestroyImmediate(layout.gameObject);
                var root=new GameObject("Generated player places");root.transform.SetParent(world.transform,false);board.seating=root.AddComponent<SeatingLayout>();board.seating.layout=root.AddComponent<TableLayout>();board.seating.seatPrefab=Asset(alias).GetComponent<PlayerSeatView>();board.layouts=Array.Empty<TableLayout>();
                var oldStyle=world.GetComponent<ShaderStyleTarget>();if(oldStyle!=null)oldStyle.targets=board.authoredEnvironment.GetComponentsInChildren<Renderer>(true);
                PrefabUtility.SaveAsPrefabAsset(world,ConfigAuthoring.WorldPath);
                var config=new LocationConfig{scene=board.interiorId,events=new[]{new LocationEventRule{id=0,enabled=false,eventId=3,chance=30,duration=2}}};
                ConfigAuthoring.Write("world.json",JsonUtility.ToJson(config,true));
            }
            PrefabUtility.UnloadPrefabContents(world);
            if(!File.Exists(Folder+"/events.json"))
            {
                var vfx=ConfigJson.Read<VfxConfig>(File.ReadAllText(Folder+"/vfx.json"));ConfigAuthoring.Write("events.json",JsonUtility.ToJson(new EventsConfig{events=new[]{new WorldEventDef{vfx=vfx.hit}}},true));
            }
            // JSON no longer overwrites values that designers edit on the card prefab.
            var profiles=ConfigJson.Object(ConfigJson.Parse(File.ReadAllText(Folder+"/prefabs.json")));
            foreach(var profile in (List<object>)profiles["prefabs"]){var list=(List<object>)ConfigJson.Object(profile)["components"];list.RemoveAll(v=>(string)ConfigJson.Object(v)["type"]=="SummonersTable.CardDepthVisual");}
            ConfigAuthoring.Write("prefabs.json",ConfigAuthoring.Pretty(ConfigJson.Write(profiles)));
        }
        static void CreateAnimationDefaults()
        {
            if(File.Exists(Folder+"/playeranimations.json"))return;
            var definitions=new Dictionary<string,string[]>{{"idle",new[]{"Idle_Normal"}},{"turn",new[]{"Defend"}},{"attack",new[]{"AttackCombo04"}},{"hit",new[]{"GetHit"}},{"dead",new[]{"Die","AttackCombo05"}},{"revive",new[]{"DieRecover"}},{"cameraForward",new[]{"DashForward"}},{"cameraBack",new[]{"DashBackward"}},{"dizzy",new[]{"Dizzy"}},{"reaction",new[]{"Sliding"}},{"victory",new[]{"AttackCombo04"}}};
            var states=definitions.Select(pair=>new AnimationStateDef{id=pair.Key,steps=pair.Value.Select((clip,i)=>new AnimationStep{clip=ManagerAssets.Animation(clip),loop=i==pair.Value.Length-1&&(pair.Key=="idle"||pair.Key=="turn"||pair.Key=="dead"||pair.Key=="victory")}).ToArray()}).ToArray();
            ConfigAuthoring.Write("playeranimations.json",JsonUtility.ToJson(new PlayerAnimationsConfig{states=states},true));
        }
        static void CreateInterfaceDefaults()
        {
            if(!File.Exists(Folder+"/interface.json"))ConfigAuthoring.Write("interface.json",JsonUtility.ToJson(new InterfaceConfig(),true));
            var rules=ConfigJson.Read<RulesConfig>(File.ReadAllText(Folder+"/rules.json"));rules.version="0.8.0";ConfigAuthoring.Write("rules.json",JsonUtility.ToJson(rules,true));
        }
        static void CreateAudioRig()
        {
            if(File.Exists("Assets/Resources/AudioRig.prefab"))return;
            var root=new GameObject("AudioRig");var rig=root.AddComponent<ConfigAudio>();rig.buses=new Transform[4];
            for(int i=0;i<4;i++){var child=new GameObject(((AudioBus)i).ToString());child.transform.SetParent(root.transform,false);rig.buses[i]=child.transform;}
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/AudioRig.prefab");Object.DestroyImmediate(root);
        }
        static void AddSettingsButton()
        {
            string path="Assets/Prefabs/MainMenuCanvas.prefab";var root=PrefabUtility.LoadPrefabContents(path);var menu=root.GetComponent<FrontEndCanvas>();
            if(!menu.actions.Contains("settings"))
            {
                var buttons=menu.buttons.ToList();var actions=menu.actions.ToList();var button=Object.Instantiate(buttons.Last(),root.transform);button.name="Settings";button.GetComponentInChildren<Text>().text="Настройки";buttons.Insert(4,button);actions.Insert(4,"settings");menu.buttons=buttons.ToArray();menu.actions=actions.ToArray();
                for(int i=0;i<buttons.Count;i++)((RectTransform)buttons[i].transform).anchoredPosition=new Vector2(0,120-i*80);
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            PrefabUtility.UnloadPrefabContents(root);
        }
        static void CreateManagerPrefabs()
        {
            Directory.CreateDirectory("Assets/Prefabs/Managers");AssetDatabase.Refresh();
            foreach(ManagerSection section in Enum.GetValues(typeof(ManagerSection)))
            {
                string path="Assets/Prefabs/Managers/"+section+"Manager.prefab";if(File.Exists(path))continue;
                var root=new GameObject(section+" Manager");root.AddComponent<PrefabConfigBinding>().configId=section+"Manager";
                var manager=root.AddComponent<AuthoringManager>();manager.section=section;manager.Import(File.ReadAllText(Path.Combine(Folder,manager.FileName)));
                PrefabUtility.SaveAsPrefabAsset(root,path);Object.DestroyImmediate(root);
            }
        }
        static void AttachWorldManager()
        {
            var root=PrefabUtility.LoadPrefabContents(ConfigAuthoring.WorldPath);var board=root.GetComponent<TableBoard>();
            if(root.GetComponentInChildren<AuthoringManager>(true)==null)
            {
                var manager=((GameObject)PrefabUtility.InstantiatePrefab(Asset("Assets/Prefabs/Managers/WorldManager.prefab"),root.transform)).GetComponent<AuthoringManager>();manager.table=board;PrefabUtility.SaveAsPrefabAsset(root,ConfigAuthoring.WorldPath);
            }
            PrefabUtility.UnloadPrefabContents(root);
        }
        static void CreateScenes()
        {
            Directory.CreateDirectory("Assets/Scenes/Authoring");AssetDatabase.Refresh();
            void Scene(string name,params ManagerSection[] sections)
            {
                string path="Assets/Scenes/Authoring/"+name+".unity";if(File.Exists(path))return;
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                new GameObject("Инструкция — выбирайте Manager").AddComponent<AuthoringLab>();
                var world=((GameObject)PrefabUtility.InstantiatePrefab(Asset(ConfigAuthoring.WorldPath))).GetComponent<TableBoard>();world.gameObject.SetActive(true);
                var anchor=new GameObject("Preview anchor").transform;anchor.position=new Vector3(0,TableBoard.TableTop+2,0);
                foreach(var section in sections)
                {
                    var manager=((GameObject)PrefabUtility.InstantiatePrefab(Asset("Assets/Prefabs/Managers/"+section+"Manager.prefab"))).GetComponent<AuthoringManager>();manager.table=world;manager.previewAnchor=anchor;
                }
                EditorSceneManager.SaveScene(scene,path);
            }
            Scene("LocationLab",ManagerSection.Events,ManagerSection.Rules);
            Scene("EffectsLab",ManagerSection.Vfx,ManagerSection.Audio,ManagerSection.Events);
            Scene("AnimationLab",ManagerSection.PlayerAnimations,ManagerSection.Audio);
            Scene("CardsLab",ManagerSection.Cards,ManagerSection.Decks,ManagerSection.Interface,ManagerSection.Presentation);
        }
        [MenuItem("Summoners Table/Authoring/Локация")]public static void OpenWorld()=>Open("LocationLab");
        [MenuItem("Summoners Table/Authoring/Эффекты и звук")]public static void OpenEffects()=>Open("EffectsLab");
        [MenuItem("Summoners Table/Authoring/Анимации")]public static void OpenAnimations()=>Open("AnimationLab");
        [MenuItem("Summoners Table/Authoring/Карты и интерфейс")]public static void OpenCards()=>Open("CardsLab");
        static void Open(string name){if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene("Assets/Scenes/Authoring/"+name+".unity");}
    }
}
