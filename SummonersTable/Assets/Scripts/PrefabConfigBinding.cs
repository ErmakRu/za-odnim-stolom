using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed class PrefabConfigBinding:MonoBehaviour
    {
        public string configId;
        void Start(){Apply();}
        public void Apply()
        {
            if(ConfigRuntime.Current==null)return;
            var entry=ConfigRuntime.Current.prefabs.FirstOrDefault(p=>(string)p["id"]==configId);if(entry==null)return;
            foreach(var node in (List<object>)entry["components"])
            {
                var row=ConfigJson.Object(node);string path=(string)row["path"],type=(string)row["type"];
                var t=string.IsNullOrEmpty(path)?transform:transform.Find(path);if(t==null)continue;
                var component=t.GetComponents<Component>().FirstOrDefault(c=>c!=null&&c.GetType().FullName==type);if(component==null)continue;
                var values=ConfigJson.Object(row["values"]);foreach(var pair in values)
                {
                    var f=Fields(component.GetType()).FirstOrDefault(x=>x.Name==pair.Key);if(f==null)continue;
                    f.SetValue(component,ConvertValue(f.FieldType,pair.Value));
                }
                if(component is Graphic graphic)graphic.SetAllDirty();
            }
        }
        public static IEnumerable<FieldInfo> Fields(Type type)
        {
            var allowed=new[]{"HandFan","PlayerStatusView","WorldHandFanSettings","TargetArrowGraphic","WorldArrowView","TurnBudgetView","MatchOptionsView","CardDepthVisual","TavernPanel","EffectsVolume"};
            if(!allowed.Contains(type.Name))return Array.Empty<FieldInfo>();
            return type.GetFields(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly).Where(f=>!f.IsNotSerialized&&!f.IsInitOnly&&Supported(f.FieldType));
        }
        public static bool Supported(Type t)=>t==typeof(float)||t==typeof(int)||t==typeof(bool)||t==typeof(Vector2)||t==typeof(Vector3)||t==typeof(Color);
        static object ConvertValue(Type type,object value)
        {
            if(type==typeof(float))return Convert.ToSingle(value);if(type==typeof(int))return Convert.ToInt32(value);if(type==typeof(bool))return value;
            var d=ConfigJson.Object(value);float N(string key)=>Convert.ToSingle(d[key]);
            if(type==typeof(Vector2))return new Vector2(N("x"),N("y"));if(type==typeof(Vector3))return new Vector3(N("x"),N("y"),N("z"));return new Color(N("r"),N("g"),N("b"),N("a"));
        }
    }
}
