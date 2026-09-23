using System;
using UnityEngine;
namespace SummonersTable
{
    public sealed class AssetIdAttribute:PropertyAttribute {public readonly string kind;public AssetIdAttribute(string kind){this.kind=kind;}}
    public sealed class AudioEventAttribute:PropertyAttribute {}
    public sealed class WorldEventAttribute:PropertyAttribute {}
    public enum AudioBus { Effects, Ambience, Voices, Music }
    [Serializable] public sealed class SoundVariant
    {
        [AssetId("audio")] public string clip="";
        [Range(0,1)] public float volume=.2f;
        public bool randomPitch;[Range(.1f,3)]public float pitch=1;
        public Vector2 pitchRange=new Vector2(.92f,1.08f);
        [Range(0,10)] public float delay;
    }
    [Serializable] public sealed class LocationEventRule
    {
        public int id;public bool enabled;
        [WorldEvent]public int eventId=3;
        [Range(0,100)]public float chance=30;
        [Min(1)]public int everyTurns=1;
        public bool randomDuration;[Min(1)]public int duration=2;
        public Vector2Int durationRange=new Vector2Int(1,3);
    }
    [Serializable] public sealed class LocationConfig
    {
        public int schemaVersion=1,id;
        public string name="Таверна";
        public bool timeChanges;
        public float playersTurnTime=-1,qte=-1,revealTime=-1,gameTimer=-1;
        public Vector2Int playerRange=new Vector2Int(2,4);
        [Range(.25f,3)]public float scale=1;
        [AssetId("interior")] public string scene="";
        public LocationEventRule[] events=Array.Empty<LocationEventRule>();
    }
    [Serializable] public sealed class WorldEventDef
    {
        public int id=3;
        public string name="Да не бомбит у меня!";
        [TextArea]public string description="Каждый второй верный символ QTE наносит 1 урон случайному существу на столе.";
        public WorldEventMechanic mechanic=WorldEventMechanic.QteUnitDamage;
        [Min(1)]public int everySymbols=2;
        [Min(0)]public int value=1;
        [AssetId("prefab")] public string vfx="";
        [AudioEvent]public string sound="damage.hit";
        [Min(.01f)]public float vfxScale=.25f,lifetime=1.5f;
    }
    public enum WorldEventMechanic { QteUnitDamage, TurnHeroHeal, TurnHeroDamage }
    [Serializable] public sealed class EventsConfig {public int schemaVersion=1;public WorldEventDef[] events=Array.Empty<WorldEventDef>();}
    [Serializable] public sealed class AnimationStep
    {
        [AssetId("animation")]public string clip="";
        [Range(.05f,4)]public float speed=1;
        public bool loop;
        [AssetId("prefab")]public string vfx="";
        [AudioEvent]public string sound="";
        public Vector3 effectOffset=new Vector3(0,1,0);
        [Min(.01f)]public float effectScale=.3f,effectLifetime=2;
    }
    [Serializable] public sealed class AnimationStateDef
    {
        public string id="idle";
        public AnimationStep[] steps=Array.Empty<AnimationStep>();
    }
    [Serializable] public sealed class PlayerAnimationsConfig {public int schemaVersion=1;public AnimationStateDef[] states=Array.Empty<AnimationStateDef>();}
    [Serializable] public sealed class FanStyle
    {
        [Min(.1f)]public float radius=1.7f;
        [Range(0,180)]public float spread=116;
        [Range(1,90)]public float maximumStep=23;
        public Vector2 cardSize=new Vector2(174,244);
        public float uiUnitsPerMetre=240;
        [Range(.05f,1)]public float uiPerspective=.22f;
        public Vector2 worldCardSize=new Vector2(1.05f,1.48f);
    }
    [Serializable] public sealed class ArrowStyle
    {
        public float width=7,headLength=22,headWidth=12,worldWidth=.045f,worldHeadLength=.26f,worldHeadWidth=.14f;
        [AssetId("material")]public string uiMaterial="",worldMaterial="";
        [AssetId("prefab")]public string beginEffect="",dragEffect="",selectEffect="";
        [AudioEvent]public string beginSound="",dragSound="",selectSound="";
        public float effectScale=.2f,effectLifetime=1;
        public Color color=new Color(.97f,.74f,.36f);
    }
    [Serializable] public sealed class InterfaceConfig
    {
        public int schemaVersion=1;
        public FanStyle fan=new FanStyle();
        public ArrowStyle arrow=new ArrowStyle();
        public Vector2 healthSize=new Vector2(170,26);
        public Color healthColor=new Color(.3f,.85f,.76f),healthTrailColor=new Color(1,.7f,.25f);
        public bool healthUsesPlayerColor;
        [AssetId("material")]public string highlightMaterial="";
        public Color highlightColor=new Color(.97f,.74f,.36f);
    }
    [Serializable]public sealed class UserPreferences
    {
        public int schemaVersion=1,screenMode=2,width=1600,height=900,refreshRate=60,frameLimit=60,shader;
        public float master=.8f,effects=.75f,ambience=.65f,voices=.8f,music=.5f;
        public Color highlightColor=new Color(.97f,.74f,.36f);
    }
}
