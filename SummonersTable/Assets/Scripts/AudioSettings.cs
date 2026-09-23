using UnityEngine;
namespace SummonersTable
{
    public static class AudioSettings
    {
        public static float Master=>UserSettings.Data.master;
        public static float Effects=>UserSettings.Data.effects;
        public static void Apply(float master,float effects){UserSettings.Data.master=Mathf.Clamp01(master);UserSettings.Data.effects=Mathf.Clamp01(effects);UserSettings.Apply();}
        public static void Initialize(){UserSettings.Load();}
    }
}
