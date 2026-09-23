using System;
using System.Linq;
using UnityEngine;
namespace SummonersTable
{
    public sealed partial class ConfigBundle
    {
        string GameplayJson()
        {
            var data=Catalog();data.world.scale=1;data.world.scene="";
            foreach(var e in data.events.events){e.vfx="";e.sound="";e.vfxScale=1;e.lifetime=1;}
            return JsonUtility.ToJson(data);
        }
        void ValidateAuthoring()
        {
            Check(world!=null&&world.schemaVersion==1&&events!=null&&events.schemaVersion==1&&animations!=null&&animations.schemaVersion==1&&ui!=null&&ui.schemaVersion==1,"Invalid manager schema");
            Check(!string.IsNullOrWhiteSpace(world.name),"world.name required");
            Check(world.playerRange.x>=1&&world.playerRange.y<=8&&world.playerRange.x<=world.playerRange.y,"world.playerRange must be 1..8");
            Range(world.scale,.25f,3,"world.scale");
            foreach(float v in new[]{world.playersTurnTime,world.qte,world.revealTime,world.gameTimer})Check(v==-1||float.IsFinite(v)&&v>=.1f&&v<=86400,"World timer must be -1 or 0.1..86400 seconds");
            Check(events.events!=null&&events.events.Select(e=>e.id).Distinct().Count()==events.events.Length,"Unique event IDs required");
            var assets=Resources.Load<ConfigAssets>("ConfigAssets");
            void Asset(string id,string kind){if(!string.IsNullOrEmpty(id)&&assets!=null)Check(assets.entries.Any(e=>e.id==id&&e.kind==kind),"Unknown "+kind+" asset: "+id);}
            Asset(world.scene,"interior");
            if(!string.IsNullOrEmpty(world.scene)&&assets!=null)Check(assets.Get<GameObject>(world.scene).GetComponentsInChildren<BoardTarget>(true).Any(t=>t.kind=="center"),"Interior requires a center BoardTarget");
            foreach(var e in events.events){Check(Enum.IsDefined(typeof(WorldEventMechanic),e.mechanic)&&!string.IsNullOrWhiteSpace(e.name),"Invalid world event");Range(e.everySymbols,1,50,"event.everySymbols");Range(e.value,0,100,"event.value");Range(e.vfxScale,.001f,100,"event.vfxScale");Range(e.lifetime,.01f,30,"event.lifetime");Asset(e.vfx,"prefab");Check(string.IsNullOrEmpty(e.sound)||audio.cues.Any(c=>c.action==e.sound),"Unknown event audio");}
            Check(world.events!=null&&world.events.Select(e=>e.id).Distinct().Count()==world.events.Length,"Unique location event slots required");
            foreach(var e in world.events){Check(events.events.Any(d=>d.id==e.eventId),"Unknown location event ID");Range(e.chance,0,100,"event.chance");Range(e.everyTurns,1,1000,"event.everyTurns");Range(e.duration,1,1000,"event.duration");Check(e.durationRange.x>=1&&e.durationRange.y>=e.durationRange.x&&e.durationRange.y<=1000,"Invalid event duration range");}
            Check(animations.states!=null&&animations.states.Select(s=>s.id).Distinct().Count()==animations.states.Length,"Unique animation states required");
            foreach(string id in new[]{"idle","turn","attack","hit","dead","revive"})Check(animations.states.Any(s=>s.id==id),"Missing animation state "+id);
            foreach(var state in animations.states)
            {
                Check(state.steps!=null&&state.steps.Length>0,"Empty animation state "+state.id);
                for(int i=0;i<state.steps.Length;i++){var step=state.steps[i];Asset(step.clip,"animation");Check(!string.IsNullOrEmpty(step.clip),"Animation clip required");Range(step.speed,.05f,4,"animation.speed");Check(!step.loop||i==state.steps.Length-1,"Only final animation step may loop");Asset(step.vfx,"prefab");Check(string.IsNullOrEmpty(step.sound)||audio.cues.Any(c=>c.action==step.sound),"Unknown animation audio");Range(step.effectScale,.001f,100,"animation.effectScale");Range(step.effectLifetime,.01f,30,"animation.effectLifetime");}
            }
            Range(ui.fan.radius,.1f,10,"fan.radius");Range(ui.fan.spread,0,180,"fan.spread");Range(ui.fan.maximumStep,1,90,"fan.maximumStep");Range(ui.fan.uiUnitsPerMetre,10,600,"fan.uiUnitsPerMetre");Range(ui.fan.uiPerspective,.05f,1,"fan.uiPerspective");
            foreach(float v in new[]{ui.fan.cardSize.x,ui.fan.cardSize.y,ui.healthSize.x,ui.healthSize.y})Range(v,1,1000,"UI size");
            foreach(float v in new[]{ui.fan.worldCardSize.x,ui.fan.worldCardSize.y})Range(v,.01f,10,"world card size");
            Asset(ui.highlightMaterial,"material");Asset(ui.arrow.uiMaterial,"material");Asset(ui.arrow.worldMaterial,"material");
            foreach(string id in new[]{ui.arrow.beginEffect,ui.arrow.dragEffect,ui.arrow.selectEffect})Asset(id,"prefab");
            foreach(string id in new[]{ui.arrow.beginSound,ui.arrow.dragSound,ui.arrow.selectSound})Check(string.IsNullOrEmpty(id)||audio.cues.Any(c=>c.action==id),"Unknown arrow audio");
            foreach(float v in new[]{ui.arrow.width,ui.arrow.headLength,ui.arrow.headWidth,ui.arrow.worldWidth,ui.arrow.worldHeadLength,ui.arrow.worldHeadWidth,ui.arrow.effectScale,ui.arrow.effectLifetime})Range(v,.001f,100,"arrow size/time");
        }
    }
}
