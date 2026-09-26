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
    public static class SharedPresentationMigration
    {
        public const string ButtonPath="Assets/Resources/UI/CommonButton.prefab";
        public const string FacePath="Assets/LayeredCards/Prefabs/LayeredCardBase.prefab";
        const string CardPath="Assets/Prefabs/Editable/Cards/CardBase.prefab";
        public static void ButtonsOne()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--prefab-path");if(index<0||index+1>=args.Length)throw new ArgumentException("--prefab-path required");
            string path=args[index+1];if(!(path.StartsWith("Assets/Prefabs/")||path==CampaignAuthoring.Prefab)||!path.EndsWith(".prefab")||path.Contains(".."))throw new ArgumentException("Unexpected prefab path");
            VerifiedBackup(path);Buttons(path);AssetDatabase.SaveAssets();
            var saved=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(saved.GetComponentsInChildren<MonoBehaviour>(true).Any(c=>c==null))throw new Exception("Missing component after migration");
            Debug.Log("BUTTON_PREFAB_VERIFIED "+path);
        }
        static void VerifiedBackup(string path)
        {
            string backup=Path.GetFullPath("../tmp/presentation-backup-20260926/"+"SummonersTable/"+path);
            using(var hash=System.Security.Cryptography.SHA256.Create())
                if(!File.Exists(backup)||!hash.ComputeHash(File.ReadAllBytes(path)).SequenceEqual(hash.ComputeHash(File.ReadAllBytes(backup))))throw new Exception("Verified backup required: "+path);
        }
        public static void ButtonsRemaining()
        {
            // Deliberately restricted to authored UI assets; world, cards and managers
            // are not traversed or rewritten by this step.
            var paths=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/Editable/UI"}).Select(AssetDatabase.GUIDToAssetPath).Concat(new[]{"Assets/Prefabs/MainMenuCanvas.prefab","Assets/Prefabs/LobbyCanvas.prefab",CampaignAuthoring.Prefab}).ToHashSet();
            var done=new HashSet<string>();
            void Visit(string path){if(!done.Add(path))return;foreach(var d in AssetDatabase.GetDependencies(path,false))if(d!=path&&paths.Contains(d))Visit(d);VerifiedBackup(path);Buttons(path);}
            foreach(var path in paths)Visit(path);
            AssetDatabase.SaveAssets();PresentationRevisionTests.Run();Debug.Log("SHARED_BUTTONS_VERIFIED");
        }
        public static void RepairLobbyBindings()
        {
            const string path="Assets/Prefabs/LobbyCanvas.prefab";var g=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var front=g.GetComponent<FrontEndCanvas>();string[] names={"deck-prev","deck-next","hero-prev","hero-next","customize"};
                for(int i=0;i<front.portraits.Length;i++)
                {
                    var column=front.portraits[i].transform.parent;
                    for(int n=0;n<names.Length;n++)
                    {
                        var button=column.GetComponentsInChildren<Button>(true).Single(b=>b.name.StartsWith(names[n]+":",StringComparison.Ordinal));
                        button.name=names[n]+":"+i;PrefabUtility.RecordPrefabInstancePropertyModifications(button.gameObject);front.seatControls[i*5+n]=button;
                    }
                }
                var buttons=g.GetComponentsInChildren<Button>(true);
                for(int i=0;i<front.actions.Length;i++)front.buttons[i]=buttons.Single(b=>b.name==front.actions[i]);
                PrefabUtility.SaveAsPrefabAsset(g,path);
            }finally{PrefabUtility.UnloadPrefabContents(g);}
            PresentationRevisionTests.Run();Debug.Log("LOBBY_BINDINGS_VERIFIED");
        }
        public static void RepairSettingsLabels()
        {
            const string path="Assets/Prefabs/Editable/UI/SettingsPanel.prefab";
            string backup="../tmp/presentation-backup-20260926/SettingsPanel-before-label-repair.prefab";
            if(!File.Exists(backup))File.Copy(path,backup);
            var g=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var screen=g.GetComponent<WidgetScreen>();
                foreach(string id in new[]{"applyDisplay","resume","leave"})screen.bindings.Single(b=>b.id==id+"Text").widget=screen.Get<Button>(id).GetComponent<SharedButton>().label.gameObject;
                PrefabUtility.SaveAsPrefabAsset(g,path);
            }finally{PrefabUtility.UnloadPrefabContents(g);}
            PresentationRevisionTests.RenderCards();
        }
        public static void Run()
        {
            CreateButton();UpdateFace();UpdateCardBase();CampaignPresentationMigration.Apply();
            var paths=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath).Append(CampaignAuthoring.Prefab).ToHashSet();
            var done=new HashSet<string>();void Visit(string p){if(!done.Add(p))return;foreach(var d in AssetDatabase.GetDependencies(p,false))if(paths.Contains(d)&&d!=p)Visit(d);Buttons(p);}
            foreach(var p in paths)Visit(p);
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();PresentationRevisionTests.Run();Debug.Log("SHARED_PRESENTATION_READY");
        }
        public static RectTransform Place(Transform t,float x,float y,float w,float h)
        {var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);return r;}
        public static T Add<T>(string name,Transform parent,float x,float y,float w,float h)where T:Component
        {var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);Place(g.transform,x,y,w,h);return g.AddComponent<T>();}
        public static Text Label(string name,Transform parent,float x,float y,float w,float h,int size)
        {var t=Add<Text>(name,parent,x,y,w,h);t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=size;t.color=new Color(.96f,.90f,.78f);t.raycastTarget=false;t.alignment=TextAnchor.MiddleCenter;t.supportRichText=false;return t;}
        static void CreateButton()
        {
            if(File.Exists(ButtonPath))return;
            Directory.CreateDirectory("Assets/Resources/UI");AssetDatabase.Refresh();
            var g=new GameObject("CommonButton",typeof(RectTransform));Place(g.transform,0,0,260,54);
            var skin=g.AddComponent<TavernPanel>();skin.color=new Color(.27f,.13f,.05f);skin.corner=10;skin.inset=3;
            var b=g.AddComponent<Button>();b.targetGraphic=skin;var colors=b.colors;colors.highlightedColor=new Color(1.18f,1.12f,.95f);colors.pressedColor=new Color(.75f,.65f,.5f);colors.disabledColor=new Color(.50f,.45f,.4f,.65f);b.colors=colors;
            var text=Label("Label",g.transform,0,0,232,42,23);text.text="КНОПКА";text.fontStyle=FontStyle.Bold;text.resizeTextForBestFit=true;text.resizeTextMaxSize=23;text.resizeTextMinSize=12;
            var r=text.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.sizeDelta=new Vector2(-26,-10);
            var shadow=text.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.05f,.015f,0,.9f);shadow.effectDistance=new Vector2(1,-2);
            var shared=g.AddComponent<SharedButton>();shared.label=text;shared.skin=skin;
            PrefabUtility.SaveAsPrefabAsset(g,ButtonPath);Object.DestroyImmediate(g);
        }
        static void UpdateFace()
        {
            var go=PrefabUtility.LoadPrefabContents(FacePath);
            try
            {
                var v=go.GetComponent<LayeredCardView>();
                if(go.GetComponent<BeveledImage>()==null){var shape=go.AddComponent<BeveledImage>();shape.corner=24;shape.color=Color.white;go.AddComponent<Mask>().showMaskGraphic=false;}
                foreach(string n in new[]{"Outer frame","Inner body"})
                {var t=go.transform.Find(n);var image=t.GetComponent<Image>();var color=image.color;if(!(image is BeveledImage)){Object.DestroyImmediate(image);image=t.gameObject.AddComponent<BeveledImage>();}image.color=color;image.raycastTarget=false;}
                // Top = name. Middle = clearly visible card type, same width and less height.
                Place(v.titleFill.transform,0,432,580,56);Place(v.title.transform,0,432,548,48);v.title.fontSize=34;v.title.resizeTextMaxSize=34;
                Place(v.accent.transform,0,5,580,46);Place(v.type.transform,0,5,548,40);v.type.fontSize=28;v.type.fontStyle=FontStyle.Bold;
                v.statsBackground=go.transform.Find("Stats backdrop").GetComponent<Image>();Place(v.statsBackground.transform,0,74,250,60);Place(v.stats.transform,0,74,230,52);v.stats.fontSize=36;v.stats.fontStyle=FontStyle.Bold;
                Place(v.role.transform,0,-49,532,36);v.role.fontSize=24;
                var r=Place(v.description.transform,0,-106,534,280);r.pivot=new Vector2(.5f,1);v.description.resizeTextForBestFit=false;v.description.fontSize=29;v.description.lineSpacing=1.06f;
                if(v.restrictions==null)v.restrictions=Label("Limitations",go.transform,0,-290,534,110,25);
                v.restrictions.rectTransform.pivot=new Vector2(.5f,1);v.restrictions.alignment=TextAnchor.UpperLeft;v.restrictions.fontStyle=FontStyle.Italic;v.restrictions.color=new Color(.97f,.76f,.42f);v.restrictions.lineSpacing=1.04f;
                PrefabUtility.SaveAsPrefabAsset(go,FacePath);
            }finally{PrefabUtility.UnloadPrefabContents(go);}
        }
        static LayeredCardView Face(Transform parent,string name,float scale)
        {
            var existing=parent.Find(name);if(existing!=null)return existing.GetComponent<LayeredCardView>();
            var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(FacePath),parent);g.name=name;
            var r=(RectTransform)g.transform;r.anchoredPosition=Vector2.zero;r.localScale=Vector3.one*scale;
            var v=g.GetComponent<LayeredCardView>();v.runtimeMode=true;v.cardName="";return v;
        }
        static void UpdateCardBase()
        {
            var g=PrefabUtility.LoadPrefabContents(CardPath);
            try
            {
                var v=g.GetComponent<CardView>();
                foreach(var holder in new[]{v.fullFace,v.compactFace})foreach(Transform child in holder.transform)if(child.name!="Shared card face")child.gameObject.SetActive(false);
                Place(v.fullFace.transform,0,0,270,423);Place(v.compactFace.transform,0,0,180,282);
                v.sharedFull=Face(v.fullFace.transform,"Shared card face",.45f);v.sharedCompact=Face(v.compactFace.transform,"Shared card face",.30f);
                v.fullArtwork=v.sharedFull.artwork;v.compactArtwork=v.sharedCompact.artwork;v.fullName=v.sharedFull.title;v.compactName=v.sharedCompact.title;
                v.fullRules=v.sharedFull.description;v.fullStats=v.sharedFull.stats;v.compactStats=v.sharedCompact.stats;
                v.fullType=v.sharedFull.accent;v.compactType=v.sharedCompact.accent;v.fullTypeText=v.sharedFull.type;v.compactTypeText=v.sharedCompact.type;
                v.fullRole=v.sharedFull.roleAccent;v.compactRole=v.sharedCompact.roleAccent;v.fullRoleText=v.sharedFull.role;v.compactRoleText=v.sharedCompact.role;
                v.fullStatsBadge=v.sharedFull.statsBackground.gameObject;v.compactStatsBadge=v.sharedCompact.statsBackground.gameObject;
                foreach(var renderer in v.worldFace.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
                var canvas=v.worldFace.GetComponentInChildren<Canvas>(true);
                if(canvas==null){canvas=Add<Canvas>("World card canvas",v.worldFace.transform,0,0,600,940);canvas.renderMode=RenderMode.WorldSpace;canvas.transform.localPosition=new Vector3(0,0,-.012f);canvas.transform.localScale=new Vector3(1f/600,1f/940,1);}
                v.sharedWorld=Face(canvas.transform,"Shared card face",1);
                foreach(var glow in new[]{v.playableGlow,v.selectionFrame})
                {
                    if(glow is BeveledImage)continue;var obj=glow.gameObject;var tint=glow.color;bool playable=glow==v.playableGlow;Object.DestroyImmediate(glow);var shape=obj.AddComponent<BeveledImage>();shape.color=tint;shape.corner=11;shape.raycastTarget=false;if(playable)v.playableGlow=shape;else v.selectionFrame=shape;
                }
                PrefabUtility.SaveAsPrefabAsset(g,CardPath);
            }finally{PrefabUtility.UnloadPrefabContents(g);}
        }
        public static void Buttons(string path)
        {
            Debug.Log("MIGRATE_BUTTONS "+path);
            var g=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var map=new Dictionary<Object,Object>();var removed=new List<GameObject>();
                foreach(var old in g.GetComponentsInChildren<Button>(true))
                {
                    if(old.GetComponent<SharedButton>()!=null)continue;
                    string owner=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(old);
                    if(!string.IsNullOrEmpty(owner)&&owner!=path)continue;
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath);var replacement=(GameObject)PrefabUtility.InstantiatePrefab(prefab,old.transform.parent);replacement.name=old.name;
                    var button=replacement.GetComponent<Button>();var normalColors=button.colors;var shared=replacement.GetComponent<SharedButton>();
                    EditorUtility.CopySerialized(old,button);button.targetGraphic=shared.skin;button.colors=normalColors;
                    var from=(RectTransform)old.transform;var to=(RectTransform)replacement.transform;
                    to.anchorMin=from.anchorMin;to.anchorMax=from.anchorMax;to.pivot=from.pivot;to.sizeDelta=from.sizeDelta;to.anchoredPosition3D=from.anchoredPosition3D;to.localRotation=from.localRotation;to.localScale=from.localScale;to.SetSiblingIndex(from.GetSiblingIndex());replacement.SetActive(old.gameObject.activeSelf);
                    var label=old.GetComponentInChildren<Text>(true);if(label!=null){shared.label.text=label.text;map[label]=shared.label;map[label.gameObject]=shared.label.gameObject;map[label.transform]=shared.label.transform;}
                    map[old]=button;map[old.gameObject]=replacement;map[old.transform]=replacement.transform;if(old.targetGraphic!=null)map[old.targetGraphic]=shared.skin;removed.Add(old.gameObject);
                }
                if(removed.Count==0)return;
                foreach(var component in g.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if(component==null||removed.Any(o=>component.transform==o.transform||component.transform.IsChildOf(o.transform)))continue;
                    using(var serialized=new SerializedObject(component))
                    {var p=serialized.GetIterator();while(p.Next(true))if(p.propertyPath!="m_GameObject"&&p.propertyPath!="m_CorrespondingSourceObject"&&p.propertyType==SerializedPropertyType.ObjectReference&&p.objectReferenceValue!=null&&map.TryGetValue(p.objectReferenceValue,out var mapped))p.objectReferenceValue=mapped;serialized.ApplyModifiedPropertiesWithoutUndo();}
                }
                foreach(var old in removed)Object.DestroyImmediate(old);
                PrefabUtility.SaveAsPrefabAsset(g,path);
            }finally{PrefabUtility.UnloadPrefabContents(g);}
        }
    }
}
