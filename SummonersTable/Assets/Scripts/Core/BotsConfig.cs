using System;
using System.Linq;
using System.Collections.Generic;
namespace SummonersTable
{
    [Serializable] public sealed class BotNode
    {
        public string id, type, operation="";
        public string[] children=Array.Empty<string>();
    }
    [Serializable] public sealed class BotEffectWeight { public string effect; public float weight; }
    [Serializable] public sealed class BotTiming
    {
        public float thinkMin,thinkMax,reactionMin,reactionMax,qteMin,qteMax,qteCorrectChance,retrySeconds;
    }
    [Serializable] public sealed class BotWeights
    {
        public float creatureBase,attack,health,permanentEffect,spellMultiplier,damage,heal,draw,stun,bounce,swap,risk;
        public float killHero,killUnit,heroPressure,unitThreat,healthUrgency,thornsPenalty,costPenalty,budgetEfficiency,followupWeight,randomJitter,minPlayScore;
        public float reactionCopy,reactionReturn,reactionDefense,minReactionScore,randomTargetChance;
    }
    [Serializable] public sealed class BotProfile
    {
        public string mode,root;
        public BotTiming timing;
        public BotWeights weights;
        public BotEffectWeight[] effects;
        public BotNode[] nodes;
    }
    [Serializable] public sealed class BotsConfig
    {
        public int schemaVersion=1;
        public int[] defaultLocalBotSeats;
        public bool autoRematch;
        public BotProfile[] profiles;
        public BotProfile Profile(string mode)=>profiles.Single(p=>p.mode==mode);
        public void Validate()
        {
            void Check(bool ok,string message){if(!ok)throw new FormatException("bots.json: "+message);}
            Check(schemaVersion==1,"schemaVersion must be 1");
            Check(defaultLocalBotSeats!=null&&defaultLocalBotSeats.Distinct().Count()==defaultLocalBotSeats.Length&&defaultLocalBotSeats.All(s=>s>=1&&s<=3),"defaultLocalBotSeats: unique seats 1..3; seat 0 is human");
            Check(profiles!=null&&profiles.Length==2&&profiles.Select(p=>p.mode).OrderBy(x=>x).SequenceEqual(new[]{MatchOptions.Commanders,MatchOptions.Wizards}),"one profile per game mode required");
            foreach(var p in profiles)
            {
                Check(p.timing!=null&&p.weights!=null&&p.effects!=null&&p.nodes!=null,"missing profile sections");
                foreach(var field in typeof(BotTiming).GetFields()) {float value=(float)field.GetValue(p.timing);Check(float.IsFinite(value)&&value>=0&&value<=60,"timing range: "+field.Name);}
                var t=p.timing;Check(t.thinkMin>0&&t.reactionMin>0&&t.qteMin>0&&t.retrySeconds>0&&t.thinkMax>=t.thinkMin&&t.reactionMax>=t.reactionMin&&t.qteMax>=t.qteMin&&t.qteCorrectChance<=1,"invalid timing range/chance");
                foreach(var field in typeof(BotWeights).GetFields()){float value=(float)field.GetValue(p.weights);Check(float.IsFinite(value)&&value>=0&&value<=1000,"weight range: "+field.Name);}
                Check(p.weights.randomTargetChance<=1,"randomTargetChance must be 0..1");
                Check(p.effects.All(e=>e!=null&&!string.IsNullOrEmpty(e.effect)&&float.IsFinite(e.weight)&&e.weight>=0&&e.weight<=1000)&&p.effects.Select(e=>e.effect).Distinct().Count()==p.effects.Length,"invalid/duplicate effect weights");
                Check(p.nodes.Length>0&&p.nodes.Length<=128&&p.nodes.All(n=>n!=null&&!string.IsNullOrWhiteSpace(n.id))&&p.nodes.Select(n=>n.id).Distinct().Count()==p.nodes.Length,"unique node IDs required, max 128");
                var nodes=p.nodes.ToDictionary(n=>n.id);Check(p.root!=null&&nodes.ContainsKey(p.root),"missing root");
                var active=new HashSet<string>();var visited=new HashSet<string>();
                void Visit(string id)
                {
                    Check(nodes.ContainsKey(id),"unknown child "+id);Check(!active.Contains(id),"cycle at "+id);if(visited.Contains(id))return;
                    active.Add(id);var n=nodes[id];Check(n.children!=null,"children array required");
                    bool composite=n.type=="selector"||n.type=="sequence";
                    Check(composite||n.type=="condition"||n.type=="action","unknown node type "+n.type);
                    if(composite){Check(n.children.Length>0&&string.IsNullOrEmpty(n.operation),"composite needs children and empty operation");foreach(string child in n.children)Visit(child);}
                    else {Check(n.children.Length==0,"leaf cannot have children");Check(n.type=="condition"?BotBrain.Conditions.Contains(n.operation):BotBrain.Actions.Contains(n.operation),"unknown operation "+n.operation);}
                    active.Remove(id);visited.Add(id);
                }
                Visit(p.root);Check(visited.Count==nodes.Count,"unreachable nodes");
                Check(p.nodes.Any(n=>n.type=="action"&&n.operation=="endTurn"),"endTurn fallback required");
            }
        }
    }
}
