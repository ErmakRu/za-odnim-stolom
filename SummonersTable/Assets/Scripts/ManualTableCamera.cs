using UnityEngine;

namespace SummonersTable
{
    public sealed class ManualTableCamera : MonoBehaviour
    {
        public PresentationSettings settings;
        public bool loadJsonOverride;
        public int Mode {get;private set;}=1;
        public Vector2 Look {get;private set;}
        public PresentationData Data {get;private set;}
        int viewer=-1,players=4;
        Camera view;
        public void Initialize(Camera camera)
        {
            view=camera;if(ConfigRuntime.Available)settings=ConfigRuntime.Settings();else if(settings==null)settings=Resources.Load<PresentationSettings>("PresentationSettings");
            Data=settings!=null?JsonUtility.FromJson<PresentationData>(settings.ToJson()):new PresentationData();
            string path=System.IO.Path.Combine(Application.persistentDataPath,"presentation.json");
            if(!ConfigRuntime.Available&&loadJsonOverride&&System.IO.File.Exists(path))
            {try{var data=JsonUtility.FromJson<PresentationData>(System.IO.File.ReadAllText(path));data.Validate();Data=data;}catch(System.Exception e){Debug.LogWarning("Settings override rejected: "+e.Message);}}
            Mode=Data.initialCameraMode;
        }
        public void Apply(PresentationData data){data.Validate();Data=data;SetMode(Mode);}
        public void SetMode(int mode){Mode=Mathf.Clamp(mode,0,2);Look=Vector2.zero;}
        public void RotateView(Vector2 delta)
        {
            var m=Data.cameraModes[Mode];var look=Look+delta;
            Look=new Vector2(Mathf.Clamp(look.x,-m.yawLimit,m.yawLimit),Mathf.Clamp(look.y,-m.upLimit,m.downLimit));
        }
        public void Sync(int seat,int count,bool input=true,bool snap=false)
        {
            if(Data==null)Initialize(GetComponent<Camera>());
            if(viewer!=seat){viewer=seat;Look=Vector2.zero;snap=true;}players=count;
            if(input)
            {
                float wheel=Input.mouseScrollDelta.y;if(Mathf.Abs(wheel)>.01f)SetMode(Mode+(wheel>0?-1:1));
                if(Input.GetMouseButton(1))RotateView(new Vector2(Input.GetAxisRaw("Mouse X"),-Input.GetAxisRaw("Mouse Y"))*Data.lookSensitivity);
            }
            var mode=Data.cameraModes[Mode];var away=TableBoard.Away(viewer,players);
            var center=TableBoard.Center;var pos=center+away*mode.distance+Vector3.up*mode.height;
            var rotation=Quaternion.LookRotation(center+Vector3.up*mode.focusHeight-pos)*Quaternion.Euler(Look.y,Look.x,0);
            float t=snap?1:1-Mathf.Exp(-Time.unscaledDeltaTime*Data.cameraSmoothing);
            view.transform.SetPositionAndRotation(Vector3.Lerp(view.transform.position,pos,t),Quaternion.Slerp(view.transform.rotation,rotation,t));
            view.fieldOfView=Mathf.Lerp(view.fieldOfView,mode.fieldOfView,t);
        }
    }
}
