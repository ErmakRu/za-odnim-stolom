using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
namespace SummonersTable
{
    public sealed partial class ConfigBundle
    {
        public static readonly string[] Files={"cards.json","decks.json","rules.json","audio.json","vfx.json","world.json","presentation.json","prefabs.json","playeranimations.json","events.json","interface.json"};
        public CardsConfig cards;public DecksConfig decks;public RulesConfig rules;public AudioConfig audio;public VfxConfig vfx;public LocationConfig world;public PlayerAnimationsConfig animations;public EventsConfig events;public InterfaceConfig ui;public PresentationConfig presentation;
        public List<Dictionary<string,object>> prefabs;public string gameplayHash;
        public Catalog Catalog()=>new Catalog{version=rules.version,title=rules.title,configHash=gameplayHash,rules=Clone(rules.rules),world=Clone(world),events=Clone(events),cards=cards.cards.Select(Clone).ToList(),decks=decks.decks.Select(Clone).ToList(),typeColors=cards.typeColors.Select(Clone).ToList(),roleColors=cards.roleColors.Select(Clone).ToList()};
        public static T Clone<T>(T value)=>JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
        public static ConfigBundle Read(string directory,string replacementFile=null,string replacement=null)
        {return Read(name=>name==replacementFile?replacement:File.ReadAllText(Path.Combine(directory,name),Encoding.UTF8));}
        public static ConfigBundle Read(Func<string,string> read)
        {
            var b=new ConfigBundle();T R<T>(string name){try{return ConfigJson.Read<T>(read(name));}catch(Exception e){throw new FormatException(name+": "+e.Message,e);}}
            b.cards=R<CardsConfig>("cards.json");b.decks=R<DecksConfig>("decks.json");b.rules=R<RulesConfig>("rules.json");b.audio=R<AudioConfig>("audio.json");b.vfx=R<VfxConfig>("vfx.json");b.world=R<LocationConfig>("world.json");b.animations=R<PlayerAnimationsConfig>("playeranimations.json");b.events=R<EventsConfig>("events.json");b.ui=R<InterfaceConfig>("interface.json");b.presentation=R<PresentationConfig>("presentation.json");
            var p=ConfigJson.Object(ConfigJson.Parse(read("prefabs.json")));if(p.Keys.Any(k=>k!="schemaVersion"&&k!="prefabs")||Convert.ToInt32(p["schemaVersion"])!=1)throw new FormatException("prefabs.json: invalid schema");
            b.prefabs=((List<object>)p["prefabs"]).Select(ConfigJson.Object).ToList();b.Validate();
            using(var hash=SHA256.Create())b.gameplayHash=BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(b.GameplayJson()))).Replace("-","").ToLowerInvariant();
            return b;
        }
        static void Check(bool ok,string message){if(!ok)throw new FormatException(message);}
        static void Range(float n,float min,float max,string key){Check(float.IsFinite(n)&&n>=min&&n<=max,key+": expected "+min+"…"+max);}
        public void Validate()
        {
            Check(cards.schemaVersion==1&&decks.schemaVersion==1&&rules.schemaVersion==1&&audio.schemaVersion==1&&vfx.schemaVersion==1&&world.schemaVersion==1&&presentation.schemaVersion==1,"schemaVersion must be 1");
            Check(cards.cards!=null&&cards.typeColors!=null&&cards.roleColors!=null&&decks.decks!=null,"cards/decks arrays are required");
            var c=Catalog();c.Validate();rules.defaults.Validate();Check(!string.IsNullOrWhiteSpace(rules.version),"rules.version required");
            var r=rules.rules;Range(r.commandersQte,2,100,"commandersQte");Range(r.wizardSpells,1,20,"wizardSpells");Range(r.wizardMixedSpells,0,r.wizardSpells,"wizardMixedSpells");Range(r.wizardCreatures,1,5,"wizardCreatures");Range(r.qteMaxLength,2,100,"qteMaxLength");Range(r.qteBaseSeconds,1,120,"qteBaseSeconds");Range(r.qteMinimumSeconds,1,120,"qteMinimumSeconds");Range(r.qteSecondsPerSymbol,0,30,"qteSecondsPerSymbol");Range(r.qteMistakePenalty,0,30,"qteMistakePenalty");Check(r.effectLimits!=null&&r.effectLimits.Select(x=>x.effect).Distinct().Count()==r.effectLimits.Length,"Unique effectLimits required");foreach(var limit in r.effectLimits){Check(new RulesDef().effectLimits.Any(x=>x.effect==limit.effect),"Unknown effect limit: "+limit.effect);Range(limit.maximum,0,100,limit.effect+".maximum");}Range(r.heroHp,1,999,"rules.heroHp");Range(r.deckSize,1,120,"rules.deckSize");Range(r.handLimit,1,20,"rules.handLimit");Range(r.startingHand,0,r.handLimit,"rules.startingHand");Check(r.boardSlots==5,"This scene requires boardSlots=5");Range(r.rounds,1,10,"rules.rounds");Range(r.turnSeconds,5,600,"rules.turnSeconds");Range(r.revealSeconds,0,30,"rules.revealSeconds");Range(r.qteMistakes,1,10,"rules.qteMistakes");Range(r.roundWinPoints,0,100,"rules.roundWinPoints");Range(r.eliminationPoints,0,100,"rules.eliminationPoints");
            Check(decks.decks.Length==3&&decks.decks.Select(d=>d.id).Distinct().Count()==3,"Three unique deck IDs required");
            var baseline=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("Data/catalog").text);
            var assets=Resources.Load<ConfigAssets>("ConfigAssets");
            foreach(var card in cards.cards)
            {
                var original=baseline.Card(card.id);Check(original!=null&&original.kind==card.kind,"Card ID and type are fixed: "+card.id);
                Check(!string.IsNullOrWhiteSpace(card.name)&&!string.IsNullOrWhiteSpace(card.rules),"Card name/rules required: "+card.id);
                Check(baseline.cards.Any(x=>x.kind==card.kind&&x.effect==card.effect&&x.target==card.target),"Unsupported effect/target combination: "+card.id);
                Range(card.qte,card.kind=="reaction"?0:2,card.kind=="reaction"?0:20,card.id+".qte");Range(card.value,0,100,card.id+".value");Range(card.attack,0,100,card.id+".attack");Range(card.health,card.kind=="creature"?1:0,100,card.id+".health");
                Check(cards.typeColors.Any(x=>x.id==card.kind)&& (card.kind!="creature"||cards.roleColors.Any(x=>x.name==card.role)),"Missing card color: "+card.id);
                if(assets!=null)Check(assets.Get<Texture2D>(card.art)!=null,"Unknown card art ID: "+card.art);
            }
            foreach(var color in cards.typeColors.Concat(cards.roleColors))Check(ColorUtility.TryParseHtmlString(color.hex,out _),"Invalid color: "+color.hex);
            Check(audio.cues!=null&&audio.cues.Select(x=>x.action).Distinct().Count()==audio.cues.Length,"Audio action IDs must be unique");
            foreach(var cue in audio.cues)
            {
                Check(Enum.IsDefined(typeof(AudioBus),cue.bus)&&cue.sounds!=null,"Invalid audio bus/sounds");
                foreach(var sound in cue.sounds){Range(sound.volume,0,1,cue.action+".volume");Range(sound.pitch,.1f,3,cue.action+".pitch");Range(sound.pitchRange.x,.1f,3,cue.action+".pitchRange");Range(sound.pitchRange.y,sound.pitchRange.x,3,cue.action+".pitchRange");Range(sound.delay,0,10,cue.action+".delay");if(assets!=null&&!string.IsNullOrEmpty(sound.clip))Check(assets.Get<AudioClip>(sound.clip)!=null,"Unknown audio asset: "+sound.clip);}
            }
            foreach(string action in new[]{"card.hover","action.invalid","creature.attack","damage.hit","qte.correct","qte.error","turn.start","ambience"}.Concat(Enumerable.Range(1,8).SelectMany(i=>new[]{"S"+i.ToString("00")+".launch","S"+i.ToString("00")+".impact"})))Check(audio.cues.Any(x=>x.action==action),"Missing audio action: "+action);
            Check(vfx.effects!=null&&vfx.effects.Length==8&&vfx.effects.Select(e=>e.id).Distinct().Count()==8,"Eight unique spell VFX required");
            void Fx(string id){if(assets!=null&&!string.IsNullOrEmpty(id))Check(assets.Get<GameObject>(id)!=null,"Unknown VFX asset: "+id);}
            foreach(var e in vfx.effects){Check(c.Card(e.id)?.kind=="spell","Unknown spell VFX "+e.id);Range(e.motion,0,5,e.id+".motion");Range(e.cardCount,1,8,e.id+".cardCount");foreach(float n in new[]{e.travelScale,e.impactScale,e.persistentScale,e.cardSize.x,e.cardSize.y})Range(n,.001f,100,e.id+".scale");Range(e.travelSeconds,.05f,10,e.id+".travelSeconds");Range(e.lifetime,.05f,15,e.id+".lifetime");Range(e.arc,0,20,e.id+".arc");Fx(e.travel);Fx(e.impact);Fx(e.persistent);}
            foreach(var id in new[]{vfx.attack,vfx.hit,vfx.death,vfx.qteFire,vfx.qteSmoke,vfx.qteAttempt,vfx.motionTitle})Fx(id);
            if(assets!=null&&!string.IsNullOrEmpty(vfx.motionTitle))Check(assets.Get<GameObject>(vfx.motionTitle).GetComponentInChildren<Michsky.UI.MTP.StyleManager>(true)!=null,"motionTitle requires a StyleManager prefab");
            foreach(float n in new[]{vfx.attackScale,vfx.impactScale,vfx.qteScale})Range(n,.001f,100,"vfx.scale");
            presentation.camera.Validate();Range(presentation.cardDepth,0,.35f,"cardDepth");Range(presentation.cardFoil,0,1,"cardFoil");
            ValidateAuthoring();
            Check(prefabs.Select(p=>(string)p["id"]).Distinct().Count()==prefabs.Count,"Duplicate prefab profile");
            foreach(var entry in prefabs)
            {
                Check(entry.Keys.All(k=>k=="id"||k=="components"),"Unknown prefab field");
                foreach(var node in (List<object>)entry["components"])
                {
                    var row=ConfigJson.Object(node);Check(row.Keys.All(k=>k=="path"||k=="type"||k=="values"),"Unknown component field");
                    var type=typeof(PrefabConfigBinding).Assembly.GetType((string)row["type"]);Check(type!=null,"Unknown configurable component");
                    foreach(var value in ConfigJson.Object(row["values"]))
                    {var field=PrefabConfigBinding.Fields(type).FirstOrDefault(f=>f.Name==value.Key);Check(field!=null,"Not a tuning field: "+value.Key);ConfigJson.Validate(field.FieldType,value.Value,"prefabs."+value.Key);if(value.Value is double number)Range((float)number,-10000,10000,value.Key);}
                }
            }
        }
    }
}
