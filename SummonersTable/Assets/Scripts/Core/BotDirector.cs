using System;
using System.Collections.Generic;
using System.Linq;
namespace SummonersTable
{
    // Host-only scheduler. Each brain receives the same privacy-filtered view as a human client.
    public sealed class BotDirector
    {
        sealed class Agent {public int seat,sequence;public string context="";public double due;public Random random;public BotBrain brain;public BotTiming timing;}
        readonly List<Agent> agents=new List<Agent>();
        public int Accepted {get;private set;}public int Rejected {get;private set;}
        public string LastError {get;private set;}="";
        public BotDirector(GameEngine engine,BotsConfig settings,int seed)
        {
            settings.Validate();var profile=settings.Profile(engine.State.options.mode);
            foreach(var player in engine.State.players.Where(p=>p.isBot))
            {var random=new Random(unchecked(seed+player.seat));agents.Add(new Agent{seat=player.seat,random=random,timing=profile.timing,brain=new BotBrain(engine.Catalog,profile,settings.autoRematch,random)});}
        }
        static double Delay(Agent a,string phase,bool qte)
        {
            var t=a.timing;float min=qte?t.qteMin:phase=="qte"||phase=="reveal"?t.reactionMin:t.thinkMin;
            float max=qte?t.qteMax:phase=="qte"||phase=="reveal"?t.reactionMax:t.thinkMax;
            return min+(max-min)*a.random.NextDouble();
        }
        public void Tick(GameEngine engine,double now)
        {
            foreach(var a in agents)
            {
                var view=engine.View(a.seat,now);bool qte=view.phase=="qte"&&view.qte?.owner==a.seat;
                string context=view.turnNumber+":"+view.phase+":"+(view.cast?.id??"");
                if(a.context!=context){a.context=context;a.due=now+Delay(a,view.phase,qte);}
                if(now<a.due)continue;
                var cmd=a.brain.Decide(view,a.seat);a.due=now+Delay(a,view.phase,qte);
                if(cmd==null)continue;cmd.seq=++a.sequence;var result=engine.Submit(a.seat,cmd,now);
                if(result.ok)Accepted++;else{Rejected++;LastError=result.message;a.due=now+a.timing.retrySeconds;}
            }
        }
    }
}
