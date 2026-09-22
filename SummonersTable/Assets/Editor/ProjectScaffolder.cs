using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace SummonersTable.Editor
{
    public static class ProjectScaffolder
    {
        static Material gray,dark;
        public static readonly string[] ScenePaths={"Assets/Scenes/MainMenu.unity","Assets/Scenes/Lobby.unity","Assets/Scenes/Match.unity","Assets/Scenes/PresentationLab.unity"};
        [MenuItem("Summoners Table/Create missing scenes and prefabs")]
        public static void Generate()
        {
            Directory.CreateDirectory("Assets/Prefabs");Directory.CreateDirectory("Assets/Scenes");Directory.CreateDirectory("Assets/Settings");
            var settings=AssetDatabase.LoadAssetAtPath<PresentationSettings>("Assets/Resources/PresentationSettings.asset");
            if(settings==null){settings=ScriptableObject.CreateInstance<PresentationSettings>();AssetDatabase.CreateAsset(settings,"Assets/Resources/PresentationSettings.asset");}
            gray=Material("Gray placeholder",new Color(.42f,.44f,.46f));dark=Material("Slot placeholder",new Color(.16f,.18f,.21f));
            var chair=PrimitivePrefab("Chair placeholder",PrimitiveType.Cube,new Vector3(1.35f,.95f,1.35f),gray);
            var avatar=PrimitivePrefab("Avatar placeholder",PrimitiveType.Cube,new Vector3(.9f,1.5f,.8f),gray);
            var table=PrimitivePrefab("Round table placeholder",PrimitiveType.Cylinder,new Vector3(12,.3f,12),gray);
            string path="Assets/Prefabs/TableWorld.prefab";
            if(!File.Exists(path))
            {
                var root=new GameObject("Table World");var board=root.AddComponent<TableBoard>();board.tablePrefab=table;board.chairPrefab=chair;board.avatarPrefab=avatar;
                var environment=new GameObject("Editable environment");environment.transform.SetParent(root.transform);board.authoredEnvironment=environment.transform;
                Instance(table,environment.transform,new Vector3(0,.7f,0));
                Primitive("Floor",PrimitiveType.Cube,environment.transform,new Vector3(0,-.13f,0),new Vector3(28,.2f,28),dark);
                var center=Primitive("Center — random enemy hero",PrimitiveType.Cylinder,environment.transform,new Vector3(0,TableBoard.TableTop,0),new Vector3(1.5f,.018f,1.5f),gray);center.AddComponent<BoardTarget>().kind="center";
                var light=new GameObject("Key light").AddComponent<Light>();light.transform.SetParent(environment.transform);light.type=LightType.Directional;light.intensity=1.25f;light.transform.rotation=Quaternion.Euler(48,-32,0);
                board.layouts=new TableLayout[3];
                for(int n=2;n<=4;n++)
                {
                    var layout=new GameObject(n+" players — edit seat and slot anchors").AddComponent<TableLayout>();layout.transform.SetParent(root.transform);layout.playerCount=n;layout.heroes=new Transform[n];layout.slotAnchors=new Transform[n*5];layout.avatars=new GameObject[n];board.layouts[n-2]=layout;
                    for(int i=0;i<n;i++)
                    {
                        var seat=new GameObject("Seat "+i);seat.transform.SetParent(layout.transform);
                        var away=TableBoard.Away(i,n);var hero=Primitive("Hero target",PrimitiveType.Cube,seat.transform,TableBoard.HeroPosition(i,n),new Vector3(1.25f,2,1.25f),gray);
                        hero.GetComponent<Renderer>().enabled=false;var target=hero.AddComponent<BoardTarget>();target.kind="hero";target.seat=i;layout.heroes[i]=hero.transform;
                        var body=Instance(avatar,seat.transform,hero.transform.position);body.transform.rotation=Quaternion.LookRotation(-away);layout.avatars[i]=body;
                        Instance(chair,seat.transform,away*7+Vector3.up*.48f).transform.rotation=Quaternion.LookRotation(-away);
                        for(int slot=0;slot<5;slot++)
                        {
                            var anchor=Primitive("Slot "+slot,PrimitiveType.Cube,seat.transform,TableBoard.SlotPosition(i,slot,n),new Vector3(.96f,.045f,1.25f),dark);anchor.transform.rotation=Quaternion.LookRotation(-away);target=anchor.AddComponent<BoardTarget>();target.kind="slot";target.seat=i;target.slot=slot;layout.slotAnchors[i*5+slot]=anchor.transform;
                        }
                    }
                    layout.gameObject.SetActive(n==4);
                }
                var cam=new GameObject("Table camera").AddComponent<Camera>();cam.transform.SetParent(root.transform);cam.tag="MainCamera";cam.gameObject.AddComponent<AudioListener>();cam.gameObject.AddComponent<ManualTableCamera>().settings=settings;board.tableCamera=cam;
                PrefabUtility.SaveAsPrefabAsset(root,path);UnityEngine.Object.DestroyImmediate(root);
            }
            if(!File.Exists("Assets/Prefabs/CardTableCanvas.prefab"))
            {var o=new GameObject("Card presentation — three slots");o.AddComponent<CardTableCanvas>().Build();PrefabUtility.SaveAsPrefabAsset(o,"Assets/Prefabs/CardTableCanvas.prefab");UnityEngine.Object.DestroyImmediate(o);}
            foreach(bool lobby in new[]{false,true})
            {
                string file="Assets/Prefabs/"+(lobby?"LobbyCanvas":"MainMenuCanvas")+".prefab";
                if(File.Exists(file))continue;var o=new GameObject(lobby?"Lobby UI":"Main menu UI");o.AddComponent<FrontEndCanvas>().Build(lobby);PrefabUtility.SaveAsPrefabAsset(o,file);UnityEngine.Object.DestroyImmediate(o);
            }
            for(int i=0;i<ScenePaths.Length;i++)
            {
                if(File.Exists(ScenePaths[i]))continue;
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                if(i<2)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/"+(i==0?"MainMenuCanvas":"LobbyCanvas")+".prefab"));
                else
                {
                    PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TableWorld.prefab"));
                    PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CardTableCanvas.prefab"));
                }
                if(i==0||i==3){var events=new GameObject("UI Event System");events.AddComponent<EventSystem>();events.AddComponent<StandaloneInputModule>();}
                if(i==3){var lab=new GameObject("Presentation laboratory").AddComponent<PresentationLab>();lab.settings=settings;lab.musicSource=lab.gameObject.AddComponent<AudioSource>();lab.musicSource.clip=settings.music;lab.musicSource.loop=true;lab.musicSource.playOnAwake=false;}
                EditorSceneManager.SaveScene(scene,ScenePaths[i]);
            }
            EditorBuildSettings.scenes=Array.ConvertAll(ScenePaths,p=>new EditorBuildSettingsScene(p,true));
            File.WriteAllText("Assets/Settings/presentation.defaults.json",settings.ToJson());AssetDatabase.Refresh();AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(ScenePaths[0]);Debug.Log("PRESENTATION_SCAFFOLD_READY");
        }
        static Material Material(string name,Color color)
        {string path="Assets/Settings/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;m=new Material(Resources.Load<Shader>("BoardGray"));m.color=color;AssetDatabase.CreateAsset(m,path);return m;}
        static GameObject Primitive(string name,PrimitiveType type,Transform parent,Vector3 position,Vector3 scale,Material material)
        {var o=GameObject.CreatePrimitive(type);o.name=name;o.transform.SetParent(parent);o.transform.localPosition=position;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=material;return o;}
        static GameObject PrimitivePrefab(string name,PrimitiveType type,Vector3 scale,Material material)
        {
            string path="Assets/Prefabs/"+name+".prefab";var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(asset!=null)return asset;
            var root=new GameObject(name);Primitive("Replaceable visual",type,root.transform,Vector3.zero,scale,material);asset=PrefabUtility.SaveAsPrefabAsset(root,path);UnityEngine.Object.DestroyImmediate(root);return asset;
        }
        static GameObject Instance(GameObject prefab,Transform parent,Vector3 position)
        {var o=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);o.transform.localPosition=position;return o;}
    }
    [CustomEditor(typeof(PresentationSettings))]
    public sealed class PresentationSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();var settings=(PresentationSettings)target;
            if(GUILayout.Button("Export JSON…"))
            {string path=EditorUtility.SaveFilePanel("Save presentation settings","Assets/Settings","presentation","json");if(path!="")File.WriteAllText(path,settings.ToJson());}
            if(GUILayout.Button("Import JSON…"))
            {string path=EditorUtility.OpenFilePanel("Load presentation settings","Assets/Settings","json");if(path!="")try{Undo.RecordObject(settings,"Import presentation settings");settings.ImportJson(File.ReadAllText(path));EditorUtility.SetDirty(settings);}catch(Exception e){EditorUtility.DisplayDialog("Invalid settings",e.Message,"OK");}}
        }
    }
}
