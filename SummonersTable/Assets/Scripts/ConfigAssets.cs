using System;using System.Linq;using UnityEngine;
namespace SummonersTable
{
    public sealed class ConfigAssets:ScriptableObject
    {
        public AssetRef[] entries;
        public T Get<T>(string id) where T:UnityEngine.Object {if(string.IsNullOrEmpty(id))return null;return entries.FirstOrDefault(e=>e.id==id)?.asset as T;}
        public string Id(UnityEngine.Object value){if(value==null)return "";return entries.FirstOrDefault(e=>e.asset==value)?.id??throw new InvalidOperationException("Asset not registered: "+value.name);}
    }
}
