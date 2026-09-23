using System;
using UnityEngine;
namespace SummonersTable
{
    // A local display preference. It never changes lobby readiness or match rules.
    public static class ShaderSettings
    {
        public const string PreferenceKey="visual.shader";
        public static readonly string[] Names={"Без шейдера","Шейдер 1","Шейдер 2"};
        public static event Action Changed;
        static int mode;static bool initialized,cannotSave;
        public static int Mode {get {Initialize();return mode;}}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){initialized=false;cannotSave=false;Changed=null;}
        public static void Initialize(){if(initialized)return;initialized=true;mode=UserSettings.Data.shader;}
        public static void Apply(int value,bool save=true)
        {
            Initialize();int next=Mathf.Clamp(value,0,2);bool changed=next!=mode;mode=next;
            if(save&&!cannotSave)try{UserSettings.Data.shader=mode;UserSettings.Save();}catch(PlayerPrefsException){cannotSave=true;Debug.LogWarning("Shader preference is read-only; using the selection for this session.");}
            if(changed)Changed?.Invoke();
        }
    }
}
