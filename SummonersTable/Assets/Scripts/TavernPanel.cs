using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed class TavernPanel : MaskableGraphic
    {
        public Color border=new Color(.64f,.40f,.19f);public float corner=15,inset=4;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;Polygon(vh,r,corner,border);
            r=new Rect(r.x+inset,r.y+inset,r.width-inset*2,r.height-inset*2);Polygon(vh,r,Mathf.Max(0,corner-inset),color);
            Polygon(vh,new Rect(r.x+14,r.yMax-5,r.width-28,2),0,new Color(1,.75f,.39f,.45f));
            Polygon(vh,new Rect(r.x+14,r.y+4,r.width-28,2),0,new Color(.08f,.025f,.012f,.8f));
        }
        static void Polygon(VertexHelper vh,Rect r,float cut,Color tint)
        {
            int first=vh.currentVertCount;var center=r.center;
            vh.AddVert(center,tint,Vector2.zero);
            var pts=new[]{new Vector2(r.x+cut,r.y),new Vector2(r.xMax-cut,r.y),new Vector2(r.xMax,r.y+cut),new Vector2(r.xMax,r.yMax-cut),new Vector2(r.xMax-cut,r.yMax),new Vector2(r.x+cut,r.yMax),new Vector2(r.x,r.yMax-cut),new Vector2(r.x,r.y+cut)};
            foreach(var p in pts)vh.AddVert(p,tint,Vector2.zero);
            for(int i=0;i<8;i++)vh.AddTriangle(first,first+i+1,first+(i+1)%8+1);
        }
    }
}
