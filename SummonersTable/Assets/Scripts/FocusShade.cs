using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FocusShade:MaskableGraphic
    {
        public bool tutorial;[Range(0,1)]public float strength=.40f;
        public float feather=.018f;Material owned;
        public Rect[] FocusAreas{get;private set;}=System.Array.Empty<Rect>();
        protected override void OnEnable(){base.OnEnable();raycastTarget=false;var shader=Shader.Find("SummonersTable/Focus Shade");if(shader!=null){owned=new Material(shader);material=owned;}Apply();}
        protected override void OnDisable(){base.OnDisable();material=null;if(owned!=null){if(Application.isPlaying)Destroy(owned);else DestroyImmediate(owned);}owned=null;}
        public void Focus(params Rect[] screenAreas){FocusAreas=screenAreas;Apply();}
        void Apply(){if(owned==null)return;owned.SetFloat("_Tutorial",tutorial?1:0);owned.SetFloat("_Strength",strength);owned.SetFloat("_Feather",feather);owned.SetFloat("_FocusCount",Mathf.Min(4,FocusAreas.Length));for(int i=0;i<4;i++){var r=i<FocusAreas.Length?FocusAreas[i]:new Rect();owned.SetVector("_Focus"+i,new Vector4(r.xMin/Screen.width,r.yMin/Screen.height,r.xMax/Screen.width,r.yMax/Screen.height));}}
        protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();var r=rectTransform.rect;vh.AddVert(new Vector3(r.xMin,r.yMin),Color.white,new Vector2(0,0));vh.AddVert(new Vector3(r.xMin,r.yMax),Color.white,new Vector2(0,1));vh.AddVert(new Vector3(r.xMax,r.yMax),Color.white,new Vector2(1,1));vh.AddVert(new Vector3(r.xMax,r.yMin),Color.white,new Vector2(1,0));vh.AddTriangle(0,1,2);vh.AddTriangle(0,2,3);}
    }
}
