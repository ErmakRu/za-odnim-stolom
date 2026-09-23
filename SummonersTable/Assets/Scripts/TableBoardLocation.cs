using UnityEngine;
namespace SummonersTable
{
    public sealed partial class TableBoard
    {
        public string interiorId="";
        int worldNoticeSerial;
        public void ApplyInterior(string id)
        {
            if(id==interiorId)return;var prefab=ConfigRuntime.Assets.Get<GameObject>(id);if(prefab==null)return;
            var next=Instantiate(prefab,transform);next.name=prefab.name;
            var target=System.Array.Find(next.GetComponentsInChildren<BoardTarget>(true),t=>t.kind=="center");if(target==null){Remove(next);throw new System.InvalidOperationException("Интерьер должен содержать цель центра стола.");}
            if(authoredEnvironment!=null)Remove(authoredEnvironment.gameObject);
            authoredEnvironment=next.transform;environment=next;centerMarker=target.gameObject;keyLight=next.GetComponentInChildren<Light>(true);interiorId=id;
        }
        static void Remove(GameObject item){if(Application.isPlaying){item.SetActive(false);Destroy(item);}else DestroyImmediate(item);}
        void SyncWorldEvents(MatchState state)
        {
            if(state.worldNotices.Count==0){worldNoticeSerial=0;return;}
            foreach(var notice in state.worldNotices)
            {
                if(notice.serial<=worldNoticeSerial)continue;worldNoticeSerial=notice.serial;
                var definition=System.Array.Find((ConfigRuntime.Current?.events??catalog.events).events,e=>e.id==notice.eventId);if(definition==null)continue;
                var prefab=ConfigRuntime.Assets.Get<GameObject>(definition.vfx);
                var position=notice.slot>=0?SlotPosition(notice.seat,notice.slot,count):HeroPosition(notice.seat,count);
                if(prefab!=null){var instance=Instantiate(prefab,position+Vector3.up*.2f,Quaternion.identity);instance.transform.localScale*=definition.vfxScale;Destroy(instance,definition.lifetime);}
                ConfigAudio.Play(definition.sound);
            }
        }
    }
}
