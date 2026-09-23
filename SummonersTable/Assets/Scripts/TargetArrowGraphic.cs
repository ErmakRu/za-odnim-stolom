using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed class TargetArrowGraphic : MaskableGraphic
    {
        public float width=7,headLength=22,headWidth=12;Vector2 from,to;ArrowFeedback feedback;
        public void SelectTarget(){feedback?.Select();}
        public void Set(Vector2 a,Vector2 b){var style=ConfigRuntime.Current?.ui.arrow;if(style!=null){width=style.width;headLength=style.headLength;headWidth=style.headWidth;color=UserSettings.Data.highlightColor;material=ConfigRuntime.Assets.Get<Material>(style.uiMaterial);if(feedback==null)feedback=gameObject.GetComponent<ArrowFeedback>()??gameObject.AddComponent<ArrowFeedback>();feedback.Move(a,b);}
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,a,null,out from);RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,b,null,out to);SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var p=TargetArrowGeometry.Build(from,to);if(p.Length==0)return;
            for(int i=0;i<p.Length-2;i+=2)Segment(vh,p[i],p[i+1]);var dir=(to-p[p.Length-2]).normalized;var side=new Vector2(-dir.y,dir.x);
            Segment(vh,to-dir*headLength+side*headWidth,to);Segment(vh,to-dir*headLength-side*headWidth,to);
        }
        void Segment(VertexHelper vh,Vector2 a,Vector2 b){var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*width*.5f;int k=vh.currentVertCount;foreach(var p in new[]{a-n,a+n,b+n,b-n})vh.AddVert(p,color,Vector2.zero);vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);}
    }
}
