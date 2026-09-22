using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SummonersTable
{
    public sealed partial class TableBoard
    {
        readonly List<GameObject> orbs=new List<GameObject>();
        readonly List<bool> orbLit=new List<bool>();
        readonly Dictionary<string,double> invalidUntil=new Dictionary<string,double>();
        readonly HashSet<string> observedImpacts=new HashSet<string>();
        readonly Dictionary<string,int> previousHp=new Dictionary<string,int>();
        readonly Dictionary<string,float> displayedHp=new Dictionary<string,float>();
        readonly Dictionary<string,double> hpChangedAt=new Dictionary<string,double>();
        readonly Dictionary<string,GameObject> projectiles=new Dictionary<string,GameObject>();
        readonly Dictionary<string,LineRenderer> outlines=new Dictionary<string,LineRenderer>();
        string orbCast="",eventMatch="",hoveredUnit="";int orbMistakes;
        float errorPulseUntil;
        public float DisplayHp(string id,int actual)
        {return displayedHp.TryGetValue(id,out float hp)?hp:actual;}
        public void Invalid(string uid){invalidUntil[uid]=Time.unscaledTime+.35;}
        public void ClearMatchVisuals()
        {
            foreach(var o in projectiles.Values)if(o!=null)Destroy(o);projectiles.Clear();
            previousHp.Clear();displayedHp.Clear();hpChangedAt.Clear();observedImpacts.Clear();
            ClearSpells();
        }
        void TintUnit(GameObject obj,UnitState unit,int owner,int viewer)
        {
            bool ready=unit.deploying||unit.uid==selectedUnit||ownAction&&owner==viewer&&!unit.exhausted;
            bool target=Highlight(owner,viewer,"unit");
            Color color=target?Color.white:SeatColors[owner];
            if(!outlines.TryGetValue(unit.uid,out var outline)||outline==null)
            {
                outline=obj.GetComponent<CardView>().worldOutline;outlines[unit.uid]=outline;
            }
            outline.enabled=ready||target;outline.startWidth=outline.endWidth=.025f;
            outline.startColor=outline.endColor=color;
            if(invalidUntil.TryGetValue(unit.uid,out double until)&&Time.unscaledTime<until)
                obj.transform.position+=ViewCamera.transform.right*(Mathf.Sin(Time.unscaledTime*70)*.1f);
        }
        void SyncEffects(MatchState state,int viewer,double clock)
        {
            if(eventMatch!=state.matchId){ClearMatchVisuals();eventMatch=state.matchId;}
            SyncOrbs(state);
            SyncSpells(state,clock);
            var hover=inputEnabled?Pick(Input.mousePosition):null;
            string hoverId=hover!=null&&hover.kind=="unit"?hover.uid:"";
            if(hoverId!=""&&hoverId!=hoveredUnit)PlaySound(CameraRig.settings?.cardHover,640*Random.Range(.94f,1.06f),.045f);
            hoveredUnit=hoverId;
            if(units.TryGetValue(hoverId,out var hoverObj))
            {
                hoverObj.transform.localScale=Vector3.one*CameraRig.Data.hoverScale;
                var p=ViewCamera.WorldToScreenPoint(hoverObj.transform.position);float tilt=Mathf.Clamp((p.x-Input.mousePosition.x)/80,-1,1)*CameraRig.Data.hoverTilt;
                hoverObj.transform.Rotate(0,tilt,0,Space.Self);
            }
            var activeProjectiles=new HashSet<string>();
            foreach(var e in state.combatEvents)
            {
                float t=(float)(clock-e.startedAt);if(t<0||t>1.2f)continue;
                Vector3 from=SlotPosition(e.source,e.sourceSlot,count)+Vector3.up*.65f;
                Vector3 to=(e.targetSlot>=0?SlotPosition(e.targetSeat,e.targetSlot,count):HeroPosition(e.targetSeat,count))+Vector3.up*.3f;
                if(t<.4f)
                {
                    activeProjectiles.Add(e.id);
                    if(!projectiles.TryGetValue(e.id,out var projectile))
                    {
                        var prefab=CameraRig.settings?.attackEffect;
                        projectile=prefab!=null?Instantiate(prefab,transform):Sphere("Attack preview",.18f,SeatColors[e.source]);if(prefab!=null)projectile.transform.localScale*=CameraRig.settings.attackEffectScale;projectiles[e.id]=projectile;
                        PlaySound(CameraRig.settings?.attack,420,.075f);
                    }
                    projectile.transform.position=Vector3.Lerp(from,to,t/.4f)+Vector3.up*Mathf.Sin(t/.4f*Mathf.PI)*.45f;
                    if(units.TryGetValue(e.unitUid,out var card))card.transform.position+=(to-from).normalized*(Mathf.Sin(t/.4f*Mathf.PI)*.32f);
                }
                if(t>=.4f&&observedImpacts.Add(e.id))
                {
                    SpawnImpact(to,e.damage);
                    if(e.targetUnit!="")Invalid(e.targetUnit);
                    PlaySound(CameraRig.settings?.hit,190,.09f);
                }
            }
            foreach(var id in projectiles.Keys.Where(id=>!activeProjectiles.Contains(id)).ToList()){Destroy(projectiles[id]);projectiles.Remove(id);}
            foreach(var p in state.players)
            {
                TrackHp("hero-"+p.seat,p.hp,HeroPosition(p.seat,count)+Vector3.up);
                foreach(var u in p.units)TrackHp(u.uid,u.hp,SlotPosition(p.seat,u.slot,count)+Vector3.up);
                if(activeLayout!=null&&Actor(p.seat)==null)
                {
                    var renderer=activeLayout.avatars[p.seat].GetComponentInChildren<Renderer>();
                    var props=new MaterialPropertyBlock();bool flash=hpChangedAt.TryGetValue("hero-"+p.seat,out var changed)&&Time.unscaledTime-changed<.25;
                    props.SetColor("_Color",flash?new Color(1,.35f,.25f):Highlight(p.seat,viewer,"hero")?Color.white:new Color(.5f,.52f,.54f));renderer.SetPropertyBlock(props);
                    if(!p.alive&&previousHp.ContainsKey("hero-"+p.seat)&&observedImpacts.Add("death-"+state.round+"-"+p.seat))SpawnImpact(HeroPosition(p.seat,count),0);
                }
            }
        }
        bool Highlight(int owner,int viewer,string kind)
        {
            if(!choosingTarget)return false;
            switch(targetMode)
            {
                case "enemy":return owner!=viewer;
                case "hero":return kind=="hero";
                case "enemyHero":return kind=="hero"&&owner!=viewer;
                case "unit":return kind=="unit";
                case "enemyUnit":return kind=="unit"&&owner!=viewer;
                default:return false;
            }
        }
        void TrackHp(string id,int hp,Vector3 point)
        {
            if(!previousHp.ContainsKey(id)){previousHp[id]=hp;displayedHp[id]=hp;}
            if(previousHp[id]!=hp){hpChangedAt[id]=Time.unscaledTime;StartCoroutine(Number(point,hp-previousHp[id]));previousHp[id]=hp;}
            if(!hpChangedAt.TryGetValue(id,out var when)||Time.unscaledTime-when>.22)
                displayedHp[id]=Mathf.MoveTowards(displayedHp[id],hp,Time.unscaledDeltaTime*15);
        }
        GameObject Sphere(string name,float size,Color color)
        {
            var o=GameObject.CreatePrimitive(PrimitiveType.Sphere);o.name=name;o.transform.SetParent(transform);o.transform.localScale=Vector3.one*size;Destroy(o.GetComponent<Collider>());o.GetComponent<Renderer>().sharedMaterial=Flat(color);return o;
        }
        void SyncOrbs(MatchState state)
        {
            var cast=state.cast;int length=state.phase=="qte"&&cast!=null?cast.qteLength:0;
            if(length==0){foreach(var orb in orbs)Destroy(orb);orbs.Clear();orbLit.Clear();orbCast="";return;}
            if(orbCast!=cast.id)
            {
                foreach(var orb in orbs)Destroy(orb);orbs.Clear();orbLit.Clear();orbCast=cast.id;orbMistakes=0;
                for(int i=0;i<length+3;i++){orbs.Add(QteOrb(i>=length,i>=length));orbLit.Add(i>=length);}
            }
            if(cast.qteMistakes>orbMistakes){errorPulseUntil=Time.unscaledTime+.45f;orbMistakes=cast.qteMistakes;PlaySound(CameraRig.settings?.qteError,150,.1f);}
            var away=Away(cast.owner,count);var right=Vector3.Cross(Vector3.up,-away);Vector3 start=away*3.1f+Vector3.up*2.75f;
            Color color=cast.cardId.StartsWith("C")?new Color(.25f,.79f,.69f):new Color(.97f,.74f,.36f);
            for(int i=0;i<orbs.Count;i++)
            {
                bool main=i<length,lit=main?i<cast.qteProgress:i-length<3-cast.qteMistakes;
                if(orbLit[i]!=lit){Destroy(orbs[i]);orbs[i]=QteOrb(lit,!main);orbLit[i]=lit;if(main&&lit)PlaySound(CameraRig.settings?.qteSuccess,640,.045f);}
                var orb=orbs[i];orb.transform.position=start+right*(main?(i-(length-1)*.5f)*.36f:(length*.18f+(i-length)*.2f))+Vector3.up*(main?0:.4f);
                bool pulse=main&&i==cast.qteProgress&&Time.unscaledTime<errorPulseUntil;
                bool effects=CameraRig.settings?.qteFire!=null;
                orb.transform.localScale=Vector3.one*(effects?CameraRig.settings.qteEffectScale:main?.22f:.11f)*(main?1:.55f)*(pulse?1.4f+.25f*Mathf.Sin(Time.unscaledTime*35):1);
                Color tint=main?(pulse?Color.red:i<cast.qteProgress?color:new Color(.045f,.05f,.06f)):(i-length<3-cast.qteMistakes?new Color(.3f,1,.35f):new Color(.04f,.08f,.04f));
                foreach(var ps in orb.GetComponentsInChildren<ParticleSystem>()){var module=ps.main;module.startColor=tint;}
                var renderer=orb.GetComponent<Renderer>();if(renderer!=null&&!effects)renderer.sharedMaterial.color=tint;
            }
        }
        GameObject QteOrb(bool lit,bool attempt)
        {
            var prefab=lit?(attempt?CameraRig.settings?.qteAttempt:CameraRig.settings?.qteFire):CameraRig.settings?.qteSmoke;
            return prefab!=null?Instantiate(prefab,transform):Sphere("QTE progress",.22f,Color.black);
        }
        void SpawnImpact(Vector3 point,int damage)
        {
            var prefab=CameraRig.settings?.impactEffect;
            if(prefab!=null){var fx=Instantiate(prefab,point,Quaternion.identity);fx.transform.localScale*=CameraRig.settings.impactEffectScale;Destroy(fx,3);}
            else StartCoroutine(Pulse(point));
        }
        IEnumerator Pulse(Vector3 point)
        {
            var o=Sphere("Impact preview",.1f,new Color(1,.7f,.3f));o.transform.position=point;
            for(float t=0;t<.25f;t+=Time.unscaledDeltaTime){o.transform.localScale=Vector3.one*(.15f+2*t);yield return null;}Destroy(o);
        }
        IEnumerator Number(Vector3 point,int delta)
        {
            var text=Instantiate(numberPrefab,transform);var o=text.gameObject;text.text=(delta>0?"+":"")+delta;text.color=delta>0?Color.green:numberPrefab.color;
            for(float t=0;t<.8f;t+=Time.unscaledDeltaTime){o.transform.position=point+Vector3.up*t;o.transform.rotation=ViewCamera.transform.rotation;yield return null;}Destroy(o);
        }
        IEnumerator Dissolve(GameObject obj)
        {
            if(obj==null)yield break;foreach(var collider in obj.GetComponentsInChildren<Collider>())collider.enabled=false;
            var prefab=CameraRig.settings?.deathEffect;if(prefab!=null){var fx=Instantiate(prefab,obj.transform.position,Quaternion.identity);fx.transform.localScale*=CameraRig.settings.impactEffectScale;Destroy(fx,3);}
            foreach(var line in obj.GetComponentsInChildren<LineRenderer>())line.enabled=false;
            var renderers=obj.GetComponentsInChildren<Renderer>();
            for(float t=0;t<.45f;t+=Time.unscaledDeltaTime)
            {foreach(var r in renderers){var p=new MaterialPropertyBlock();p.SetFloat("_Dissolve",t/.45f);r.SetPropertyBlock(p);}yield return null;}
            Destroy(obj);
        }
        void PlaySound(AudioClip clip,float pitch,float duration)
        {
            var source=GetComponent<AudioSource>();
            if(clip!=null){source.pitch=clip==CameraRig.settings?.cardHover?pitch/640f:1;source.PlayOneShot(clip);return;}
            int length=(int)(22050*duration);var samples=new float[length];for(int i=0;i<length;i++)samples[i]=Mathf.Sin(i*pitch*2*Mathf.PI/22050)*(1-i/(float)length)*.3f;
            clip=AudioClip.Create("Placeholder feedback",length,1,22050,false);clip.SetData(samples,0);source.PlayOneShot(clip);Destroy(clip,duration+1);
        }
    }
}
