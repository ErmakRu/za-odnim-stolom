using UnityEngine;
namespace SummonersTable
{
    public static class AudioSettings
    {
        public static float Master{get;private set;}=.8f;
        public static float Effects{get;private set;}=.75f;
        static bool cannotSave;
        public static void Apply(float master,float effects)
        {
            Master=Mathf.Clamp01(master);Effects=Mathf.Clamp01(effects);AudioListener.volume=Master;
            if(cannotSave)return;
            try{PlayerPrefs.SetFloat("audio.master",Master);PlayerPrefs.SetFloat("audio.effects",Effects);PlayerPrefs.Save();}
            catch(PlayerPrefsException){cannotSave=true;Debug.LogWarning("Audio preferences are read-only; volume still applies for this session.");}
        }
        public static void Initialize(){Master=Mathf.Clamp01(PlayerPrefs.GetFloat("audio.master",.8f));Effects=Mathf.Clamp01(PlayerPrefs.GetFloat("audio.effects",.75f));AudioListener.volume=Master;}
    }
}
