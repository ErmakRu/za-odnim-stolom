using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
namespace SummonersTable
{
    public sealed class HeroAnimationSequence:MonoBehaviour
    {
        PlayableGraph graph;AnimationClipPlayable playable;AnimationPlayableOutput output;
        AnimationStateDef definition;Animator animator;int index;float started;bool completed;
        readonly List<GameObject> effects=new List<GameObject>();
        public string StateId=>definition?.id??"";
        public string ClipName {get;private set;}="";
        public bool Busy=>definition!=null&&!completed&&StateId!="idle"&&StateId!="turn";
        public void Play(AnimationStateDef state,Animator target,bool restart)
        {
            if(!restart&&animator==target&&StateId==state.id&&graph.IsValid())return;Stop();definition=ConfigBundle.Clone(state);animator=target;index=0;completed=false;
            graph=PlayableGraph.Create("Configured hero "+state.id);graph.SetTimeUpdateMode(DirectorUpdateMode.UnscaledGameTime);output=AnimationPlayableOutput.Create(graph,"Hero",animator);Step();graph.Play();
        }
        void Step()
        {
            var step=definition.steps[index];var clip=ConfigRuntime.Assets.Get<AnimationClip>(step.clip);if(clip==null){Stop();return;}
            if(playable.IsValid())graph.DestroyPlayable(playable);
            playable=AnimationClipPlayable.Create(graph,clip);playable.SetApplyFootIK(false);playable.SetSpeed(step.speed);output.SetSourcePlayable(playable);ClipName=clip.name;started=Time.unscaledTime;
            var fx=ConfigRuntime.Assets.Get<GameObject>(step.vfx);if(fx!=null){var instance=Instantiate(fx,transform.position+step.effectOffset,Quaternion.identity);instance.transform.localScale*=step.effectScale;effects.Add(instance);Destroy(instance,step.effectLifetime);}
            if(!string.IsNullOrEmpty(step.sound))ConfigAudio.Play(step.sound);
        }
        void Update()
        {
            if(definition==null||!playable.IsValid()||completed)return;
            var step=definition.steps[index];var clip=playable.GetAnimationClip();float time=(Time.unscaledTime-started)*step.speed;
            if(step.loop){playable.SetTime(time%Mathf.Max(.01f,clip.length));return;}
            if(time<clip.length)return;
            if(index+1<definition.steps.Length){index++;Step();}
            else{playable.SetTime(clip.length);playable.SetSpeed(0);completed=true;}
        }
        public void Stop(){if(graph.IsValid())graph.Destroy();definition=null;completed=true;foreach(var item in effects)if(item!=null)Destroy(item);effects.Clear();}
        void OnDisable(){Stop();}
        void OnDestroy(){Stop();}
    }
}
