using System.IO;
using UnityEngine;
namespace SummonersTable
{
    public sealed class PresentationLab : MonoBehaviour
    {
        public PresentationSettings settings;
        public AudioSource musicSource;
        public bool showPanel=true;
        string status="";
        System.Collections.IEnumerator Start(){var app=FindFirstObjectByType<GameApp>();while(!app.IsReady)yield return null;app.StartPresentationLab();}
        void OnGUI()
        {
            if(!showPanel)return;
            var board=FindFirstObjectByType<TableBoard>();if(board==null||board.CameraRig==null)return;
            var app=FindFirstObjectByType<GameApp>();var camera=board.CameraRig;var data=camera.Data;
            var previousMatrix=GUI.matrix;float scale=Mathf.Min(Screen.width/1600f,Screen.height/1000f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)/2,(Screen.height-1000*scale)/2,0),Quaternion.identity,new Vector3(scale,scale,1));
            GUILayout.BeginArea(new Rect(12,110,260,480),GUI.skin.box);
            GUILayout.Label("ЛАБОРАТОРИЯ ПРЕЗЕНТАЦИИ");GUILayout.Label("Камера: "+data.cameraModes[camera.Mode].name);
            GUILayout.Label("Чувствительность: "+data.lookSensitivity.ToString("0.0"));data.lookSensitivity=GUILayout.HorizontalSlider(data.lookSensitivity,.2f,8);
            var mode=data.cameraModes[camera.Mode];GUILayout.Label("Высота: "+mode.height.ToString("0.0"));mode.height=GUILayout.HorizontalSlider(mode.height,1.5f,24);
            GUILayout.Label("Радиус: "+mode.distance.ToString("0.0"));mode.distance=GUILayout.HorizontalSlider(mode.distance,0,15);
            GUILayout.Label("Угол обзора: "+mode.fieldOfView.ToString("0"));mode.fieldOfView=GUILayout.HorizontalSlider(mode.fieldOfView,30,90);
            if(GUILayout.Button("Следующий вид"))camera.SetMode((camera.Mode+1)%3);
            if(GUILayout.Button("Сбросить тестовый стол"))app.StartPresentationLab();
            if(GUILayout.Button("Атаки / попадание / урон"))app.LabAttack();
            if(GUILayout.Button("Завершить тестовый QTE"))app.LabFinishQte();
            if(GUILayout.Button("Музыка: вкл / выкл")&&musicSource!=null){if(musicSource.isPlaying)musicSource.Stop();else if(musicSource.clip!=null)musicSource.Play();else status="Назначьте AudioClip в инспекторе.";}
            if(GUILayout.Button("Сохранить presentation.json"))
            {string path=Path.Combine(Application.persistentDataPath,"presentation.json");File.WriteAllText(path,JsonUtility.ToJson(data,true));status=path;}
            if(GUILayout.Button("Загрузить presentation.json"))
            {try{camera.Apply(JsonUtility.FromJson<PresentationData>(File.ReadAllText(Path.Combine(Application.persistentDataPath,"presentation.json"))));status="Настройки загружены";}catch(System.Exception e){status=e.Message;}}
            GUILayout.Label(status);GUILayout.EndArea();GUI.matrix=previousMatrix;
        }
    }
}
