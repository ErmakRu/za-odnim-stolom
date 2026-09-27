using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed class TutorialPanel:MonoBehaviour
    {
        public Text instruction,secondaryLabel;
        public Button proceed,retry;
        public RectTransform highlight,callout,secondaryCallout;
        public FocusShade shade;
        public void PointAt(Rect primary,Rect? secondary=null,string caption="")
        {
            if(highlight!=null)highlight.gameObject.SetActive(false);
            shade.Focus(secondary.HasValue?new[]{primary,secondary.Value}:new[]{primary});
            Place(callout,primary);
            secondaryCallout.gameObject.SetActive(secondary.HasValue&&!string.IsNullOrEmpty(caption));
            if(secondaryCallout.gameObject.activeSelf){secondaryLabel.text=caption;Place(secondaryCallout,secondary.Value);}
        }
        void Place(RectTransform box,Rect screenArea)
        {
            var root=(RectTransform)transform;var canvas=GetComponent<Canvas>();float scale=canvas.scaleFactor;
            Vector2 screen=new Vector2(screenArea.center.x,screenArea.yMax+18+box.rect.height*scale*.5f);
            if(screen.y+box.rect.height*scale*.5f>Screen.height-20)screen.y=screenArea.yMin-18-box.rect.height*scale*.5f;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen,null,out var p);
            var bounds=root.rect;var half=box.rect.size*.5f;
            p.x=Mathf.Clamp(p.x,bounds.xMin+half.x+18,bounds.xMax-half.x-18);p.y=Mathf.Clamp(p.y,bounds.yMin+half.y+18,bounds.yMax-half.y-18);
            box.anchoredPosition=p;
        }
    }
}
