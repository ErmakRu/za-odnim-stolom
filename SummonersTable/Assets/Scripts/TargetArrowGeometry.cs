using UnityEngine;
namespace SummonersTable
{
    public static class TargetArrowGeometry
    {
        public const int Samples=64;
        public static Vector3 Point(Vector3 start,Vector3 end,Vector3 bend,float t)
        {var control=(start+end)*.5f+bend;return (1-t)*(1-t)*start+2*(1-t)*t*control+t*t*end;}
        public static Vector2[] Build(Vector2 start,Vector2 end)
        {
            if(Vector2.Distance(start,end)<4)return System.Array.Empty<Vector2>();
            var p=new Vector2[Samples+1];for(int i=0;i<=Samples;i++)p[i]=Point(start,end,Vector3.up*Mathf.Min(80,Vector2.Distance(start,end)*.22f),i/(float)Samples);return p;
        }
    }
}
