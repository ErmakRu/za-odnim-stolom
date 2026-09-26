using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed class SharedButton:MonoBehaviour
    {
        public Text label;public TavernPanel skin;
        public static Button Create(Transform parent,string name,string text,Vector2 position,Vector2 size)
        {
            var prefab=Resources.Load<SharedButton>("UI/CommonButton");
            if(prefab==null)throw new System.InvalidOperationException("CommonButton prefab is missing");
            SharedButton v;
#if UNITY_EDITOR
            v=!Application.isPlaying?((GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab.gameObject,parent)).GetComponent<SharedButton>():Instantiate(prefab,parent,false);
#else
            v=Instantiate(prefab,parent,false);
#endif
            v.name=name;v.label.text=text;
            var r=(RectTransform)v.transform;r.anchoredPosition=position;r.sizeDelta=size;return v.GetComponent<Button>();
        }
        public static Button TopLeft(Transform parent,string name,string text,Rect bounds)
        {
            var button=Create(parent,name,text,new Vector2(bounds.x,-bounds.y),bounds.size);
            var r=(RectTransform)button.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);return button;
        }
    }
}
