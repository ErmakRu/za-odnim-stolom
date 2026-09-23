using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace SummonersTable.Editor
{
    public static class ManagerAssets
    {
        public static string Register(Object asset,string kind)
        {
            if(asset==null)return "";var registry=Resources.Load<ConfigAssets>("ConfigAssets");var found=registry.entries.FirstOrDefault(e=>e.asset==asset);
            if(found!=null){if(kind=="interior"&&found.kind!=kind){found.kind=kind;EditorUtility.SetDirty(registry);AssetDatabase.SaveAssets();}return found.id;}
            if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset,out string guid,out long localId))throw new Exception("Сначала добавьте ассет в проект Unity.");
            string id="asset:"+guid+(asset is AnimationClip?":"+localId:"");
            var entry=new AssetRef{id=id,asset=asset,path=AssetDatabase.GetAssetPath(asset),kind=kind};
            registry.entries=registry.entries.Append(entry).ToArray();EditorUtility.SetDirty(registry);AssetDatabase.SaveAssets();return id;
        }
        public static string Animation(string name)
        {
            var library=Resources.Load<HeroLibrary>("HeroLibrary");
            var clip=library.controller.animationClips.FirstOrDefault(c=>c.name==name||c.name.EndsWith("|"+name));
            if(clip==null)clip=library.controller.animationClips.FirstOrDefault(c=>c.name.Contains(name));
            if(clip==null)throw new Exception("Animation not found: "+name);return Register(clip,"animation");
        }
    }
    [CustomPropertyDrawer(typeof(AssetIdAttribute))]
    public sealed class ManagerAssetDrawer:PropertyDrawer
    {
        public override void OnGUI(Rect position,SerializedProperty property,GUIContent label)
        {
            var kind=((AssetIdAttribute)attribute).kind;
            Type type=kind=="audio"?typeof(AudioClip):kind=="animation"?typeof(AnimationClip):kind=="material"?typeof(Material):kind=="texture"?typeof(Texture2D):typeof(GameObject);
            var registry=Resources.Load<ConfigAssets>("ConfigAssets");var current=registry?.entries.FirstOrDefault(e=>e.id==property.stringValue)?.asset;
            EditorGUI.BeginProperty(position,label,property);EditorGUI.BeginChangeCheck();
            var selected=EditorGUI.ObjectField(position,label,current,type,false);
            if(EditorGUI.EndChangeCheck())property.stringValue=ManagerAssets.Register(selected,kind);
            EditorGUI.EndProperty();
        }
    }
    [CustomPropertyDrawer(typeof(AudioEventAttribute))]
    public sealed class AudioEventDrawer:PropertyDrawer
    {
        public override void OnGUI(Rect rect,SerializedProperty property,GUIContent label)
        {
            var manager=property.serializedObject.targetObject as AuthoringManager;
            var audio=manager?.audio;
            if(audio?.cues==null)try{audio=ConfigJson.Read<AudioConfig>(System.IO.File.ReadAllText(ConfigAuthoring.Folder+"/audio.json"));}catch{}
            var ids=new[]{""}.Concat(audio?.cues?.Select(c=>c.action)??Array.Empty<string>()).ToArray();
            int selected=Math.Max(0,Array.IndexOf(ids,property.stringValue));int next=EditorGUI.Popup(rect,label.text,selected,ids.Select(s=>s==""?"Без звука":s).ToArray());property.stringValue=ids[next];
        }
    }
    [CustomPropertyDrawer(typeof(WorldEventAttribute))]
    public sealed class WorldEventDrawer:PropertyDrawer
    {
        public override void OnGUI(Rect rect,SerializedProperty property,GUIContent label)
        {
            try{var data=ConfigJson.Read<EventsConfig>(System.IO.File.ReadAllText(ConfigAuthoring.Folder+"/events.json"));if(data.events.Length==0){EditorGUI.LabelField(rect,label.text,"Нет событий");return;}int selected=Math.Max(0,Array.FindIndex(data.events,e=>e.id==property.intValue));int next=EditorGUI.Popup(rect,label.text,selected,data.events.Select(e=>e.id+" · "+e.name).ToArray());property.intValue=data.events[next].id;}
            catch{EditorGUI.PropertyField(rect,property,label);}
        }
    }
}
