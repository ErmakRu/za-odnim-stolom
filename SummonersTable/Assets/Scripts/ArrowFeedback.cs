using UnityEngine;
namespace SummonersTable
{
    public sealed class ArrowFeedback:MonoBehaviour
    {
        GameObject drag;bool started;Vector3 last;Camera view;
        ArrowStyle Style=>ConfigRuntime.Current?.ui.arrow;
        public void Move(Vector2 from,Vector2 to)
        {
            if(Style==null)return;if(view==null)view=Object.FindFirstObjectByType<TableBoard>()?.ViewCamera;if(view==null)return;
            last=view.ScreenToWorldPoint(new Vector3(to.x,to.y,5));
            if(!started){started=true;Spawn(Style.beginEffect,view.ScreenToWorldPoint(new Vector3(from.x,from.y,5)));ConfigAudio.Play(Style.beginSound);var prefab=ConfigRuntime.Assets.Get<GameObject>(Style.dragEffect);if(prefab!=null){drag=Instantiate(prefab);drag.transform.localScale*=Style.effectScale;}ConfigAudio.Play(Style.dragSound);}
            if(drag!=null)drag.transform.position=last;
        }
        public void Select(){if(!started||Style==null)return;Spawn(Style.selectEffect,last);ConfigAudio.Play(Style.selectSound);}
        void Spawn(string id,Vector3 position){var prefab=ConfigRuntime.Assets.Get<GameObject>(id);if(prefab==null)return;var item=Instantiate(prefab,position,Quaternion.identity);item.transform.localScale*=Style.effectScale;Destroy(item,Style.effectLifetime);}
        void OnDisable(){if(drag!=null)Destroy(drag);started=false;}
    }
}
