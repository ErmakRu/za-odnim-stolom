using UnityEngine;
namespace SummonersTable
{
    public sealed class EffectsVolume : MonoBehaviour
    {
        public float baseVolume=.15f;AudioSource source;
        void Awake(){source=GetComponent<AudioSource>();Update();}
        void Update(){if(source!=null)source.volume=baseVolume*AudioSettings.Effects;}
    }
}
