using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace SummonersTable.Editor
{
    public static class ConfigAuthoring
    {
        public const string Folder="Assets/StreamingAssets/Config",WorldPath="Assets/Prefabs/TableWorld.prefab",Root="Assets/Prefabs/Editable/";
        static GameObject Asset(string path)=>AssetDatabase.LoadAssetAtPath<GameObject>(path);
        static GameObject Save(GameObject item,string path){var saved=PrefabUtility.SaveAsPrefabAsset(item,path);Object.DestroyImmediate(item);return saved;}
        static GameObject MakePart(Transform source,string file)
        {
            var go=Object.Instantiate(source.gameObject);go.name=file;go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            return Save(go,Root+"World/"+file+".prefab");
        }
        public static void Migrate()
        {
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();MigrateSeats();MigrateLobby();BuildAssetRegistry();CreateDefaults();CreateControls();BindPrefabs();ExportPrefabSettings(false);CopyDefaults();SyncCards();AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            Debug.Log("CONFIG_MIGRATION_COMPLETE");
        }
        public static void RepairLobbyAndTest()
        {
            string path="Assets/Prefabs/LobbyCanvas.prefab",stagePath=Root+"World/LobbyPortraitStage.prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var ui=root.GetComponent<FrontEndCanvas>();var stages=root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Hero preview ")).OrderBy(t=>t.name).ToArray();
                if(!File.Exists(stagePath)){var copy=Object.Instantiate(stages[0].gameObject);copy.name="LobbyPortraitStage";copy.transform.localPosition=Vector3.zero;copy.AddComponent<PrefabConfigBinding>().configId="LobbyPortraitStage";Save(copy,stagePath);}
                for(int i=0;i<4;i++)
                {
                    var old=stages[i];var next=((GameObject)PrefabUtility.InstantiatePrefab(Asset(stagePath),old.parent)).transform;
                    next.name=old.name;next.localPosition=old.localPosition;next.localRotation=old.localRotation;next.localScale=old.localScale;
                    ui.portraits[i].actor=next.GetComponentInChildren<HeroActor>(true);ui.portraits[i].portraitCamera=next.GetComponentInChildren<Camera>(true);
                    Object.DestroyImmediate(old.gameObject);
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
            var config=ConfigJson.Object(ConfigJson.Parse(File.ReadAllText(Path.Combine(Folder,"prefabs.json"))));var profiles=(List<object>)config["prefabs"];
            if(!profiles.Any(p=>(string)ConfigJson.Object(p)["id"]=="LobbyPortraitStage")){profiles.Add(new Dictionary<string,object>{{"id","LobbyPortraitStage"},{"components",new List<object>()}});Write("prefabs.json",Pretty(ConfigJson.Write(config)));}
            CopyDefaults();AssetDatabase.SaveAssets();AssetDatabase.Refresh();EditorPlaySmoke.Run();
        }
        public static void PolishConfigAndTest()
        {
            string path=Root+"UI/SettingsPanel.prefab";var root=PrefabUtility.LoadPrefabContents(path);var screen=root.GetComponent<WidgetScreen>();
            foreach(string id in new[]{"resume","leave"}){var rect=(RectTransform)screen.Get<Button>(id).transform;rect.anchoredPosition=new Vector2(rect.anchoredPosition.x,-900);}
            PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);AssetDatabase.SaveAssets();EditorPlaySmoke.Run();
        }
        static void MigrateSeats()
        {
            var root=PrefabUtility.LoadPrefabContents(WorldPath);try
            {
                var board=root.GetComponent<TableBoard>();if(root.GetComponentsInChildren<PlayerSeatView>(true).Length==9)return;
                var layout=board.layouts.First(l=>l.playerCount==4);var oldRoot=layout.heroes[0].parent;
                var chair=oldRoot.Find("Armchair from stuff");var chairAsset=MakePart(chair,"Chair");
                var avatarAsset=MakePart(layout.avatars[0].transform,"PlayerAvatar");var targetAsset=MakePart(layout.heroes[0],"PlayerTarget");var slotAsset=MakePart(layout.slotAnchors[0],"CreatureSlot");
                var seat=new GameObject("PlayerSeat");seat.transform.rotation=Quaternion.LookRotation(-TableBoard.Away(0,4));var view=seat.AddComponent<PlayerSeatView>();
                Transform Place(GameObject asset,Transform from)
                {
                    var go=(GameObject)PrefabUtility.InstantiatePrefab(asset,seat.transform);go.name=from.name;go.transform.SetPositionAndRotation(from.position,from.rotation);go.transform.localScale=from.lossyScale;return go.transform;
                }
                view.chair=Place(chairAsset,chair);view.avatar=Place(avatarAsset,layout.avatars[0].transform).GetComponent<HeroActor>();view.heroTarget=Place(targetAsset,layout.heroes[0]);
                view.slots=Enumerable.Range(0,5).Select(i=>Place(slotAsset,layout.slotAnchors[i])).ToArray();view.Assign(0);
                seat.transform.rotation=Quaternion.identity;var seatAsset=Save(seat,Root+"World/PlayerSeat.prefab");
                foreach(var l in board.layouts)
                {
                    var previous=l.heroes.Select(h=>h.parent.gameObject).Distinct().ToArray();
                    for(int i=0;i<l.playerCount;i++)
                    {
                        var item=(GameObject)PrefabUtility.InstantiatePrefab(seatAsset,l.transform);item.name="Seat "+i;item.transform.localRotation=Quaternion.LookRotation(-TableBoard.Away(i,l.playerCount));
                        var v=item.GetComponent<PlayerSeatView>();v.Assign(i);l.heroes[i]=v.heroTarget;l.avatars[i]=v.avatar.gameObject;for(int n=0;n<5;n++)l.slotAnchors[i*5+n]=v.slots[n];
                    }
                    foreach(var old in previous)Object.DestroyImmediate(old);
                }
                board.chairPrefab=chairAsset;board.avatarPrefab=avatarAsset;PrefabUtility.SaveAsPrefabAsset(root,WorldPath);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static void MigrateLobby()
        {
            string path="Assets/Prefabs/LobbyCanvas.prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var ui=root.GetComponent<FrontEndCanvas>();var source=ui.portraits[0].transform.parent;
                if(PrefabUtility.GetCorrespondingObjectFromSource(source.gameObject)!=null&&AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(source.gameObject)).EndsWith("LobbyPlayerPanel.prefab"))return;
                var copy=Object.Instantiate(source.gameObject);copy.name="LobbyPlayerPanel";((RectTransform)copy.transform).anchoredPosition=Vector2.zero;var asset=Save(copy,Root+"UI/LobbyPlayerPanel.prefab");
                for(int seat=0;seat<4;seat++)
                {
                    var previous=ui.portraits[seat].transform.parent;var next=((GameObject)PrefabUtility.InstantiatePrefab(asset,previous.parent)).transform;
                    next.name="Player place "+(seat+1);next.SetSiblingIndex(previous.GetSiblingIndex());var oldRect=(RectTransform)previous;var nextRect=(RectTransform)next;nextRect.anchorMin=oldRect.anchorMin;nextRect.anchorMax=oldRect.anchorMax;nextRect.pivot=oldRect.pivot;nextRect.sizeDelta=oldRect.sizeDelta;nextRect.anchoredPosition=oldRect.anchoredPosition;nextRect.localScale=oldRect.localScale;
                    foreach(var graphic in previous.GetComponentsInChildren<Graphic>(true)){var graphicPath=AnimationUtility.CalculateTransformPath(graphic.transform,previous).Replace(":"+seat,":0");var replacement=next.Find(graphicPath)?.GetComponent<Graphic>();if(replacement!=null)replacement.color=graphic.color;}
                    // Remap every serialized reference in the lobby controller through the same hierarchy paths.
                    var serialized=new SerializedObject(ui);var property=serialized.GetIterator();while(property.Next(true))
                    {
                        if(property.propertyType!=SerializedPropertyType.ObjectReference)continue;var value=property.objectReferenceValue;var component=value as Component;var go=value as GameObject;var tr=component!=null?component.transform:go!=null?go.transform:null;
                        if(tr==null||tr!=previous&&!tr.IsChildOf(previous))continue;string child=AnimationUtility.CalculateTransformPath(tr,previous).Replace(":"+seat,":0");var replacement=string.IsNullOrEmpty(child)?next:next.Find(child);
                        property.objectReferenceValue=component!=null?replacement.GetComponent(component.GetType()):(Object)replacement.gameObject;
                    }
                    serialized.ApplyModifiedPropertiesWithoutUndo();serialized.Dispose();
                    Object.DestroyImmediate(previous.gameObject);
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        [MenuItem("Summoners Table/Config/Refresh asset IDs")]
        public static void BuildAssetRegistry()
        {
            var map=new Dictionary<string,AssetRef>();var retained=Resources.Load<ConfigAssets>("ConfigAssets");if(retained!=null)foreach(var entry in retained.entries)if(entry.asset!=null)map[entry.id]=entry;
            void Add(Object asset,string custom=null)
            {
                if(asset==null)return;string path=AssetDatabase.GetAssetPath(asset);if(string.IsNullOrEmpty(path))return;
                string id=custom??"asset:"+AssetDatabase.AssetPathToGUID(path);if(map.Values.Any(e=>e.asset==asset))return;
                map[id]=new AssetRef{id=id,path=path,kind=asset is AudioClip?"audio":asset is Texture2D?"texture":"prefab",asset=asset};
            }
            foreach(var guid in AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets"}))Add(AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid)));
            foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/Art"})){var path=AssetDatabase.GUIDToAssetPath(guid);Add(AssetDatabase.LoadAssetAtPath<Texture2D>(path),"art:"+Path.GetFileNameWithoutExtension(path));}
            var presentation=Resources.Load<PresentationSettings>("PresentationSettings");
            foreach(var field in typeof(PresentationSettings).GetFields())if(field.FieldType==typeof(GameObject))Add(field.GetValue(presentation) as Object);
            foreach(var v in Resources.Load<CardLibrary>("CardLibrary").cards.Where(c=>c.spellEffect!=null))
            {Add(v.spellEffect.travelPrefab);Add(v.spellEffect.impactPrefab);Add(v.spellEffect.persistentPrefab);}
            var library=Resources.Load<ConfigAssets>("ConfigAssets");if(library==null){library=ScriptableObject.CreateInstance<ConfigAssets>();AssetDatabase.CreateAsset(library,"Assets/Resources/ConfigAssets.asset");}
            library.entries=map.Values.OrderBy(e=>e.id).ToArray();EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
            var manifest=new Dictionary<string,object>{{"schemaVersion",1},{"assets",library.entries.Select(e=>(object)new Dictionary<string,object>{{"id",e.id},{"kind",e.kind},{"path",e.path}}).ToList()}};
            Write("assets.json",Pretty(ConfigJson.Write(manifest)));
        }
        static void CreateDefaults()
        {
            var c=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("Data/catalog").text);var assets=Resources.Load<ConfigAssets>("ConfigAssets");foreach(var card in c.cards)card.art="art:"+card.id;
            IfMissing("cards.json",new CardsConfig{cards=c.cards.ToArray(),typeColors=c.typeColors.ToArray(),roleColors=c.roleColors.ToArray()});IfMissing("decks.json",new DecksConfig{decks=c.decks.ToArray()});IfMissing("rules.json",new RulesConfig{rules=c.rules});
            var s=Resources.Load<PresentationSettings>("PresentationSettings");var cues=new List<AudioCue>();
            void Cue(string action,AudioClip clip,float volume=.15f){cues.Add(new AudioCue{action=action,sounds=new[]{new SoundVariant{clip=assets.Id(clip),volume=volume}}});}
            Cue("card.hover",s.cardHover);Cue("action.invalid",s.invalidAction);Cue("creature.attack",s.attack);Cue("damage.hit",s.hit);Cue("qte.correct",s.qteSuccess);Cue("qte.error",s.qteError);Cue("turn.start",s.turnNotice);Cue("ambience",s.music,.08f);
            var effects=new List<EffectConfig>();foreach(var card in Resources.Load<CardLibrary>("CardLibrary").cards.Where(ca=>ca.spellEffect!=null))
            {
                var fx=card.spellEffect;effects.Add(new EffectConfig{id=card.definition.id,motion=(int)fx.motion,travel=assets.Id(fx.travelPrefab),impact=assets.Id(fx.impactPrefab),persistent=assets.Id(fx.persistentPrefab),travelScale=fx.travelScale,impactScale=fx.impactScale,persistentScale=fx.persistentScale,travelSeconds=fx.travelSeconds,lifetime=fx.lifetime,arc=fx.arc,cardSize=fx.cardSize,cardCount=fx.cardCount});
                Cue(card.definition.id+".launch",fx.launchSound,fx.sound!=null?fx.sound.volume:.15f);Cue(card.definition.id+".impact",fx.impactSound,fx.sound!=null?fx.sound.volume:.15f);
                string path=AssetDatabase.GetAssetPath(fx);var prefab=PrefabUtility.LoadPrefabContents(path);prefab.GetComponent<SpellEffect>().configId=card.definition.id;PrefabUtility.SaveAsPrefabAsset(prefab,path);PrefabUtility.UnloadPrefabContents(prefab);
            }
            IfMissing("audio.json",new AudioConfig{cues=cues.ToArray()});
            IfMissing("vfx.json",new VfxConfig{effects=effects.ToArray(),attack=assets.Id(s.attackEffect),hit=assets.Id(s.impactEffect),death=assets.Id(s.deathEffect),qteFire=assets.Id(s.qteFire),qteSmoke=assets.Id(s.qteSmoke),qteAttempt=assets.Id(s.qteAttempt),motionTitle=assets.Id(s.motionTitle),attackScale=s.attackEffectScale,impactScale=s.impactEffectScale,qteScale=s.qteEffectScale});
            IfMissing("presentation.json",new PresentationConfig{camera=ConfigBundle.Clone(s.data)});
            var world=PrefabUtility.LoadPrefabContents(WorldPath);try{IfMissing("world.json",ReadWorld(world.GetComponent<TableBoard>()));}finally{PrefabUtility.UnloadPrefabContents(world);}
        }
        public static WorldConfig ReadWorld(TableBoard board)
        {
            var seat=board.layouts.First(l=>l.playerCount==4).GetComponentsInChildren<PlayerSeatView>(true)[0];var p=board.GetComponent<TableProportions>();
            return new WorldConfig{table=TransformConfig.Read(board.authoredEnvironment.Find("Imported table")),chair=TransformConfig.Read(seat.chair),avatar=TransformConfig.Read(seat.avatar.transform),heroTarget=TransformConfig.Read(seat.heroTarget),slots=seat.slots.Select(TransformConfig.Read).ToArray(),unitsPerMetre=p.unitsPerMetre,tableHeight=p.tableHeight,tableDiameter=p.tableDiameter,avatarHipHeight=seat.avatar.seatedHipHeight,layouts=board.layouts.Select(l=>new LayoutConfig{players=l.playerCount,seats=l.GetComponentsInChildren<PlayerSeatView>(true).Select(v=>new SeatConfig{root=TransformConfig.Read(v.transform)}).ToArray()}).ToArray()};
        }
        static IEnumerable<string> PrefabPaths()=>AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p=>p.Count(c=>c=='/')).ThenBy(p=>p);
        public static void BindPrefabs()
        {
            foreach(var path in PrefabPaths())
            {
                var root=PrefabUtility.LoadPrefabContents(path);
                try{var binding=root.GetComponent<PrefabConfigBinding>();if(binding==null){binding=root.AddComponent<PrefabConfigBinding>();binding.configId=Path.GetFileNameWithoutExtension(path);PrefabUtility.SaveAsPrefabAsset(root,path);}}
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
        }
        public static void ExportPrefabSettings(bool overwrite)
        {
            if(File.Exists(Path.Combine(Folder,"prefabs.json"))&&!overwrite)return;var profiles=new List<object>();var seen=new HashSet<string>();
            foreach(var path in PrefabPaths())
            {
                var root=Asset(path);var binding=root.GetComponent<PrefabConfigBinding>();if(binding==null||!seen.Add(binding.configId))continue;var nodes=new List<object>();
                foreach(var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if(component==null||component.GetComponentInParent<PrefabConfigBinding>(true)!=binding)continue;var fields=PrefabConfigBinding.Fields(component.GetType()).ToArray();if(fields.Length==0)continue;
                    var values=new Dictionary<string,object>();foreach(var field in fields){var value=field.GetValue(component);values[field.Name]=value is Vector2||value is Vector3||value is Color?ConfigJson.Parse(JsonUtility.ToJson(value)):value;}
                    nodes.Add(new Dictionary<string,object>{{"path",AnimationUtility.CalculateTransformPath(component.transform,root.transform)},{"type",component.GetType().FullName},{"values",values}});
                }
                profiles.Add(new Dictionary<string,object>{{"id",binding.configId},{"components",nodes}});
            }
            Write("prefabs.json",Pretty(ConfigJson.Write(new Dictionary<string,object>{{"schemaVersion",1},{"prefabs",profiles}})));
        }
        public static void SyncCards()
        {
            var config=ConfigBundle.Read(Folder);var catalog=config.Catalog();
            foreach(var card in catalog.cards)
            {
                string path=Root+"Cards/Instances/"+card.id+".prefab";var root=PrefabUtility.LoadPrefabContents(path);root.GetComponent<CardView>().Import(card,catalog);PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
            }
        }
        public static void CopyDefaults(){Directory.CreateDirectory("Assets/Resources/ConfigDefaults");foreach(string file in ConfigBundle.Files)File.Copy(Path.Combine(Folder,file),Path.Combine("Assets/Resources/ConfigDefaults",file),true);AssetDatabase.Refresh();}
        static void IfMissing(string file,object value){if(!File.Exists(Path.Combine(Folder,file)))Write(file,JsonUtility.ToJson(value,true));}
        public static void Write(string file,string text){Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,file),text+"\n",new System.Text.UTF8Encoding(false));}
        public static string Pretty(string json)
        {
            var b=new System.Text.StringBuilder();int depth=0;bool quote=false,escape=false;
            foreach(char c in json){if(quote){b.Append(c);if(escape)escape=false;else if(c=='\\')escape=true;else if(c=='"')quote=false;continue;}if(c=='"'){quote=true;b.Append(c);}else if(c=='{'||c=='['){b.Append(c).Append('\n').Append(' ',++depth*2);}else if(c=='}'||c==']'){b.Append('\n').Append(' ',--depth*2).Append(c);}else if(c==',')b.Append(c).Append('\n').Append(' ',depth*2);else if(c==':')b.Append(": ");else if(!char.IsWhiteSpace(c))b.Append(c);}
            return b.ToString();
        }
        static void CreateControls()
        {
            string path=Root+"UI/ConfigControls.prefab";
            if(!File.Exists(path))
            {
                var root=new GameObject("ConfigControls",typeof(RectTransform));var rect=(RectTransform)root.transform;rect.sizeDelta=new Vector2(760,120);var v=root.AddComponent<ConfigControlsView>();
                v.reload=SharedButton.Create(root.transform,"Reload","Перезагрузить Config",new Vector2(0,35),new Vector2(390,42));v.reload.GetComponent<SharedButton>().label.fontSize=18;
                Text Label(string name,Transform parent,Vector2 size,Vector2 position,int fontSize){var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.sizeDelta=size;r.anchoredPosition=position;var text=go.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=fontSize;text.alignment=TextAnchor.MiddleCenter;text.color=Color.white;text.raycastTarget=false;return text;}
                v.status=Label("Status",root.transform,new Vector2(750,66),new Vector2(0,-26),16);Save(root,path);
            }
            foreach(string parent in new[]{"Assets/Prefabs/MainMenuCanvas.prefab",Root+"UI/SettingsPanel.prefab"})
            {
                var root=PrefabUtility.LoadPrefabContents(parent);if(root.GetComponentInChildren<ConfigControlsView>(true)==null)
                {var view=(GameObject)PrefabUtility.InstantiatePrefab(Asset(path),root.transform);var r=(RectTransform)view.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=parent.Contains("MainMenu")?new Vector2(570,-270):new Vector2(0,-310);if(parent.Contains("MainMenu"))r.localScale=Vector3.one*.65f;PrefabUtility.SaveAsPrefabAsset(root,parent);}PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
