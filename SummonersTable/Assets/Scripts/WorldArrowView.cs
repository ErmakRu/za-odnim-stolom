using UnityEngine;
using UnityEngine.Rendering;
namespace SummonersTable
{
    public sealed class WorldArrowView:MonoBehaviour
    {
        [System.NonSerialized]public ArrowStyle previewStyle;
        public LineRenderer line,head;public int segments=64;
        public float headLength=.36f,headWidth=.22f,widthMultiplier=1;
        public Material magicMaterial;Material water,magic,waterSource;
        Material Surface(bool spell)
        {
            var style=previewStyle??ConfigRuntime.Current?.ui.arrow;
            var source=style!=null?ConfigRuntime.Assets.Get<Material>(style.worldMaterial):line.sharedMaterial;
            if(water==null||waterSource!=source){if(water!=null)Destroy(water);waterSource=source;if(source!=null)water=Clone(source);}
            if(spell&&magicMaterial!=null&&magic==null)magic=Clone(magicMaterial);
            return spell&&magic!=null?magic:water;
        }
        Material Clone(Material source)
        {
            var m=new Material(source);m.name=source.name+" — arrow";
            if(m.HasProperty("_ZTest"))m.SetFloat("_ZTest",(float)CompareFunction.Always);
            if(m.HasProperty("_Cull"))m.SetFloat("_Cull",0);
            if(m.HasProperty("_DepthFade"))m.SetFloat("_DepthFade",0);
            if(m.HasProperty("_AlphaIsDissolve"))m.SetFloat("_AlphaIsDissolve",0);
            m.renderQueue=3100;return m;
        }
        void Draw(Vector3 a,Vector3 b,Vector3 bend,Vector3 normal,float width,float length,float spread,bool spell)
        {
            var surface=Surface(spell);line.sharedMaterial=head.sharedMaterial=surface;
            line.useWorldSpace=head.useWorldSpace=true;line.alignment=head.alignment=LineAlignment.View;
            line.textureMode=head.textureMode=LineTextureMode.Stretch;
            line.startColor=line.endColor=head.startColor=head.endColor=Color.white;
            line.startWidth=line.endWidth=head.startWidth=head.endWidth=width;
            line.numCornerVertices=head.numCornerVertices=6;line.numCapVertices=head.numCapVertices=4;
            line.positionCount=TargetArrowGeometry.Samples+1;
            for(int i=0;i<line.positionCount;i++)line.SetPosition(i,TargetArrowGeometry.Point(a,b,bend,i/(float)(line.positionCount-1)));
            var direction=(b-line.GetPosition(line.positionCount-2)).normalized;var side=Vector3.Cross(direction,normal).normalized;
            head.positionCount=3;head.SetPositions(new[]{b-direction*length+side*spread,b,b-direction*length-side*spread});
            line.enabled=head.enabled=width>0&&Vector3.Distance(a,b)>.001f;
        }
        public void Set(Vector3 a,Vector3 b,float lift,float width,Color color,bool spell=false)
        {
            var style=previewStyle??ConfigRuntime.Current?.ui.arrow;
            float w=width<=0?0:(style?.worldWidth??.105f)*widthMultiplier;
            Draw(a,b,Vector3.up*lift*2,Vector3.up,w,style?.worldHeadLength??headLength,style?.worldHeadWidth??headWidth,spell);
        }
        public void SetScreen(Camera camera,Vector2 from,Vector2 to,bool spell)
        {
            float depth=Mathf.Max(camera.nearClipPlane+1,4);Vector3 World(Vector2 p)=>camera.ScreenToWorldPoint(new Vector3(p.x,p.y,depth));
            var style=ConfigRuntime.Current?.ui.arrow;float units=Vector3.Distance(World(Vector2.zero),World(Vector2.right));
            float logical=Mathf.Min(Screen.width/1600f,Screen.height/1000f);
            float bend=Mathf.Min(80*logical,Vector2.Distance(from,to)*.22f);
            Draw(World(from),World(to),camera.transform.up*(bend*units),camera.transform.forward,(style?.width??16)*logical*units,(style?.headLength??30)*logical*units,(style?.headWidth??19)*logical*units,spell);
        }
        void OnDestroy(){if(water!=null)Destroy(water);if(magic!=null)Destroy(magic);}
    }
}
