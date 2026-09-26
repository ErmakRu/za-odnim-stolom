using UnityEngine;
using UnityEngine.UI;

namespace SummonersTable
{
    // Vector silhouettes stay crisp in the hand, enlarged preview and on the table.
    [AddComponentMenu("UI/Card stat icon")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CardStatIcon:MaskableGraphic
    {
        public enum Shape { Sword,Heart }
        public Shape shape;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var rect=GetPixelAdjustedRect();float size=Mathf.Min(rect.width,rect.height);
            Vector2 Point(Vector2 p)=>rect.center+p*size;
            void Quad(Vector2 a,Vector2 b,Vector2 c,Vector2 d)
            {
                int n=mesh.currentVertCount;foreach(var p in new[]{a,b,c,d})mesh.AddVert(Point(p),color,Vector2.zero);
                mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);
            }
            if(shape==Shape.Heart)
            {
                Vector2 Curve(float t)=>new Vector2(16*Mathf.Pow(Mathf.Sin(t),3),13*Mathf.Cos(t)-5*Mathf.Cos(2*t)-2*Mathf.Cos(3*t)-Mathf.Cos(4*t)+2)/36;
                mesh.AddVert(Point(Vector2.zero),color,Vector2.zero);
                for(int i=0;i<64;i++)mesh.AddVert(Point(Curve(i*Mathf.PI/32)),color,Vector2.zero);
                for(int i=0;i<64;i++)mesh.AddTriangle(0,i+1,(i+1)%64+1);
            }
            else
            {
                Vector2 Turn(float x,float y)=>new Vector2((x-y)*.7071f,(x+y)*.7071f);
                void Blade(float x0,float y0,float x1,float y1,float x2,float y2,float x3,float y3)=>Quad(Turn(x0,y0),Turn(x1,y1),Turn(x2,y2),Turn(x3,y3));
                Blade(-.095f,-.14f,.095f,-.14f,.095f,.31f,0,.47f);
                Blade(-.095f,-.14f,0,.47f,-.095f,.31f,-.095f,-.14f);
                Blade(-.25f,-.19f,.25f,-.19f,.25f,-.10f,-.25f,-.10f);
                Blade(-.05f,-.39f,.05f,-.39f,.05f,-.18f,-.05f,-.18f);
                Blade(-.09f,-.45f,.09f,-.45f,.09f,-.37f,-.09f,-.37f);
            }
        }
    }
}
