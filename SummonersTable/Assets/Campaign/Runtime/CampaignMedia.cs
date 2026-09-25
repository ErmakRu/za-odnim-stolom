using System;
using System.Linq;
using UnityEngine;
namespace SummonersTable
{
    [Serializable] public sealed class CampaignSound { public string id;public AudioClip clip; }
    public sealed class CampaignMedia:ScriptableObject
    {
        public CampaignSound[] sounds=Array.Empty<CampaignSound>();
        public AudioClip Find(string id)=>sounds.FirstOrDefault(s=>s.id==id)?.clip;
    }
}
