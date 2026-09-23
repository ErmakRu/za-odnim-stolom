using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable.Editor
{
    public static partial class PrefabAuthoring
    {
        static void CreateLab()
        {
            string path=Root+"UI/LabControls.prefab";if(File.Exists(path)){AttachLab();return;}
            var s=new Screen("Presentation laboratory",false,35);var canvas=s.view.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=s.view.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,1000);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            I("Panel",s.Parent,new Rect(10,110,330,745),ink,true);s.Text("heading",new Rect(20,116,312,55),"ЛАБОРАТОРИЯ",17);
            var ids=new[]{"sensitivity","height","distance","fov"};var min=new[]{.2f,1.5f,0,30};var max=new[]{8f,42f,26f,90f};
            for(int i=0;i<4;i++){s.Text(ids[i]+"Label",new Rect(25,176+i*55,295,23),"",16);var slider=s.Slider(ids[i],new Rect(25,204+i*55,287,18));slider.minValue=min[i];slider.maxValue=max[i];}
            s.Button("camera",new Rect(25,406,140,32),"Вид камеры");s.Button("reset",new Rect(177,406,140,32),"Сброс стола");
            s.Button("attack",new Rect(25,448,140,32),"Атаки / урон");s.Button("qte",new Rect(177,448,140,32),"Пройти QTE");
            s.Text("animationLabel",new Rect(25,488,287,28),"",17);s.Button("nextAnimation",new Rect(25,522,140,32),"Анимация ›");s.Button("animation",new Rect(177,522,140,32),"Проиграть");
            s.Text("spellLabel",new Rect(25,563,287,45),"",17);s.Button("nextSpell",new Rect(25,616,140,32),"Эффект ›");s.Button("spell",new Rect(177,616,140,32),"Показать");
            s.Button("title",new Rect(25,659,140,32),"Заголовок");s.Button("music",new Rect(177,659,140,32),"Музыка");
            s.Button("save",new Rect(25,702,140,32),"Сохранить");s.Button("load",new Rect(177,702,140,32),"Загрузить");s.Text("status",new Rect(25,745,290,99),"",13);
            s.Save("LabControls");AttachLab();
        }
        static void AttachLab()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/PresentationLab.unity");var lab=Object.FindFirstObjectByType<PresentationLab>();
            if(lab.controls==null){lab.controls=((GameObject)PrefabUtility.InstantiatePrefab(Load(Root+"UI/LabControls.prefab"))).GetComponent<WidgetScreen>();EditorSceneManager.SaveScene(scene);}
        }
    }
}
