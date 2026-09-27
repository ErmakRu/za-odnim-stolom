using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CardFoilBorder:MaskableGraphic
    {
        [Min(1)]public float thickness=4;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            var outer=new[]{new Vector2(r.xMin,r.yMin),new Vector2(r.xMin,r.yMax),new Vector2(r.xMax,r.yMax),new Vector2(r.xMax,r.yMin)};
            var inner=new[]{outer[0]+new Vector2(thickness,thickness),outer[1]+new Vector2(thickness,-thickness),outer[2]+new Vector2(-thickness,-thickness),outer[3]+new Vector2(-thickness,thickness)};
            for(int i=0;i<4;i++){foreach(var p in new[]{outer[i],inner[i]})vh.AddVert(p,color,new Vector2((p.x-r.xMin)/r.width,(p.y-r.yMin)/r.height));}
            for(int i=0;i<4;i++){int a=i*2,b=((i+1)%4)*2;vh.AddTriangle(a,b,b+1);vh.AddTriangle(a,b+1,a+1);}
        }
    }
}
