using UnityEngine;
namespace SummonersTable
{
    public sealed class WorldArrowView : MonoBehaviour
    {
        [System.NonSerialized]public ArrowStyle previewStyle;Material initialMaterial;
        public LineRenderer line,head;public int segments=25;public float headLength=.26f,headWidth=.14f,widthMultiplier=1;
        public void Set(Vector3 a,Vector3 b,float lift,float width,Color color)
        {
            var style=previewStyle??ConfigRuntime.Current?.ui.arrow;if(style!=null){headLength=style.worldHeadLength;headWidth=style.worldHeadWidth;width*=style.worldWidth/.045f;if(initialMaterial==null)initialMaterial=line.sharedMaterial;line.sharedMaterial=head.sharedMaterial=ConfigRuntime.Assets.Get<Material>(style.worldMaterial)??initialMaterial;}
            width*=widthMultiplier;line.startColor=line.endColor=head.startColor=head.endColor=color;
            line.startWidth=line.endWidth=width;head.startWidth=head.endWidth=width*1.4f;line.positionCount=Mathf.Max(3,segments);
            for(int i=0;i<line.positionCount;i++){float t=i/(float)(line.positionCount-1);line.SetPosition(i,Vector3.Lerp(a,b,t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*lift));}
            var d=(b-line.GetPosition(line.positionCount-3)).normalized;var side=Vector3.Cross(d,Vector3.up).normalized;
            head.positionCount=3;head.SetPositions(new[]{b-d*headLength+side*headWidth,b,b-d*headLength-side*headWidth});
        }
    }
}
