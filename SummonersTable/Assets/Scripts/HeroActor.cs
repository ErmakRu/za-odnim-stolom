using System.Linq;
using UnityEngine;

namespace SummonersTable
{
    public sealed class HeroActor : MonoBehaviour
    {
        public HeroLibrary library;
        public bool seated;
        public float seatedHipHeight=.82f,seatedThighLength,seatedFootHeight=.12f;
        public bool Highlighted;
        public string HeroId {get;private set;}="";
        public string AnimationName {get;private set;}="";
        public Animator Animator {get;private set;}
        public Transform Head {get;private set;}
        Vector3 namePosition;bool posed;float crownAboveHead=.32f;
        public Vector3 NamePosition=>posed?namePosition:(Head!=null?Head.position:transform.position)+Vector3.up*(crownAboveHead*transform.lossyScale.y+.2f);
        GameObject model;Transform hips,leftThigh,rightThigh,leftCalf,rightCalf,leftFoot,rightFoot;
        int outfit=-1,palette=-1;string idle="Idle_Normal",queued="";
        HeroAnimationSequence sequence;
        float actionUntil,flashUntil;Vector2 look;
        Renderer[] renderers;
        static readonly Color[] colors={new Color(.74f,.22f,.13f),new Color(.19f,.48f,.23f),new Color(.16f,.38f,.71f),new Color(.67f,.38f,.14f)};
        public void Configure(string id,int costume,int tint,bool sit)
        {
            if(library==null)library=Resources.Load<HeroLibrary>("HeroLibrary");if(library==null)return;
            seated=sit;id=HeroOptions.Normalize(id);
            if(HeroId!=id||model==null)
            {
                if(model!=null){if(Application.isPlaying)Destroy(model);else DestroyImmediate(model);}HeroId=id;var definition=library.Find(id);crownAboveHead=definition.crownAboveHead;posed=false;model=Instantiate(definition.prefab,transform);model.name="Hero "+id;
                model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;model.transform.localScale=Vector3.one;
                foreach(var t in model.GetComponentsInChildren<Transform>(true))
                {t.gameObject.layer=gameObject.layer;if(t.name=="Weapon"||t.name=="Shield")t.gameObject.SetActive(false);}
                Animator=model.GetComponentInChildren<Animator>();Animator.runtimeAnimatorController=library.controller;Animator.applyRootMotion=false;Animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                Head=Animator.GetBoneTransform(HumanBodyBones.Head);leftThigh=Animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);rightThigh=Animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);leftCalf=Animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);rightCalf=Animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
                hips=Animator.GetBoneTransform(HumanBodyBones.Hips);leftFoot=Animator.GetBoneTransform(HumanBodyBones.LeftFoot);rightFoot=Animator.GetBoneTransform(HumanBodyBones.RightFoot);
                renderers=model.GetComponentsInChildren<Renderer>(true);
                // Preserve the animals' natural fur colors; only their armor is tinted.
                if(library.naturalMaterial!=null)foreach(var r in renderers.Where(r=>!r.name.StartsWith("Body")))r.sharedMaterial=library.naturalMaterial;
                model.AddComponent<ShaderStyleTarget>().Configure(renderers);
                outfit=palette=-1;AnimationName="";Play("Idle_Normal",0);
            }
            if(outfit!=costume||palette!=tint)
            {
                outfit=HeroOptions.Outfit(costume);palette=HeroOptions.Palette(tint);
                foreach(var t in model.GetComponentsInChildren<Transform>(true))
                    if(t.name.StartsWith("Body")&&int.TryParse(t.name.Substring(4),out int number))t.gameObject.SetActive(number==outfit+1);
                Paint(false);
            }
        }
        public void SetLook(Vector2 direction){look=direction;}
        public void SetVisible(bool visible){if(renderers!=null)foreach(var r in renderers)r.enabled=visible;}
        public void Idle(bool active){idle=active?"Defend":"Idle_Normal";if(sequence!=null&&sequence.Busy)return;if(Time.unscaledTime>=actionUntil&&queued=="")Play(idle,0);}
        public void Play(string name,float seconds=1,string after="")
        {
            if(Animator==null)return;
            if(PlayConfigured(name,seconds))return;
            if(AnimationName!=name||seconds>0){Animator.speed=1;Animator.CrossFadeInFixedTime(name,.13f);AnimationName=name;}
            if(seconds>0){actionUntil=Time.unscaledTime+seconds;queued=after;}
        }
        public void Die(){if(!PlayConfigured("Die",0))Play("Die",1.17f,"AttackCombo05");}
        public void Hit(){flashUntil=Time.unscaledTime+.24f;Play("GetHit",.84f);}
        public void Revive(){queued="";actionUntil=0;Play("DieRecover",1.17f);}
        void Paint(bool flash)
        {
            if(renderers==null)return;
            foreach(var r in renderers)
            {
                var block=new MaterialPropertyBlock();var c=flash?new Color(1,.65f,.4f):Highlighted?Color.white:colors[Mathf.Max(0,palette)];
                block.SetColor("_Color01",c);block.SetColor("_Color02",c*.72f);block.SetColor("_Color03",c);block.SetColor("_Color04",c*.8f);block.SetColor("_Color",flash?new Color(1,.65f,.4f):Color.white);r.SetPropertyBlock(block);
            }
        }
        void Update()
        {
            if(Animator==null)return;
            if(sequence!=null&&sequence.StateId!="")AnimationName=sequence.ClipName;
            if(Time.unscaledTime>=actionUntil&&queued!="")
            {string next=queued;queued="";Play(next,next=="AttackCombo05"?100000:1);}
            Paint(Time.unscaledTime<flashUntil);
        }
        void LateUpdate()
        {
            if(Animator==null)return;
            // The pack has no seated idle. Keep its upper-body animation and pose the
            // humanoid legs onto the actual chair without moving the gameplay anchor.
            if(seated&&(AnimationName=="Idle_Normal"||AnimationName=="Defend"))
            {
                if(hips!=null)hips.position=new Vector3(hips.position.x,transform.position.y+seatedHipHeight*transform.parent.lossyScale.y,hips.position.z);
                PoseLeg(leftThigh,leftCalf,leftFoot);PoseLeg(rightThigh,rightCalf,rightFoot);
            }
            // Apply an offset to the animated bone; its authored forward axis is
            // different from the model's and must not be replaced by world Euler angles.
            if(Head!=null)Head.rotation=Quaternion.AngleAxis(Mathf.Clamp(look.x,-90,90),model.transform.up)*Quaternion.AngleAxis(Mathf.Clamp(look.y,-90,45),model.transform.right)*Head.rotation;
            if(Head!=null){namePosition=Head.position+Vector3.up*(crownAboveHead*transform.lossyScale.y+.2f);posed=true;}
        }
        bool PlayConfigured(string clip,float seconds)
        {
            if(!Application.isPlaying||ConfigRuntime.Current?.animations==null)return false;
            string id=clip switch {"Idle_Normal"=>"idle","Defend"=>"turn","AttackCombo04"=>seconds>100?"victory":"attack","GetHit"=>"hit","Die"=>"dead","DieRecover"=>"revive","DashForward"=>"cameraForward","DashBackward"=>"cameraBack","Dizzy"=>"dizzy","Sliding"=>"reaction",_=>""};
            var definition=System.Array.Find(ConfigRuntime.Current.animations.states,s=>s.id==id);if(definition==null)return false;
            if(sequence==null)sequence=gameObject.AddComponent<HeroAnimationSequence>();sequence.Play(definition,Animator,seconds>0);AnimationName=sequence.ClipName;queued="";
            actionUntil=seconds>0?Time.unscaledTime+seconds:Time.unscaledTime;return true;
        }
        public void EditorPose(){LateUpdate();}
        void PoseLeg(Transform thigh,Transform calf,Transform foot)
        {
            if(thigh==null||calf==null||foot==null)return;
            thigh.rotation=Quaternion.FromToRotation(calf.position-thigh.position,model.transform.forward)*thigh.rotation;
            if(seatedThighLength>0)calf.position=thigh.position+model.transform.forward*seatedThighLength*transform.parent.lossyScale.y;
            calf.rotation=Quaternion.FromToRotation(foot.position-calf.position,Vector3.down)*calf.rotation;
            if(seatedThighLength>0)foot.position=new Vector3(calf.position.x,transform.position.y+seatedFootHeight*transform.parent.lossyScale.y,calf.position.z);
        }
    }
}
