using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    // A stencil-compatible UI image with the same clipped corners as tavern buttons.
    public sealed class BeveledImage:Image
    {
        public float corner=22;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;float c=Mathf.Min(corner,Mathf.Min(r.width,r.height)*.45f);
            var points=new[]{new Vector2(r.x+c,r.y),new Vector2(r.xMax-c,r.y),new Vector2(r.xMax,r.y+c),new Vector2(r.xMax,r.yMax-c),new Vector2(r.xMax-c,r.yMax),new Vector2(r.x+c,r.yMax),new Vector2(r.x,r.yMax-c),new Vector2(r.x,r.y+c)};
            vh.AddVert(r.center,color,new Vector2(.5f,.5f));
            foreach(var p in points)vh.AddVert(p,color,new Vector2((p.x-r.x)/r.width,(p.y-r.y)/r.height));
            for(int i=0;i<8;i++)vh.AddTriangle(0,i+1,(i+1)%8+1);
        }
    }
}
