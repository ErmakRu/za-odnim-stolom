using System;
using UnityEngine;
namespace SummonersTable
{
    public enum ManagerSection { World,Audio,Vfx,PlayerAnimations,Events,Cards,Decks,Rules,Presentation,Interface }
    [ExecuteAlways] public sealed class AuthoringManager:MonoBehaviour
    {
        public ManagerSection section;
        public LocationConfig world;public new AudioConfig audio;public VfxConfig vfx;
        public PlayerAnimationsConfig playeranimations;public EventsConfig events;
        public CardsConfig cards;public DecksConfig decks;public RulesConfig rules;
        public PresentationConfig presentation;public InterfaceConfig ui;
        [Range(1,8)] public int previewPlayers=4;
        [Range(1,20)]public int previewCards=8;
        public int previewIndex;public string previewHero="badger";
        public Transform previewAnchor;
        public TableBoard table;
        public static event Action<AuthoringManager> Edited;
        public string FileName=>section==ManagerSection.Interface?"interface.json":section.ToString().ToLowerInvariant()+".json";
        public object Data=>section switch {ManagerSection.World=>world,ManagerSection.Audio=>audio,ManagerSection.Vfx=>vfx,ManagerSection.PlayerAnimations=>playeranimations,ManagerSection.Events=>events,ManagerSection.Cards=>cards,ManagerSection.Decks=>decks,ManagerSection.Rules=>rules,ManagerSection.Presentation=>presentation,_=>ui};
        public string Export()=>JsonUtility.ToJson(Data,true);
        public void Import(string json)
        {
            switch(section)
            {
                case ManagerSection.World:world=ConfigJson.Read<LocationConfig>(json);break;
                case ManagerSection.Audio:audio=ConfigJson.Read<AudioConfig>(json);break;
                case ManagerSection.Vfx:vfx=ConfigJson.Read<VfxConfig>(json);break;
                case ManagerSection.PlayerAnimations:playeranimations=ConfigJson.Read<PlayerAnimationsConfig>(json);break;
                case ManagerSection.Events:events=ConfigJson.Read<EventsConfig>(json);break;
                case ManagerSection.Cards:cards=ConfigJson.Read<CardsConfig>(json);break;
                case ManagerSection.Decks:decks=ConfigJson.Read<DecksConfig>(json);break;
                case ManagerSection.Rules:rules=ConfigJson.Read<RulesConfig>(json);break;
                case ManagerSection.Presentation:presentation=ConfigJson.Read<PresentationConfig>(json);break;
                case ManagerSection.Interface:ui=ConfigJson.Read<InterfaceConfig>(json);break;
            }
            Edited?.Invoke(this);
        }
        void OnValidate(){Edited?.Invoke(this);}
    }
}
