using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed class MenuCardIllustration:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerMoveHandler
    {
        public RawImage artwork;
        Material owned;Vector2 look,target;bool hover;
        void OnEnable(){if(artwork!=null&&artwork.material!=null){owned=new Material(artwork.material);artwork.material=owned;}}
        void OnDisable(){if(owned!=null){var original=Resources.Load<Material>("Styles/CardWindowUI");artwork.material=original;Destroy(owned);owned=null;}hover=false;target=look=Vector2.zero;}
        public void OnPointerEnter(PointerEventData e){hover=true;OnPointerMove(e);}
        public void OnPointerExit(PointerEventData e){hover=false;target=Vector2.zero;}
        public void OnPointerMove(PointerEventData e){if(!hover)return;RectTransformUtility.ScreenPointToLocalPointInRectangle(artwork.rectTransform,e.position,e.pressEventCamera,out var p);var size=artwork.rectTransform.rect.size;target=new Vector2(p.x/size.x,p.y/size.y)*.7f;}
        void Update(){look=Vector2.Lerp(look,target,1-Mathf.Exp(-Time.unscaledDeltaTime*12));if(owned!=null)owned.SetVector("_ViewOffset",new Vector4(look.x,look.y,0,0));}
    }
}
