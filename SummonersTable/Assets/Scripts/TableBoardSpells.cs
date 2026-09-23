using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace SummonersTable
{
    public sealed partial class TableBoard
    {
        int lastSpellEvent;
        readonly Dictionary<string,GameObject> stunned=new Dictionary<string,GameObject>();
        readonly List<SpellEffect> spellInstances=new List<SpellEffect>();
        void ClearSpells()
        {lastSpellEvent=0;foreach(var fx in spellInstances)if(fx!=null)Destroy(fx.gameObject);spellInstances.Clear();foreach(var fx in stunned.Values)if(fx!=null)Destroy(fx);stunned.Clear();}
        void SyncSpells(MatchState state,double clock)
        {
            foreach(var e in state.history.Where(e=>e.id>lastSpellEvent).OrderBy(e=>e.id))
            {
                lastSpellEvent=e.id;
                if(e.kind!="effect"||catalog.Card(e.cardId)?.kind!="spell"||clock-e.at>2)continue;
                var targets=e.targets.Where(t=>!t.prevented&&t.seat>=0).ToList();if(targets.Count==0)continue;
                var from=HeroPosition(e.actor,count)+Vector3.up*.25f;
                var points=targets.Select(t=>t.slot>=0?SlotPosition(t.seat,t.slot,count)+Vector3.up*.25f:HeroPosition(t.seat,count)+Vector3.up*.25f).ToList();
                if(e.cardId=="S08"){from=points[0];points=new List<Vector3>{HeroPosition(targets[0].seat,count)+Vector3.up*.65f};}
                PlaySpell(e.cardId,from,points);
            }
            spellInstances.RemoveAll(f=>f==null);
            var active=new HashSet<string>();var stun=cardLibrary.Find("S04").spellEffect;
            foreach(var p in state.players)foreach(var u in p.units.Where(u=>u.skipAttacks>0))
            {active.Add(u.uid);if(!stunned.ContainsKey(u.uid))stunned[u.uid]=ConfiguredPersistent(stun,SlotPosition(p.seat,u.slot,count)+Vector3.up*.4f,transform);}
            foreach(var id in stunned.Keys.Where(id=>!active.Contains(id)).ToList()){Destroy(stunned[id]);stunned.Remove(id);}
        }
        GameObject ConfiguredPersistent(SpellEffect prefab,Vector3 position,Transform parent)
        {var settings=ConfigRuntime.Current?.vfx.effects.FirstOrDefault(e=>e.id=="S04");if(settings==null)return prefab.Persistent(position,parent);var source=ConfigRuntime.Assets.Get<GameObject>(settings.persistent);if(source==null)return null;var obj=Instantiate(source,position,Quaternion.identity,parent);obj.transform.localScale*=settings.persistentScale;return obj;}
        void PlaySpell(string id,Vector3 from,List<Vector3> targets)
        {var prefab=cardLibrary.Find(id)?.spellEffect;if(prefab==null)return;var fx=Instantiate(prefab,transform);ConfigRuntime.ConfigureSpell(fx,id);spellInstances.Add(fx);fx.Play(from,targets,ViewCamera);}
        public void PreviewSpell(string id)
        {if(count<2)return;var from=HeroPosition(0,count);var to=id=="S02"||id=="S06"||id=="S07"?HeroPosition(0,count):id=="S04"||id=="S08"?SlotPosition(1,2,count)+Vector3.up*.4f:HeroPosition(1,count);
            var targets=id=="S03"?Enumerable.Range(1,count-1).Select(s=>HeroPosition(s,count)).ToList():new List<Vector3>{to};
            if(id=="S08"){from=to;targets=new List<Vector3>{HeroPosition(1,count)};}PlaySpell(id,from,targets);}
    }
}
