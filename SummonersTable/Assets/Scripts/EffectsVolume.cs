using UnityEngine;
namespace SummonersTable
{
    public sealed class EffectsVolume:MonoBehaviour
    {
        public float baseVolume=.15f;public AudioBus bus;AudioSource source;
        void Awake(){source=GetComponent<AudioSource>();Update();}
        void Update(){if(source!=null)source.volume=baseVolume*UserSettings.Volume(bus);}
    }
}
