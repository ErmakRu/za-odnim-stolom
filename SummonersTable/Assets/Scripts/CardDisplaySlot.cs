using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed class CardDisplaySlot : MonoBehaviour
    {
        public CardLibrary library;public string presentation="full";public CardView view;string current="";
        public string CardId=>current;public Text statsLabel=>view==null?null:view.fullStats;
        public void Show(CardDef card,Catalog catalog,Font font)
        {
            gameObject.SetActive(card!=null);if(card==null)return;
            if(library==null)library=Resources.Load<CardLibrary>("CardLibrary");
            if(view==null||current!=card.id){if(view!=null)Destroy(view.gameObject);var prefab=library.Find(card.id);if(prefab==null)throw new System.InvalidOperationException("Missing card variant: "+card.id);view=Instantiate(prefab,transform,false);current=card.id;}
            view.Mode(presentation);Fit();
        }
        public void Fit()
        {
            if(view==null)return;var holder=(RectTransform)transform;var rect=(RectTransform)view.transform;
            var face=(RectTransform)(presentation=="compact"?view.compactFace:view.fullFace).transform;
            rect.anchoredPosition=Vector2.zero;rect.localRotation=Quaternion.identity;rect.localScale=new Vector3(holder.rect.width/face.rect.width,holder.rect.height/face.rect.height,1);
        }
    }
}