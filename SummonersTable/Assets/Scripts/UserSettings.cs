using System;
using System.IO;
using UnityEngine;
namespace SummonersTable
{
    public static class UserSettings
    {
        public static string TestPath;
        public static string PathName=>TestPath??Path.Combine(Application.persistentDataPath,"user-settings.json");
        static UserPreferences data;public static event Action Changed;
        public static UserPreferences Data {get{if(data==null)Load();return data;}}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){data=null;Changed=null;TestPath=null;}
        public static void Load()
        {
            try{data=File.Exists(PathName)?ConfigJson.Read<UserPreferences>(File.ReadAllText(PathName)):Defaults();Validate(data);}
            catch(Exception e){Debug.LogWarning("User settings: "+e.Message);data=Defaults();}
            Apply(false);
        }
        static UserPreferences Defaults()=>new UserPreferences{highlightColor=ConfigRuntime.Current?.ui.highlightColor??new Color(.97f,.74f,.36f)};
        static void Validate(UserPreferences p)
        {
            if(p.schemaVersion!=1||p.screenMode<0||p.screenMode>2||p.width<640||p.width>7680||p.height<480||p.height>4320||p.refreshRate<24||p.refreshRate>500||p.frameLimit< -1||p.frameLimit==0||p.frameLimit>500)throw new FormatException("Invalid display settings");
            foreach(float v in new[]{p.master,p.effects,p.ambience,p.voices,p.music,p.highlightColor.r,p.highlightColor.g,p.highlightColor.b,p.highlightColor.a})if(!float.IsFinite(v)||v<0||v>1)throw new FormatException("Invalid volume/color");
            p.shader=Mathf.Clamp(p.shader,0,2);
        }
        public static void Apply(bool save=true,bool display=false)
        {
            Validate(Data);AudioListener.volume=Data.master;Application.targetFrameRate=Data.frameLimit;
            if(display&&!Application.isEditor){QualitySettings.vSyncCount=0;Screen.SetResolution(Data.width,Data.height,(FullScreenMode)(Data.screenMode==0?0:Data.screenMode==1?1:3),new RefreshRate{numerator=(uint)Data.refreshRate,denominator=1});}
            if(save)Save();Changed?.Invoke();
        }
        public static void Save()
        {
            string temporary=PathName+".tmp";
            try{Directory.CreateDirectory(Path.GetDirectoryName(PathName));File.WriteAllText(temporary,JsonUtility.ToJson(Data,true));if(File.Exists(PathName))File.Replace(temporary,PathName,null);else File.Move(temporary,PathName);}
            catch(Exception e){Debug.LogWarning("Cannot save user settings: "+e.Message);}
            finally{if(File.Exists(temporary))File.Delete(temporary);}
        }
        public static float Volume(AudioBus bus)=>bus switch{AudioBus.Ambience=>Data.ambience,AudioBus.Voices=>Data.voices,AudioBus.Music=>Data.music,_=>Data.effects};
    }
}
