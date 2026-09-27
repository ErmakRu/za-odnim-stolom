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
        [Min(0)]public float targetGap=24,screenPadding=18;
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
            float gap=targetGap*scale;var half=box.rect.size*scale*.5f;
            var candidates=new[]{new Vector2(screenArea.center.x,screenArea.yMax+gap+half.y),new Vector2(screenArea.center.x,screenArea.yMin-gap-half.y),new Vector2(screenArea.xMax+gap+half.x,screenArea.center.y),new Vector2(screenArea.xMin-gap-half.x,screenArea.center.y)};
            float best=float.PositiveInfinity;Vector2 chosen=candidates[0];
            foreach(var candidate in candidates)
            {
                var center=new Vector2(Mathf.Clamp(candidate.x,half.x+screenPadding,Screen.width-half.x-screenPadding),Mathf.Clamp(candidate.y,half.y+screenPadding,Screen.height-half.y-screenPadding));
                var rect=new Rect(center-half,half*2);
                float overlap=Mathf.Max(0,Mathf.Min(rect.xMax,screenArea.xMax)-Mathf.Max(rect.xMin,screenArea.xMin))*Mathf.Max(0,Mathf.Min(rect.yMax,screenArea.yMax)-Mathf.Max(rect.yMin,screenArea.yMin));
                float score=overlap*100+Vector2.Distance(candidate,center);if(score<best){best=score;chosen=center;}
            }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root,chosen,null,out var p);box.anchoredPosition=p;
        }
    }
}
