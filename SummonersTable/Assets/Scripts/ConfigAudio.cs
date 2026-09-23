using System.Collections;
using System.Linq;
using UnityEngine;
namespace SummonersTable
{
    public sealed class ConfigAudio:MonoBehaviour
    {
        static ConfigAudio host;public Transform[] buses;AudioSource ambience;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){host=null;}
        static ConfigAudio Host
        {
            get{if(host==null){var prefab=Resources.Load<ConfigAudio>("AudioRig");host=prefab!=null?Instantiate(prefab):new GameObject("AudioRig").AddComponent<ConfigAudio>();DontDestroyOnLoad(host.gameObject);}return host;}
        }
        public static SoundVariant Select(AudioCue cue)=>cue?.sounds==null||cue.sounds.Length==0?null:cue.sounds[Random.Range(0,cue.sounds.Length)];
        public static float Pitch(SoundVariant sound)=>sound.randomPitch?Random.Range(sound.pitchRange.x,sound.pitchRange.y):sound.pitch;
        public static void Play(string action)
        {
            var cue=ConfigRuntime.Current?.audio.cues.FirstOrDefault(c=>c.action==action);var sound=Select(cue);if(sound==null)return;
            var clip=ConfigRuntime.Assets.Get<AudioClip>(sound.clip);if(clip!=null&&sound.volume>0)Host.StartCoroutine(Host.Cue(cue.bus,ConfigBundle.Clone(sound),clip,action));
        }
        Transform Bus(AudioBus bus)=>buses!=null&&buses.Length>(int)bus&&buses[(int)bus]!=null?buses[(int)bus]:transform;
        IEnumerator Cue(AudioBus bus,SoundVariant sound,AudioClip clip,string action)
        {
            if(sound.delay>0)yield return new WaitForSecondsRealtime(sound.delay);
            var item=new GameObject("Audio "+action);item.transform.SetParent(Bus(bus));var source=item.AddComponent<AudioSource>();source.playOnAwake=false;source.clip=clip;source.pitch=Pitch(sound);
            var volume=item.AddComponent<EffectsVolume>();volume.baseVolume=sound.volume;volume.bus=bus;source.volume=sound.volume*UserSettings.Volume(bus);source.Play();Destroy(item,clip.length/source.pitch+.15f);
        }
        public static void RefreshAmbience()
        {
            if(!Application.isPlaying||ConfigRuntime.Current==null)return;var h=Host;var cue=ConfigRuntime.Current.audio.cues.FirstOrDefault(c=>c.action=="ambience");var sound=Select(cue);
            if(h.ambience!=null){h.ambience.Stop();Destroy(h.ambience.gameObject);h.ambience=null;}
            if(sound==null)return;var clip=ConfigRuntime.Assets.Get<AudioClip>(sound.clip);if(clip==null)return;
            var obj=new GameObject("Ambience loop");obj.transform.SetParent(h.Bus(cue.bus));h.ambience=obj.AddComponent<AudioSource>();h.ambience.playOnAwake=false;h.ambience.loop=true;h.ambience.clip=clip;h.ambience.pitch=Pitch(sound);
            var level=obj.AddComponent<EffectsVolume>();level.baseVolume=sound.volume;level.bus=cue.bus;h.ambience.volume=sound.volume*UserSettings.Volume(cue.bus);h.ambience.PlayDelayed(sound.delay);
        }
    }
}
