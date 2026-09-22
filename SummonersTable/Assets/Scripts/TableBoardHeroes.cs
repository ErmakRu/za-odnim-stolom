using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace SummonersTable
{
    public sealed partial class TableBoard
    {
        string heroMatch="";int heroRound;
        readonly Dictionary<int,int> heroHp=new Dictionary<int,int>(),heroModes=new Dictionary<int,int>();
        readonly Dictionary<int,bool> heroAlive=new Dictionary<int,bool>();
        readonly HashSet<string> heroEvents=new HashSet<string>();
        public HeroActor Actor(int seat){return activeLayout==null?null:activeLayout.avatars[seat].GetComponent<HeroActor>();}
        void SyncHeroes(MatchState state,int viewer)
        {
            if(activeLayout==null)return;
            if(heroMatch!=state.matchId){heroMatch=state.matchId;heroHp.Clear();heroAlive.Clear();heroModes.Clear();heroEvents.Clear();heroRound=state.round;}
            if(heroRound!=state.round){heroEvents.Clear();heroRound=state.round;}
            foreach(var p in state.players)
            {
                var actor=Actor(p.seat);if(actor==null)continue;
                actor.Configure(p.heroId,p.outfit,p.palette,true);
                actor.Highlighted=Highlight(p.seat,viewer,"hero");
                actor.SetLook(p.seat==viewer?CameraRig.Look:new Vector2(p.lookYaw,p.lookPitch));
                int mode=p.seat==viewer?CameraRig.Mode:p.cameraMode;
                if(heroModes.TryGetValue(p.seat,out int oldMode)&&oldMode!=mode&&p.alive)actor.Play(mode<oldMode?"DashForward":"DashBackward",.67f);
                heroModes[p.seat]=mode;
                if(heroAlive.TryGetValue(p.seat,out bool wasAlive))
                {
                    if(wasAlive&&!p.alive){actor.Die();SpawnDeath(HeroPosition(p.seat,count));}
                    else if(!wasAlive&&p.alive)actor.Revive();
                    else if(p.alive&&heroHp.TryGetValue(p.seat,out int oldHp)&&p.hp<oldHp)actor.Hit();
                }
                heroAlive[p.seat]=p.alive;heroHp[p.seat]=p.hp;
                if((state.phase=="roundEnd"||state.phase=="matchEnd")&&state.winners.Contains(p.seat))
                {if(heroEvents.Add("win-"+p.seat+"-"+state.round))actor.Play("AttackCombo04",100000);}
                else if(p.alive)actor.Idle(state.activeSeat==p.seat);
                bool hindered=p.units.Any(u=>u.skipAttacks>0)||state.cast?.owner==p.seat&&state.players.Where(e=>e.seat!=p.seat).Any(e=>e.units.Any(u=>!u.deploying&&(catalog.Card(u.cardId).effect=="qteExtra"||catalog.Card(u.cardId).effect=="timeTax")));
                if(hindered&&heroEvents.Add("dizzy-"+p.seat+"-"+state.turnNumber))actor.Play("Dizzy",1.67f);
            }
            foreach(var r in state.tableReactions.Where(r=>r.successful))
                if(heroEvents.Add("reaction-"+r.uid))Actor(r.owner)?.Play("Sliding",.67f);
        }
        void SpawnDeath(Vector3 position)
        {
            var prefab=CameraRig.settings?.deathEffect;if(prefab==null){SpawnImpact(position,0);return;}
            var effect=Instantiate(prefab,position,Quaternion.identity);effect.transform.localScale*=CameraRig.settings.impactEffectScale;Destroy(effect,4);
        }
    }
}
