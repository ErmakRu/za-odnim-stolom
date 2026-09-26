using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed class RegularPolygonImage:Image
    {
        [Range(3,64)]public int sides=8;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=rectTransform.rect;int count=Mathf.Clamp(sides,3,64);
            float radius=Mathf.Min(r.width,r.height)*.5f;mesh.AddVert(r.center,color,Vector2.one*.5f);
            for(int i=0;i<count;i++)
            {
                float angle=(i+.5f)*Mathf.PI*2/count;var p=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                mesh.AddVert(r.center+p*radius,color,Vector2.one*.5f+p*.5f);
            }
            for(int i=0;i<count;i++)mesh.AddTriangle(0,i+1,(i+1)%count+1);
        }
    }
}
