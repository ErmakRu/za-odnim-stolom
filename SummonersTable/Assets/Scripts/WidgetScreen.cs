using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    [Serializable] public sealed class WidgetBinding {public string id;public GameObject widget;}
    public sealed class WidgetScreen : MonoBehaviour
    {
        public WidgetBinding[] bindings;
        readonly Dictionary<string,GameObject> index=new Dictionary<string,GameObject>();
        public GameObject Item(string id){if(index.Count==0)foreach(var b in bindings)index[b.id]=b.widget;if(!index.TryGetValue(id,out var o))throw new InvalidOperationException(name+": missing prefab binding "+id);return o;}
        public T Get<T>(string id) where T:Component{return Item(id).GetComponent<T>();}
        public void Text(string id,string value){(Get<Text>(id)??Item(id).GetComponentInChildren<Text>(true)).text=value??"";}
        public void Visible(string id,bool value){Item(id).SetActive(value);}
        public void Enabled(string id,bool value){Get<Selectable>(id).interactable=value;}
        public void Click(string id,Action callback){var b=Get<Button>(id);b.onClick.RemoveAllListeners();b.onClick.AddListener(()=>callback());}
        public void Show(bool value){gameObject.SetActive(value);}
    }
}
