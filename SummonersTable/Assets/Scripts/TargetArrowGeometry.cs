using System.Collections.Generic;
using UnityEngine;

namespace SummonersTable
{
    // Fixed-size links are added as the pointer moves away; a short drag does not
    // stretch or squeeze the complete arrow into the distance already travelled.
    public static class TargetArrowGeometry
    {
        public static Vector2[] Build(Vector2 start,Vector2 end)
        {
            float distance=Vector2.Distance(start,end);
            if(distance<28)return new Vector2[0];
            Vector2 control=(start+end)*.5f+Vector2.up*Mathf.Min(80,distance*.22f);
            const int samples=64;
            var points=new Vector2[samples+1];var lengths=new float[samples+1];points[0]=start;
            for(int i=1;i<=samples;i++)
            {
                float t=i/(float)samples;
                points[i]=(1-t)*(1-t)*start+2*(1-t)*t*control+t*t*end;
                lengths[i]=lengths[i-1]+Vector2.Distance(points[i-1],points[i]);
            }
            var links=new List<Vector2>();
            for(float d=8;d+14<=lengths[samples]-28;d+=24)
            {links.Add(At(d,points,lengths));links.Add(At(d+14,points,lengths));}
            // Final pair is the head tangent, not another shaft link.
            links.Add(control);links.Add(end);return links.ToArray();
        }
        static Vector2 At(float distance,Vector2[] points,float[] lengths)
        {
            for(int i=1;i<points.Length;i++)
                if(lengths[i]>=distance)return Vector2.Lerp(points[i-1],points[i],(distance-lengths[i-1])/Mathf.Max(.001f,lengths[i]-lengths[i-1]));
            return points[points.Length-1];
        }
    }
}
