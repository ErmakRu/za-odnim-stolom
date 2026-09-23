using System.IO;
using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed class PresentationLab : MonoBehaviour
    {
        public PresentationSettings settings;public AudioSource musicSource;public WidgetScreen controls;
        public bool showPanel=true;
        string status="";int previewAnimation,previewSpell;TableBoard board;
        readonly string[] animations={"Idle_Normal","Defend","AttackCombo04","Die","AttackCombo05","DashForward","DashBackward","DieRecover","Dizzy","GetHit","Sliding"};
        System.Collections.IEnumerator Start()
        {
            var app=FindFirstObjectByType<GameApp>();while(!app.IsReady)yield return null;app.StartPresentationLab();board=FindFirstObjectByType<TableBoard>(FindObjectsInactive.Include);
            controls.Click("camera",()=>board.CameraRig.SetMode((board.CameraRig.Mode+1)%3));controls.Click("reset",app.StartPresentationLab);
            controls.Click("attack",app.LabAttack);controls.Click("qte",app.LabFinishQte);
            controls.Click("nextAnimation",()=>{previewAnimation=(previewAnimation+1)%animations.Length;board.Actor(1)?.Play(animations[previewAnimation],3);});
            controls.Click("animation",()=>board.Actor(1)?.Play(animations[previewAnimation],3));controls.Click("title",()=>FindFirstObjectByType<MotionAnnouncements>()?.Preview("ВАШ ХОД"));
            controls.Click("nextSpell",()=>previewSpell=(previewSpell+1)%8);controls.Click("spell",()=>board.PreviewSpell("S"+(previewSpell+1).ToString("00")));
            controls.Click("music",()=>{if(musicSource.isPlaying)musicSource.Stop();else musicSource.Play();});
            controls.Click("save",SaveCamera);
            controls.Click("load",()=>{ConfigRuntime.Reload();status=ConfigRuntime.Message;});
            controls.Get<Slider>("sensitivity").onValueChanged.AddListener(v=>board.CameraRig.Data.lookSensitivity=v);
            controls.Get<Slider>("height").onValueChanged.AddListener(v=>board.CameraRig.Data.cameraModes[board.CameraRig.Mode].height=v);
            controls.Get<Slider>("distance").onValueChanged.AddListener(v=>board.CameraRig.Data.cameraModes[board.CameraRig.Mode].distance=v);
            controls.Get<Slider>("fov").onValueChanged.AddListener(v=>board.CameraRig.Data.cameraModes[board.CameraRig.Mode].fieldOfView=v);
        }
        void SaveCamera()
        {
            string temp=null;
            try
            {
                string path=Path.Combine(ConfigRuntime.DirectoryPath,"presentation.json");var data=ConfigBundle.Clone(ConfigRuntime.Current.presentation);data.camera=ConfigBundle.Clone(board.CameraRig.Data);
                string text=JsonUtility.ToJson(data,true);ConfigBundle.Read(ConfigRuntime.DirectoryPath,"presentation.json",text);
                temp=path+".tmp";File.WriteAllText(temp,text);File.Replace(temp,path,null);ConfigRuntime.Reload();status="Сохранено: Config/presentation.json";
#if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh();
#endif
            }
            catch(System.Exception e){status=e.Message;}
            finally{if(temp!=null&&File.Exists(temp))File.Delete(temp);}
        }
        void Update()
        {
            if(controls==null)return;controls.Show(showPanel);if(board==null||!showPanel)return;
            var rig=board.CameraRig;var data=rig.Data;var mode=data.cameraModes[rig.Mode];
            controls.Text("heading","ЛАБОРАТОРИЯ · "+mode.name);controls.Text("status",status);
            SliderValue("sensitivity","Чувствительность",data.lookSensitivity);SliderValue("height","Высота",mode.height);SliderValue("distance","Радиус",mode.distance);SliderValue("fov","Угол обзора",mode.fieldOfView);
            controls.Text("animationLabel",animations[previewAnimation]);controls.Text("spellLabel",board.cardLibrary.Find("S"+(previewSpell+1).ToString("00")).definition.name);
        }
        void SliderValue(string id,string name,float value){controls.Text(id+"Label",name+": "+value.ToString("0.0"));controls.Get<Slider>(id).SetValueWithoutNotify(value);}
    }
}
