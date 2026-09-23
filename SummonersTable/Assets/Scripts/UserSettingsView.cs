using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed class UserSettingsView:MonoBehaviour
    {
        public Slider master,effects,ambience,voices,music,red,green,blue;
        public Text ambienceLabel,voicesLabel,musicLabel,displayHint;
        public Dropdown screenMode,resolution,refreshRate,frameLimit;
        public Button applyDisplay;public Image colorPreview;
        readonly List<Vector2Int> resolutions=new List<Vector2Int>();readonly List<int> rates=new List<int>();readonly int[] frames={30,60,90,120,144,165,240,-1};
        bool initialized;
        void OnEnable(){if(!Application.isPlaying)return;if(!initialized)Initialize();Refresh();}
        void Initialize()
        {
            initialized=true;
            resolutions.AddRange(new[]{new Vector2Int(1280,720),new Vector2Int(1600,900),new Vector2Int(1920,1080),new Vector2Int(2560,1440),new Vector2Int(3840,2160)});
            foreach(var value in Screen.resolutions){var size=new Vector2Int(value.width,value.height);if(!resolutions.Contains(size))resolutions.Add(size);}
            var saved=new Vector2Int(UserSettings.Data.width,UserSettings.Data.height);if(!resolutions.Contains(saved))resolutions.Add(saved);
            resolutions.Sort((a,b)=>a.x!=b.x?a.x.CompareTo(b.x):a.y.CompareTo(b.y));
            rates.AddRange(new[]{30,60,75,90,120,144,165,240});foreach(var value in Screen.resolutions){int hz=Mathf.RoundToInt((float)value.refreshRateRatio.value);if(!rates.Contains(hz))rates.Add(hz);}if(!rates.Contains(UserSettings.Data.refreshRate))rates.Add(UserSettings.Data.refreshRate);rates.Sort();
            Options(screenMode,new[]{"Полный экран","Оконный без рамки","Оконный с рамкой"});Options(resolution,resolutions.Select(v=>v.x+" × "+v.y));Options(refreshRate,rates.Select(v=>v+" Гц"));Options(frameLimit,frames.Select(v=>v<0?"Без ограничения":v+" FPS"));
            ambience.onValueChanged.AddListener(v=>{UserSettings.Data.ambience=v;UserSettings.Apply();});voices.onValueChanged.AddListener(v=>{UserSettings.Data.voices=v;UserSettings.Apply();});music.onValueChanged.AddListener(v=>{UserSettings.Data.music=v;UserSettings.Apply();});
            foreach(var slider in new[]{red,green,blue})slider.onValueChanged.AddListener(v=>{UserSettings.Data.highlightColor=new Color(red.value,green.value,blue.value,1);UserSettings.Apply();});
            applyDisplay.onClick.AddListener(()=>{var data=UserSettings.Data;data.screenMode=screenMode.value;data.width=resolutions[resolution.value].x;data.height=resolutions[resolution.value].y;data.refreshRate=rates[refreshRate.value];data.frameLimit=frames[frameLimit.value];UserSettings.Apply(true,true);displayHint.text=Application.isEditor?"Сохранено. Режим окна ОС применяется в Player; в Editor используйте Game View.":"Параметры экрана применены.";});
        }
        static void Options(Dropdown control,IEnumerable<string> labels){control.ClearOptions();control.AddOptions(labels.ToList());}
        public void Refresh()
        {
            var d=UserSettings.Data;master.SetValueWithoutNotify(d.master);effects.SetValueWithoutNotify(d.effects);ambience.SetValueWithoutNotify(d.ambience);voices.SetValueWithoutNotify(d.voices);music.SetValueWithoutNotify(d.music);
            red.SetValueWithoutNotify(d.highlightColor.r);green.SetValueWithoutNotify(d.highlightColor.g);blue.SetValueWithoutNotify(d.highlightColor.b);
            screenMode.SetValueWithoutNotify(d.screenMode);resolution.SetValueWithoutNotify(Mathf.Max(0,resolutions.IndexOf(new Vector2Int(d.width,d.height))));refreshRate.SetValueWithoutNotify(Mathf.Max(0,rates.IndexOf(d.refreshRate)));frameLimit.SetValueWithoutNotify(Mathf.Max(0,Array.IndexOf(frames,d.frameLimit)));
        }
        void Update(){if(!initialized)return;ambienceLabel.text="Окружение: "+Mathf.RoundToInt(UserSettings.Data.ambience*100)+"%";voicesLabel.text="Голоса: "+Mathf.RoundToInt(UserSettings.Data.voices*100)+"%";musicLabel.text="Музыка: "+Mathf.RoundToInt(UserSettings.Data.music*100)+"%";colorPreview.color=UserSettings.Data.highlightColor;}
    }
}
