using System;
using UnityEngine;

namespace SummonersTable
{
    [Serializable] public sealed class HeroDefinition {public string id,name;public GameObject prefab;}
    [CreateAssetMenu(menuName="Summoners Table/Hero library")]
    public sealed class HeroLibrary : ScriptableObject
    {
        public HeroDefinition[] heroes;
        public RuntimeAnimatorController controller;
        public Material naturalMaterial;
        public GameObject chair,table;
        public float heroScale=1.65f,seatedHeight=.50f;
        public HeroDefinition Find(string id){return Array.Find(heroes,h=>h.id==id)??heroes[0];}
    }
}
