using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using TMPro;
using Michsky.UI.MTP;

namespace SummonersTable.Editor
{
    public static class TavernSetup
    {
        const string Root="Assets/ThirdParty/",Animal=Root+"3dModels/ModularAnimalKnightsPolyart/",Destination="Assets/Presentation/";
        public static void Build(){Run();BuildTools.BuildWindows();}
        [MenuItem("Summoners Table/Upgrade tavern assets and lobby")]
        public static void Run()
        {
            if(Resources.Load<TMP_Settings>("TMP Settings")==null)
            {AssetDatabase.ImportPackage(Directory.GetFiles("Library/PackageCache","TMP Essential Resources.unitypackage",SearchOption.AllDirectories).First(),false);AssetDatabase.Refresh();}
            Directory.CreateDirectory(Destination+"Materials");Directory.CreateDirectory(Destination+"Effects");AssetDatabase.Refresh();
            var library=AssetDatabase.LoadAssetAtPath<HeroLibrary>("Assets/Resources/HeroLibrary.asset");
            if(library==null){library=ScriptableObject.CreateInstance<HeroLibrary>();AssetDatabase.CreateAsset(library,"Assets/Resources/HeroLibrary.asset");}
            var animalFiles=new[]{"Badger","Deer","Dog","Lion","Lizard","Owl","Rabit","Rat"};
            library.heroes=Enumerable.Range(0,8).Select(i=>new HeroDefinition{id=HeroOptions.Ids[i],name=HeroOptions.Names[i],prefab=Load<GameObject>(Animal+"Prefab/MaskTint/"+animalFiles[i]+"MaskTint.prefab")}).ToArray();
            library.heroScale=1.65f;library.seatedHeight=0;
            library.chair=Load<GameObject>(Root+"3dModels/room_items/SM_Armchair.fbx");
            library.table=Load<GameObject>(Root+"3dModels/room_items/SM_Table.fbx");
            library.naturalMaterial=Load<Material>(Animal+"Material/PolyartStandard.mat");
            string controllerPath=Destination+"TavernHero.controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)??AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            if(controller.layers.Length==0)controller.AddLayer("Base Layer");
            var sm=controller.layers[0].stateMachine;
            foreach(string clipName in new[]{"Idle_Normal","Defend","AttackCombo04","Die","AttackCombo05","DashForward","DashBackward","DieRecover","Dizzy","GetHit","Sliding"})
            {
                string file=clipName=="DieRecover"?"DIeRecover":clipName;
                var clip=AssetDatabase.LoadAllAssetsAtPath(Animal+"Animation/"+file+".fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
                var state=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name==clipName)??sm.AddState(clipName);state.motion=clip;
                if(clipName=="Idle_Normal")sm.defaultState=state;
            }
            library.controller=controller;EditorUtility.SetDirty(controller);EditorUtility.SetDirty(sm);EditorUtility.SetDirty(library);
            var settings=Resources.Load<PresentationSettings>("PresentationSettings");
            settings.cardHover=Audio("Card_Game_Items_Pour_Small_01");settings.invalidAction=Audio("Card_Game_Alert_Your_Turn_01");
            settings.attack=Audio("Card_Game_Abilities_Air_Puff_Short_01");settings.hit=Audio("Card_Game_Abilities_Pop_Big_01");
            settings.qteSuccess=Audio("Card_Game_UI_Notification_Ding_01");settings.qteError=Audio("Card_Game_UI_Notification_Silly_Squawk_01");settings.turnNotice=Audio("Card_Game_Alert_Your_Turn_01");
            settings.music=Audio("Card_Game_Ambience_Forest");
            settings.attackEffect=Effect("Attack trail",Root+"VFX/Vefects/Trails VFX URP/VFX/Particles/VFX_Trail_Fire.prefab");
            settings.impactEffect=Effect("Hit flash",Root+"VFX/Lana Studio/Hyper Casual FX/Prefabs/Flash/Flash_star_ellow_purple.prefab");
            settings.deathEffect=Effect("Death smoke",Root+"VFX/VFXPACK_FIRE_WALLCOEUR/Prefab/VFX_Smoke.prefab");
            settings.qteFire=Effect("QTE fire",Root+"VFX/VFXPACK_FIRE_WALLCOEUR/Prefab/VFX_Fire.prefab");
            settings.qteSmoke=Effect("QTE smoke",Root+"VFX/VFXPACK_FIRE_WALLCOEUR/Prefab/VFX_BlackSmoke.prefab");
            settings.qteAttempt=Effect("QTE attempt",Root+"VFX/VFXPACK_FIRE_WALLCOEUR/Prefab/VFX_Fire_Green.prefab");settings.qteEffectScale=.42f;
            settings.motionTitle=Load<GameObject>(Root+"VFX/Motion Titles Pack/Prefabs/MTP-9.prefab");EditorUtility.SetDirty(settings);
            BuildLobby(library);UpgradeTable(library);BuildAnnouncements(settings);
            foreach(string path in new[]{"Assets/Scenes/Match.unity","Assets/Scenes/PresentationLab.unity"})
            {
                var scene=EditorSceneManager.OpenScene(path);
                if(UnityEngine.Object.FindFirstObjectByType<MotionAnnouncements>(FindObjectsInactive.Include)==null)PrefabUtility.InstantiatePrefab(Load<GameObject>("Assets/Prefabs/MotionAnnouncements.prefab"));
                var lab=UnityEngine.Object.FindFirstObjectByType<PresentationLab>();if(lab!=null){lab.musicSource.clip=settings.music;lab.musicSource.volume=.18f;}
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");Debug.Log("TAVERN_ASSETS_READY");
        }
        static T Load<T>(string path) where T:UnityEngine.Object {var a=AssetDatabase.LoadAssetAtPath<T>(path);if(a==null)throw new Exception("Missing asset: "+path);return a;}
        static AudioClip Audio(string name){string path=AssetDatabase.GetAllAssetPaths().First(p=>p.StartsWith(Root+"Audio/")&&Path.GetFileNameWithoutExtension(p)==name&&p.EndsWith(".wav"));return Load<AudioClip>(path);}
        static GameObject Effect(string name,string path)
        {
            var obj=UnityEngine.Object.Instantiate(Load<GameObject>(path));obj.name=name;
            foreach(var script in obj.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(script);
            foreach(var light in obj.GetComponentsInChildren<Light>(true))UnityEngine.Object.DestroyImmediate(light);
            foreach(var audio in obj.GetComponentsInChildren<AudioSource>(true))UnityEngine.Object.DestroyImmediate(audio);
            foreach(var renderer in obj.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>ParticleMaterial(m)).ToArray();
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            }
            foreach(var ps in obj.GetComponentsInChildren<ParticleSystem>(true)){var main=ps.main;main.scalingMode=ParticleSystemScalingMode.Hierarchy;main.simulationSpace=ParticleSystemSimulationSpace.Local;}
            var prefab=PrefabUtility.SaveAsPrefabAsset(obj,Destination+"Effects/"+name+".prefab");UnityEngine.Object.DestroyImmediate(obj);return prefab;
        }
        public static GameObject PrepareEffect(string name,string path)
        {
            var existing=AssetDatabase.LoadAssetAtPath<GameObject>(Destination+"Effects/"+name+".prefab");
            return existing!=null?existing:Effect(name,path);
        }
        static Material ParticleMaterial(Material source)
        {
            if(source==null)return null;
            string path=Destination+"Materials/"+source.name+"-"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source)).Substring(0,8)+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material!=null)return material;
            material=new Material(Resources.Load<Shader>("ImportedParticle"));
            var textures=source.GetTexturePropertyNames();string texture=textures.FirstOrDefault(t=>(t=="_MainTex"||t=="_BaseMap"||t=="_BaseTexture")&&source.GetTexture(t)!=null)??textures.FirstOrDefault(t=>source.GetTexture(t)!=null);
            if(texture!=null){material.mainTexture=source.GetTexture(texture);material.mainTextureScale=source.GetTextureScale(texture);material.mainTextureOffset=source.GetTextureOffset(texture);}
            material.color=source.HasProperty("_BaseColor")?source.GetColor("_BaseColor"):source.HasProperty("_Color")?source.GetColor("_Color"):Color.white;
            AssetDatabase.CreateAsset(material,path);return material;
        }
        static void BuildLobby(HeroLibrary library)
        {
            var root=new GameObject("Lobby UI");var ui=root.AddComponent<FrontEndCanvas>();ui.Build(true);
            var studio=new GameObject("Portrait studio — editable cameras and lights");studio.transform.SetParent(root.transform,false);studio.transform.localPosition=new Vector3(0,1000,0);
            for(int i=0;i<4;i++)
            {
                var place=new GameObject("Hero preview "+i);place.transform.SetParent(studio.transform,false);place.transform.localPosition=Vector3.right*i*12;
                var model=new GameObject("Animated animal knight");model.layer=28;model.transform.SetParent(place.transform,false);model.transform.localRotation=Quaternion.Euler(0,-8,0);
                var actor=model.AddComponent<HeroActor>();actor.library=library;
                var cam=new GameObject("Portrait camera").AddComponent<Camera>();cam.transform.SetParent(place.transform,false);cam.transform.localPosition=new Vector3(0,.86f,3.1f);cam.transform.localRotation=Quaternion.LookRotation(new Vector3(0,.81f,0)-cam.transform.localPosition);cam.fieldOfView=32;cam.nearClipPlane=.01f;cam.farClipPlane=15;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.clear;cam.cullingMask=1<<28;cam.allowHDR=false;
                for(int j=0;j<2;j++){var light=new GameObject(j==0?"Warm key":"Soft fill").AddComponent<Light>();light.transform.SetParent(place.transform,false);light.type=LightType.Point;light.range=10;light.intensity=j==0?2.7f:1.3f;light.color=j==0?new Color(1,.83f,.62f):new Color(.65f,.77f,1);light.transform.localPosition=new Vector3(j==0?-2:2,2.6f,2);light.cullingMask=1<<28;}
                ui.portraits[i].actor=actor;ui.portraits[i].portraitCamera=cam;
            }
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/LobbyCanvas.prefab");UnityEngine.Object.DestroyImmediate(root);
        }
        static void UpgradeTable(HeroLibrary library)
        {
            string path="Assets/Prefabs/TableWorld.prefab";var root=PrefabUtility.LoadPrefabContents(path);var board=root.GetComponent<TableBoard>();
            foreach(var layout in board.layouts)
                for(int seat=0;seat<layout.playerCount;seat++)
                {
                    var old=layout.avatars[seat];var parent=old.transform.parent;UnityEngine.Object.DestroyImmediate(old);
                    var actor=new GameObject("Hero avatar "+seat).AddComponent<HeroActor>();actor.transform.SetParent(parent,false);actor.library=library;actor.seated=true;actor.transform.localPosition=TableBoard.Away(seat,layout.playerCount)*6.8f+Vector3.up*library.seatedHeight;actor.transform.localScale=Vector3.one*library.heroScale;actor.transform.localRotation=Quaternion.LookRotation(-TableBoard.Away(seat,layout.playerCount));layout.avatars[seat]=actor.gameObject;
                    var chair=parent.Cast<Transform>().FirstOrDefault(t=>t.name.StartsWith("Chair")||t.name.StartsWith("Armchair"));if(chair!=null)UnityEngine.Object.DestroyImmediate(chair.gameObject);
                    var replacement=new GameObject("Armchair from stuff");replacement.transform.SetParent(parent,false);
                    PrefabUtility.InstantiatePrefab(library.chair,replacement.transform);replacement.transform.localPosition=TableBoard.Away(seat,layout.playerCount)*7;replacement.transform.localRotation=Quaternion.LookRotation(-TableBoard.Away(seat,layout.playerCount));
                }
            var oldTable=board.authoredEnvironment.Cast<Transform>().FirstOrDefault(t=>t.name.StartsWith("Round table")||t.name=="Imported table");if(oldTable!=null)UnityEngine.Object.DestroyImmediate(oldTable.gameObject);
            // Scale a wrapper, preserving the FBX import unit scale and axis conversion.
            var table=new GameObject("Imported table");table.transform.SetParent(board.authoredEnvironment,false);
            var mesh=(GameObject)PrefabUtility.InstantiatePrefab(library.table,table.transform);
            var tabletop=FurnitureMaterial("Tavern table",null,new Color(.34f,.19f,.09f));
            foreach(var r in mesh.GetComponentsInChildren<Renderer>())r.sharedMaterial=tabletop;
            var bounds=new Bounds();bool first=true;foreach(var r in table.GetComponentsInChildren<Renderer>()){if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}
            table.transform.localScale=new Vector3(12/bounds.size.x,TableBoard.TableTop/bounds.size.y,12/bounds.size.z);table.transform.localPosition=new Vector3(-bounds.center.x*table.transform.localScale.x,-bounds.min.y*table.transform.localScale.y,-bounds.center.z*table.transform.localScale.z);
            Debug.Log("TABLE_BOUNDS_BEFORE_SCALE "+bounds+" wrapper "+table.transform.localScale);
            PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
        }
        static Material FurnitureMaterial(string name,string texture,Color color)
        {
            string path=Destination+"Materials/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,path);}
            material.mainTexture=texture==null?null:Load<Texture2D>(texture);material.color=color;material.SetFloat("_Glossiness",.18f);EditorUtility.SetDirty(material);return material;
        }
        static void BuildAnnouncements(PresentationSettings settings)
        {
            var root=new GameObject("Motion announcements",typeof(Canvas),typeof(CanvasScaler));root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;root.GetComponent<Canvas>().sortingOrder=40;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,1000);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(settings.motionTitle,root.transform);var rect=instance.GetComponent<RectTransform>();rect.anchoredPosition=new Vector2(0,80);rect.sizeDelta=new Vector2(900,100);rect.localScale=Vector3.one*.8f;
            var style=instance.GetComponent<StyleManager>();style.UseUnscaledTime=true;style.showFor=2.6f;style.AnimationSpeed=1.6f;style.loopAnimations=false;style.playOnEnable=false;
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Destination+"CyrillicTitleFont.asset");
            if(font==null){font=TMP_FontAsset.CreateFontAsset(Load<Font>(Root+"VFX/Motion Titles Pack/Fonts/Roboto-Bold.ttf"));font.atlasPopulationMode=AtlasPopulationMode.Dynamic;AssetDatabase.CreateAsset(font,Destination+"CyrillicTitleFont.asset");foreach(var texture in font.atlasTextures)AssetDatabase.AddObjectToAsset(texture,font);AssetDatabase.AddObjectToAsset(font.material,font);}
            foreach(var item in instance.GetComponentsInChildren<TextItem>(true)){item.selectedFont=font;item.text="ЗА ОДНИМ СТОЛОМ";item.UpdateAll();}
            foreach(var graphic in instance.GetComponentsInChildren<Graphic>(true))graphic.raycastTarget=false;
            root.AddComponent<MotionAnnouncements>().style=style;instance.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/MotionAnnouncements.prefab");UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
