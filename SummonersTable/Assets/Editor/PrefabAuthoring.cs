using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace SummonersTable.Editor
{
    // One-time migration / creation of missing assets. Existing authored assets are preserved.
    public static partial class PrefabAuthoring
    {
        const string Root="Assets/Prefabs/Editable/";
        static Font font;static CardLibrary library;
        static readonly Color ink=new Color(.03f,.065f,.08f,.98f),panel=new Color(.055f,.105f,.13f,.98f),teal=new Color(.25f,.79f,.69f),gold=new Color(.97f,.74f,.36f);
        [MenuItem("Summoners Table/Create missing editable prefabs (0.5)")]
        public static void Ensure()
        {
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach(string folder in new[]{Root,Root+"Cards",Root+"Cards/Types",Root+"Cards/Instances",Root+"UI",Root+"World"})Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();CreateCards();
            if(!File.Exists(Root+"UI/GameInterface.prefab"))
            {
                CreateInterface();UpgradeCardCanvas();UpgradeWardrobe();UpgradeWorld();
                foreach(string scenePath in new[]{"Assets/Scenes/Match.unity","Assets/Scenes/PresentationLab.unity"})
                {
                    var scene=EditorSceneManager.OpenScene(scenePath);
                    if(Object.FindFirstObjectByType<PrefabInterface>(FindObjectsInactive.Include)==null)PrefabUtility.InstantiatePrefab(Load(Root+"UI/GameInterface.prefab"));
                    EditorSceneManager.SaveScene(scene);
                }
            }
            CreateSpellEffects();FinalizeInterface();CreateLab();
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        }
        static GameObject Load(string path){return AssetDatabase.LoadAssetAtPath<GameObject>(path);}
        static RectTransform R(string name,Transform parent,Rect bounds)
        {
            var r=CardTableCanvas.Rect(name,parent,Vector2.zero,bounds.size);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(bounds.x,-bounds.y);return r;
        }
        static RectTransform Center(string name,Transform parent,Vector2 size){return CardTableCanvas.Rect(name,parent,Vector2.zero,size);}
        static Image I(string name,Transform parent,Rect r,Color color,bool raycast=false){var image=R(name,parent,r).gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=raycast;return image;}
        static Text T(string name,Transform parent,Rect r,string value,int size=20,TextAnchor align=TextAnchor.MiddleLeft)
        {
            var t=R(name,parent,r).gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.color=Color.white;t.alignment=align;t.raycastTarget=false;t.supportRichText=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;return t;
        }
        static GameObject Save(GameObject obj,string path){var asset=PrefabUtility.SaveAsPrefabAsset(obj,path);Object.DestroyImmediate(obj);return asset;}
        static Material Mat(string name,Color color,Texture texture=null)
        {
            string path=Root+"World/"+name+".mat";var old=AssetDatabase.LoadAssetAtPath<Material>(path);if(old!=null)return old;
            var m=new Material(Resources.Load<Shader>("BoardCard"));m.color=color;m.mainTexture=texture;AssetDatabase.CreateAsset(m,path);return m;
        }
        static Renderer Plane(string name,Transform parent,Vector3 position,Vector3 size,Material material)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Quad);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=size;Object.DestroyImmediate(obj.GetComponent<Collider>());obj.GetComponent<Renderer>().sharedMaterial=material;return obj.GetComponent<Renderer>();
        }
        static void CreateCards()
        {
            var catalog=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("Data/catalog").text);
            string path=Root+"Cards/CardBase.prefab";
            if(!File.Exists(path))
            {
                var root=Center("CardBase",null,new Vector2(270,480));var v=root.gameObject.AddComponent<CardView>();
                v.playableGlow=I("Playable glow",root,new Rect(55,130,160,220),teal);v.selectionFrame=I("Selected border",root,new Rect(57,132,156,216),gold);
                v.fullFace=Center("Full face",root,new Vector2(270,480)).gameObject;var f=v.fullFace.transform;
                I("Background",f,new Rect(0,0,270,480),ink,true);v.fullType=I("Type band",f,new Rect(0,0,270,32),teal);v.fullTypeText=T("Type",f,new Rect(9,0,252,32),"КАРТА",16,TextAnchor.MiddleCenter);
                v.fullArtwork=R("Artwork",f,new Rect(8,40,254,155)).gameObject.AddComponent<RawImage>();v.fullArtwork.raycastTarget=false;
                v.fullRole=I("Creature group color",f,new Rect(8,199,254,25),teal);v.fullRoleText=T("Faction and group",f,new Rect(12,199,246,25),"",14,TextAnchor.MiddleCenter);
                v.fullName=T("Name",f,new Rect(12,232,246,54),"Название",22,TextAnchor.MiddleCenter);v.fullName.fontStyle=FontStyle.Bold;
                v.fullStats=T("Stats",f,new Rect(12,290,246,32),"",16,TextAnchor.MiddleCenter);
                v.fullRules=T("Rules",f,new Rect(14,329,242,139),"",18,TextAnchor.UpperLeft);v.fullRules.resizeTextForBestFit=true;v.fullRules.resizeTextMinSize=13;v.fullRules.resizeTextMaxSize=18;
                v.compactFace=Center("Compact face",root,new Vector2(150,210)).gameObject;f=v.compactFace.transform;
                I("Background",f,new Rect(0,0,150,210),ink,true);v.compactType=I("Type band",f,new Rect(0,0,150,25),teal);v.compactTypeText=T("Type",f,new Rect(3,0,144,25),"",12,TextAnchor.MiddleCenter);
                v.compactArtwork=R("Artwork",f,new Rect(4,29,142,102)).gameObject.AddComponent<RawImage>();v.compactArtwork.raycastTarget=false;
                v.compactRole=I("Group color",f,new Rect(4,131,142,5),teal);v.compactName=T("Name",f,new Rect(7,140,136,43),"Название",16);v.compactName.fontStyle=FontStyle.Bold;v.compactStats=T("Stats",f,new Rect(7,185,136,23),"",14);
                v.worldFace=new GameObject("Tabletop face");v.worldFace.transform.SetParent(root,false);
                v.worldArtwork=Plane("Artwork",v.worldFace.transform,Vector3.zero,Vector3.one,Mat("Card neutral",Color.white));
                v.worldType=Plane("Type edge",v.worldFace.transform,new Vector3(0,.47f,-.01f),new Vector3(1,.06f,1),Mat("Creature type",teal));
                v.worldRole=Plane("Creature group edge",v.worldFace.transform,new Vector3(0,-.47f,-.01f),new Vector3(1,.06f,1),Mat("Role neutral",Color.white));
                var outline=new GameObject("Readiness outline");outline.transform.SetParent(v.worldFace.transform,false);v.worldOutline=outline.AddComponent<LineRenderer>();v.worldOutline.sharedMaterial=Mat("Outline",Color.white);v.worldOutline.useWorldSpace=false;v.worldOutline.loop=true;v.worldOutline.positionCount=4;v.worldOutline.SetPositions(new[]{new Vector3(-.52f,-.52f,-.02f),new Vector3(.52f,-.52f,-.02f),new Vector3(.52f,.52f,-.02f),new Vector3(-.52f,.52f,-.02f)});v.worldOutline.startWidth=v.worldOutline.endWidth=.025f;
                v.Mode("full");v.Highlight(false,false);Save(root.gameObject,path);
            }
            foreach(string type in new[]{"creature","spell","reaction"})
            {
                string name=type=="creature"?"Creature":type=="spell"?"Spell":"Reaction";string file=Root+"Cards/Types/"+name+"Card.prefab";
                if(!File.Exists(file)){var instance=(GameObject)PrefabUtility.InstantiatePrefab(Load(path));instance.name=name+"Card";var v=instance.GetComponent<CardView>();v.definition.kind=type;var c=catalog.typeColors.Find(x=>x.id==type);ColorUtility.TryParseHtmlString(c.hex,out var tint);v.fullType.color=v.compactType.color=tint;Save(instance,file);}
            }
            foreach(var card in catalog.cards)
            {
                string file=Root+"Cards/Instances/"+card.id+".prefab";if(File.Exists(file))continue;
                string type=card.kind=="creature"?"Creature":card.kind=="spell"?"Spell":"Reaction";
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(Load(Root+"Cards/Types/"+type+"Card.prefab"));instance.name=card.id+" — "+card.name;var v=instance.GetComponent<CardView>();v.Import(card,catalog);
                v.worldArtwork.sharedMaterial=Mat(card.id+" art",Color.white,Resources.Load<Texture2D>("Art/"+card.id));
                v.worldType.sharedMaterial=Mat(type+" edge",v.fullType.color);if(card.kind=="creature")v.worldRole.sharedMaterial=Mat(card.role+" edge",v.fullRole.color);
                Save(instance,file);
            }
            if(!File.Exists(Root+"World/CardBack.prefab")){var obj=new GameObject("Card back");Plane("Back",obj.transform,Vector3.zero,Vector3.one,Mat("Card back",Color.white,Resources.Load<Texture2D>("Art/card_back")));Save(obj,Root+"World/CardBack.prefab");}
            library=Resources.Load<CardLibrary>("CardLibrary");if(library==null){library=ScriptableObject.CreateInstance<CardLibrary>();AssetDatabase.CreateAsset(library,"Assets/Resources/CardLibrary.asset");}
            library.cards=catalog.cards.Select(c=>Load(Root+"Cards/Instances/"+c.id+".prefab").GetComponent<CardView>()).ToArray();library.cardBackPrefab=Load(Root+"World/CardBack.prefab");EditorUtility.SetDirty(library);
            if(!File.Exists(Root+"UI/CardSlot.prefab")){var rect=Center("Card presenter",null,new Vector2(270,480));rect.gameObject.AddComponent<CardDisplaySlot>().library=library;Save(rect.gameObject,Root+"UI/CardSlot.prefab");}
        }
        sealed class Screen
        {
            public WidgetScreen view;readonly List<WidgetBinding> bindings=new List<WidgetBinding>();public Transform Parent=>view.transform;
            public Screen(string name,bool background=false,int order=0)
            {
                view=Center(name,null,new Vector2(1600,1000)).gameObject.AddComponent<WidgetScreen>();
                if(order>0){var c=view.gameObject.AddComponent<Canvas>();c.overrideSorting=true;c.sortingOrder=order;view.gameObject.AddComponent<GraphicRaycaster>();}
                if(background)I("Background",Parent,new Rect(0,0,1600,1000),ink,true);
            }
            public T Bind<T>(string id,T c)where T:Component{bindings.Add(new WidgetBinding{id=id,widget=c.gameObject});return c;}
            public Text Text(string id,Rect r,string text,int size=20,TextAnchor align=TextAnchor.MiddleLeft){return Bind(id,T(id,Parent,r,text,size,align));}
            public Button Button(string id,Rect r,string label,Color? color=null)
            {
                var image=I(id,Parent,r,color??teal,true);var button=image.gameObject.AddComponent<Button>();T("Label",button.transform,new Rect(4,0,r.width-8,r.height),label,20,TextAnchor.MiddleCenter);return Bind(id,button);
            }
            public RectTransform Area(string id,Rect r){return Bind(id,R(id,Parent,r));}
            public Slider Slider(string id,Rect r)
            {
                var root=R(id,Parent,r);var slider=root.gameObject.AddComponent<Slider>();I("Track",root,new Rect(0,r.height*.35f,r.width,r.height*.3f),new Color(.18f,.25f,.28f));
                var fill=I("Fill",root,new Rect(0,r.height*.35f,r.width,r.height*.3f),teal);slider.fillRect=fill.rectTransform;
                var handle=I("Handle",root,new Rect(0,0,20,r.height),gold,true);slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.direction=UnityEngine.UI.Slider.Direction.LeftToRight;FixSlider(slider);return Bind(id,slider);
            }
            public InputField Input(string id,Rect r,string initial,int limit)
            {
                var image=I(id,Parent,r,new Color(.13f,.21f,.24f),true);var field=image.gameObject.AddComponent<InputField>();field.textComponent=T("Input",image.transform,new Rect(10,0,r.width-20,r.height),"",22);field.characterLimit=limit;field.text=initial;return Bind(id,field);
            }
            public WidgetScreen Save(string file){view.bindings=bindings.ToArray();return PrefabAuthoring.Save(view.gameObject,Root+"UI/"+file+".prefab").GetComponent<WidgetScreen>();}
        }
        static T Nest<T>(T prefab,Transform parent)where T:Component {return ((GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject,parent)).GetComponent<T>();}
        static CardDisplaySlot Slot(Transform parent,string name,Rect bounds,string presentation="full")
        {
            var slot=Nest(Load(Root+"UI/CardSlot.prefab").GetComponent<CardDisplaySlot>(),parent);slot.name=name;var r=(RectTransform)slot.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(bounds.x,-bounds.y);r.sizeDelta=bounds.size;slot.presentation=presentation;return slot;
        }
        static ScrollRect Scroll(Transform parent,string name,Rect bounds,Vector2 size)
        {
            var root=R(name,parent,bounds);var sc=root.gameObject.AddComponent<ScrollRect>();var vp=R("Viewport",root,new Rect(0,0,bounds.width,bounds.height));vp.gameObject.AddComponent<RectMask2D>();var bg=vp.gameObject.AddComponent<Image>();bg.color=new Color(0,0,0,.01f);
            var content=R("Content",vp,new Rect(0,0,size.x,size.y));sc.viewport=vp;sc.content=content;sc.horizontal=false;sc.movementType=ScrollRect.MovementType.Clamped;sc.scrollSensitivity=35;return sc;
        }
    }
}
