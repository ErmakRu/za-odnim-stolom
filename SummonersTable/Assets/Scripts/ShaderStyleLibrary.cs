using UnityEngine;
namespace SummonersTable
{
    [CreateAssetMenu(menuName="Summoners Table/Shader styles")]
    public sealed class ShaderStyleLibrary:ScriptableObject
    {
        [Tooltip("Built-in renderer adaptations of the two packages supplied in stuff.")]
        public Material toon,painterly;
        public Material Template(int mode)=>mode==1?toon:mode==2?painterly:null;
    }
}
