using System;
using System.Linq;
using UnityEngine;
namespace SummonersTable
{
    [Serializable] public sealed class CardsConfig { public int schemaVersion=1; public CardDef[] cards; public ColorDef[] typeColors,roleColors; }
    [Serializable] public sealed class DecksConfig { public int schemaVersion=1; public DeckDef[] decks; }
    [Serializable] public sealed class RulesConfig { public int schemaVersion=1; public string version="0.8.0",title="За одним столом"; public RulesDef rules=new RulesDef(); public MatchOptions defaults=new MatchOptions(); }
    [Serializable] public sealed class AudioCue { public string action; public AudioBus bus; public SoundVariant[] sounds=Array.Empty<SoundVariant>(); }
    [Serializable] public sealed class AudioConfig { public int schemaVersion=1; public AudioCue[] cues; }
    [Serializable] public sealed class EffectConfig
    {
        public string id; [AssetId("prefab")]public string travel,impact,persistent; public int motion,cardCount=2;
        public float travelScale=.22f,impactScale=.35f,persistentScale=.2f,travelSeconds=.55f,lifetime=1.8f,arc=.65f;
        public Vector2 cardSize=new Vector2(.65f,.85f);
    }
    [Serializable] public sealed class VfxConfig { public int schemaVersion=1; public EffectConfig[] effects; [AssetId("prefab")]public string attack,hit,death,qteFire,qteSmoke,qteAttempt,motionTitle; public float attackScale=.3f,impactScale=.35f,qteScale=.17f; }
    [Serializable] public sealed class TransformConfig
    {
        public Vector3 position,rotation,scale=Vector3.one;
        public static TransformConfig Read(Transform t)=>new TransformConfig{position=t.localPosition,rotation=t.localEulerAngles,scale=t.localScale};
        public void Apply(Transform t){t.localPosition=position;t.localEulerAngles=rotation;t.localScale=scale;}
    }
    [Serializable] public sealed class SeatConfig { public TransformConfig root=new TransformConfig(); }
    [Serializable] public sealed class LayoutConfig { public int players; public SeatConfig[] seats; }
    [Serializable] public sealed class WorldConfig
    {
        public int schemaVersion=1;
        public TransformConfig table,chair,avatar,heroTarget;
        public TransformConfig[] slots; public LayoutConfig[] layouts;
        public float unitsPerMetre=8,tableHeight=.75f,tableDiameter=1.5f,avatarHipHeight=4.4f;
    }
    [Serializable] public sealed class PresentationConfig
    {
        public int schemaVersion=1; public PresentationData camera=new PresentationData();
        public float cardDepth=.16f,cardFoil=.22f;
    }
    [Serializable] public sealed class AssetRef { public string id,path,kind; public UnityEngine.Object asset; }
}
