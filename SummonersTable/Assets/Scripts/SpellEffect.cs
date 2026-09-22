using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SummonersTable
{
    // All rendered parts are referenced prefabs. This component only moves their instances.
    public sealed class SpellEffect : MonoBehaviour
    {
        public enum Motion { Projectile, Area, Exchange, Draw, Return, Boost }
        public Motion motion;
        public GameObject travelPrefab,impactPrefab,cardBackPrefab,persistentPrefab;
        public AudioClip launchSound,impactSound;
        public AudioSource sound;
        public float travelScale=.22f,impactScale=.35f,persistentScale=.2f,travelSeconds=.55f,lifetime=1.8f,arc=.65f;
        public int cardCount=2;
        public Vector2 cardSize=new Vector2(.65f,.85f);
        public void Play(Vector3 source,IList<Vector3> targets,Camera camera)
        {StartCoroutine(Run(source,targets,camera));}
        public GameObject Persistent(Vector3 position,Transform parent)
        {if(persistentPrefab==null)return null;var fx=Instantiate(persistentPrefab,position,Quaternion.identity,parent);fx.transform.localScale*=persistentScale;return fx;}
        IEnumerator Run(Vector3 source,IList<Vector3> targets,Camera camera)
        {
            if(sound!=null&&launchSound!=null)sound.PlayOneShot(launchSound);
            if(motion==Motion.Area||motion==Motion.Boost)
            {
                foreach(var target in targets)Impact(target);
                if(sound!=null&&impactSound!=null)sound.PlayOneShot(impactSound);
            }
            else
            {
                var movers=new List<GameObject>();var starts=new List<Vector3>();var ends=new List<Vector3>();
                foreach(var target in targets)
                {
                    if(motion==Motion.Exchange){Mover(source,target,true,movers,starts,ends,camera);Mover(target,source,true,movers,starts,ends,camera);}
                    else if(motion==Motion.Draw){for(int i=0;i<cardCount;i++)Mover(new Vector3(i*.35f,TableBoard.TableTop+.4f,0),source+camera.transform.right*(i*.4f),true,movers,starts,ends,camera);}
                    else Mover(source,target,motion==Motion.Return,movers,starts,ends,camera);
                }
                for(float t=0;t<travelSeconds;t+=Time.unscaledDeltaTime)
                {float u=Mathf.Clamp01(t/Mathf.Max(.01f,travelSeconds));for(int i=0;i<movers.Count;i++)movers[i].transform.position=Vector3.Lerp(starts[i],ends[i],u)+Vector3.up*Mathf.Sin(u*Mathf.PI)*arc;yield return null;}
                for(int i=0;i<movers.Count;i++){Impact(ends[i]);Destroy(movers[i]);}
                if(sound!=null&&impactSound!=null)sound.PlayOneShot(impactSound);
            }
            yield return new WaitForSecondsRealtime(lifetime);Destroy(gameObject);
        }
        void Mover(Vector3 from,Vector3 to,bool card,List<GameObject> movers,List<Vector3> starts,List<Vector3> ends,Camera camera)
        {
            var prefab=card?cardBackPrefab:travelPrefab;if(prefab==null)return;
            var obj=Instantiate(prefab,from,card?camera.transform.rotation:Quaternion.identity,transform);
            if(card){obj.transform.localScale=new Vector3(cardSize.x,cardSize.y,1);if(travelPrefab!=null){var trail=Instantiate(travelPrefab,obj.transform,false);trail.transform.localScale=Vector3.one*travelScale;}}
            else obj.transform.localScale*=travelScale;
            movers.Add(obj);starts.Add(from);ends.Add(to);
        }
        void Impact(Vector3 position)
        {if(impactPrefab==null)return;var fx=Instantiate(impactPrefab,position,Quaternion.identity,transform);fx.transform.localScale*=impactScale;}
    }
}
