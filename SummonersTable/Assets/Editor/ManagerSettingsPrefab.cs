using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace SummonersTable.Editor
{
    public static class ManagerSettingsPrefab
    {
        static Font Font=>Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        static readonly Color panel=new Color(.06f,.105f,.13f,.99f),accent=new Color(.25f,.79f,.69f);
        static List<WidgetBinding> bindings;
        static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {var go=new GameObject(name,typeof(RectTransform));var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
        static void Bind(string id,Component component){bindings.Add(new WidgetBinding{id=id,widget=component.gameObject});}
        static Text Text(string id,Transform parent,float x,float y,float w,float h,string value,int size=22)
        {var text=Rect(id,parent,x,y,w,h).gameObject.AddComponent<Text>();text.font=Font;text.fontSize=size;text.color=Color.white;text.text=value;text.raycastTarget=false;text.alignment=TextAnchor.MiddleLeft;Bind(id,text);return text;}
        static Image Image(string name,Transform parent,float x,float y,float w,float h,Color color)
        {var image=Rect(name,parent,x,y,w,h).gameObject.AddComponent<Image>();image.color=color;return image;}
        static Button Button(string id,Transform parent,float x,float y,float w,float h,string label)
        {var button=SharedButton.TopLeft(parent,id,label,new UnityEngine.Rect(x,y,w,h));Bind(id+"Text",button.GetComponent<SharedButton>().label);Bind(id,button);return button;}
        static Slider Slider(string id,Transform parent,float x,float y,float w)
        {
            var root=Rect(id,parent,x,y,w,30);var slider=root.gameObject.AddComponent<Slider>();var bg=Image("Track",root,0,10,w,10,new Color(.18f,.25f,.28f));var fill=Image("Fill",root,0,10,w,10,accent);var handle=Image("Handle",root,0,0,16,30,new Color(1,.8f,.35f));
            fill.rectTransform.anchorMin=new Vector2(0,.5f);fill.rectTransform.anchorMax=new Vector2(1,.5f);fill.rectTransform.pivot=new Vector2(.5f,.5f);fill.rectTransform.anchoredPosition=Vector2.zero;fill.rectTransform.sizeDelta=new Vector2(0,10);
            handle.rectTransform.anchorMin=handle.rectTransform.anchorMax=new Vector2(0,.5f);handle.rectTransform.pivot=new Vector2(.5f,.5f);handle.rectTransform.anchoredPosition=Vector2.zero;
            slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.minValue=0;slider.maxValue=1;Bind(id,slider);return slider;
        }
        static Dropdown Dropdown(string id,Transform parent,float x,float y,float w)
        {
            var image=Image(id,parent,x,y,w,46,new Color(.18f,.25f,.28f));var dropdown=image.gameObject.AddComponent<Dropdown>();dropdown.targetGraphic=image;
            dropdown.captionText=Text(id+"Value",image.transform,12,0,w-50,46,"Выбрать",21);Text(id+"Arrow",image.transform,w-36,0,30,46,"▼",18);
            var template=Rect("Template",image.transform,0,46,w,240);template.gameObject.AddComponent<Image>().color=panel;var scroll=template.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            var viewport=Rect("Viewport",template,4,4,w-8,232);viewport.gameObject.AddComponent<Image>().color=Color.white;viewport.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            var content=Rect("Content",viewport,0,0,w-8,42);var item=Rect("Item",content,0,0,w-8,42);var background=Image("Background",item,0,0,w-8,42,new Color(.18f,.25f,.28f));var check=Image("Checkmark",item,7,14,14,14,accent);
            var label=Text(id+"Item",item,29,0,w-40,42,"",20);var toggle=item.gameObject.AddComponent<Toggle>();toggle.targetGraphic=background;toggle.graphic=check;toggle.isOn=true;
            dropdown.template=template;dropdown.itemText=label;scroll.viewport=viewport;scroll.content=content;template.gameObject.SetActive(false);dropdown.options.Add(new Dropdown.OptionData("Выбрать"));return dropdown;
        }
        public static void Create()
        {
            string path=ConfigAuthoring.Root+"UI/SettingsPanel.prefab";var old=AssetDatabase.LoadAssetAtPath<GameObject>(path);var root=new GameObject("ESC settings",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster),typeof(WidgetScreen),typeof(UserSettingsView),typeof(PrefabConfigBinding));
            var rect=(RectTransform)root.transform;rect.sizeDelta=new Vector2(1600,1000);var canvas=root.GetComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=40;
            root.GetComponent<PrefabConfigBinding>().configId="SettingsPanel";bindings=new List<WidgetBinding>();
            Image("Background",root.transform,0,0,1600,1000,panel);Text("title",root.transform,100,55,1400,70,"НАСТРОЙКИ",38);
            var view=root.GetComponent<UserSettingsView>();
            string[] ids={"master","effects","ambience","voices","music"},names={"Общая громкость","Звуковые эффекты","Окружение","Голоса","Музыка"};var sliders=new Slider[5];var labels=new Text[5];
            for(int i=0;i<5;i++){labels[i]=Text(ids[i]+"Label",root.transform,100,170+i*86,620,36,names[i]);sliders[i]=Slider(ids[i],root.transform,108,210+i*86,604);}
            view.master=sliders[0];view.effects=sliders[1];view.ambience=sliders[2];view.voices=sliders[3];view.music=sliders[4];view.ambienceLabel=labels[2];view.voicesLabel=labels[3];view.musicLabel=labels[4];
            string[] settings={"Режим экрана","Разрешение","Частота обновления","Ограничение кадров"};for(int i=0;i<4;i++)Text("displayLabel"+i,root.transform,820,162+i*95,660,30,settings[i]);
            view.screenMode=Dropdown("screenMode",root.transform,820,199,660);view.resolution=Dropdown("resolution",root.transform,820,294,660);view.refreshRate=Dropdown("refreshRate",root.transform,820,389,660);view.frameLimit=Dropdown("frameLimit",root.transform,820,484,660);
            view.applyDisplay=Button("applyDisplay",root.transform,820,552,660,48,"ПРИМЕНИТЬ ЭКРАН");view.displayHint=Text("displayHint",root.transform,820,604,660,40,"В редакторе размер предпросмотра задаёт Game View.",16);
            var shader=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ConfigAuthoring.Root+"UI/ShaderChoice.prefab"),root.transform);var sr=(RectTransform)shader.transform;sr.anchorMin=sr.anchorMax=sr.pivot=new Vector2(0,1);sr.anchoredPosition=new Vector2(100,-650);
            Text("colorLabel",root.transform,820,657,540,32,"Цвет подсветки",23);view.colorPreview=Image("Color preview",root.transform,1410,655,70,36,accent);
            view.red=Slider("red",root.transform,828,716,180);view.green=Slider("green",root.transform,1048,716,180);view.blue=Slider("blue",root.transform,1268,716,180);
            Text("redLabel",root.transform,820,753,180,25,"Красный",17);Text("greenLabel",root.transform,1040,753,180,25,"Зелёный",17);Text("blueLabel",root.transform,1260,753,180,25,"Синий",17);
            Text("note",root.transform,100,800,600,76,"Онлайн-матч продолжает идти, пока открыты настройки.",18);
            var config=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ConfigAuthoring.Root+"UI/ConfigControls.prefab"),root.transform);var cr=(RectTransform)config.transform;cr.anchorMin=cr.anchorMax=new Vector2(.5f,.5f);cr.anchoredPosition=new Vector2(350,-345);cr.localScale=Vector3.one*.8f;
            Button("resume",root.transform,360,925,400,55,"ПРОДОЛЖИТЬ");Button("leave",root.transform,840,925,400,55,"ВЫЙТИ В МЕНЮ");
            root.GetComponent<WidgetScreen>().bindings=bindings.ToArray();PrefabUtility.SaveAsPrefabAsset(root,path);Object.DestroyImmediate(root);
        }
    }
}
