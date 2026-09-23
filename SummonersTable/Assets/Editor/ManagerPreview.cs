using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace SummonersTable.Editor
{
    [InitializeOnLoad] public static class ManagerPreview
    {
        sealed class Effect
        {
            public string id;public Vector3 from,to;public float scale,start,life,travel,arc;public GameObject instance,prefab;public Vector2 cardSize;
        }
        sealed class Sound {public SoundVariant variant;public AudioBus bus;public float start;public bool played,loop;}
        sealed class Session
        {
            public AuthoringManager manager;public GameObject root,hero;public double started;
            public List<Effect> effects=new List<Effect>();public List<Sound> sounds=new List<Sound>();
            public AnimationStateDef sequence;public int step=-1;public float stepAt;
            public List<AudioClip> clips=new List<AudioClip>();
        }
        static Session current;static readonly HashSet<AuthoringManager> pending=new HashSet<AuthoringManager>();
        public static bool IsRunning=>current!=null;
        public static int OwnedObjects=>Resources.FindObjectsOfTypeAll<PreviewOwned>().Count(p=>p.gameObject.scene.IsValid());
        static ManagerPreview()
        {
            AuthoringManager.Edited+=m=>pending.Add(m);EditorApplication.update+=Update;
            AssemblyReloadEvents.beforeAssemblyReload+=StopAll;
            EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.ExitingEditMode||s==PlayModeStateChange.ExitingPlayMode)StopAll();};
            EditorSceneManager.sceneClosing+=(s,remove)=>StopAll();
            PrefabStage.prefabStageClosing+=s=>StopAll();
            SceneView.duringSceneGui+=view=>{if(!Application.isPlaying)foreach(var status in Resources.FindObjectsOfTypeAll<PlayerStatusView>())if(status.seatOwned&&status.gameObject.scene.IsValid()&&status.GetComponentInParent<SeatingLayout>()!=null)status.PreviewPose(view.camera);};
        }
        public static void Refresh(AuthoringManager manager)
        {
            if(manager==null||Application.isPlaying)return;
            if(manager.section==ManagerSection.World&&manager.table!=null&&manager.world!=null)
            {
                ConfigRuntime.ApplyWorld(manager.table,manager.world);
                var layout=manager.table.seating;
                if(layout==null)return;
                layout.previewCount=manager.previewPlayers;layout.bodyScale=manager.world.scale;layout.Build(manager.previewPlayers,manager.world.scale);
                foreach(var seat in layout.Seats)
                {
                    seat.avatar.Configure(manager.previewHero,0,0,true);
                    seat.avatar.Animator?.Rebind();seat.avatar.Animator?.Update(0);seat.avatar.EditorPose();
                    if(seat.status!=null){seat.status.nickname.text="Игрок "+(Array.IndexOf(layout.Seats,seat)+1);seat.status.healthNumber.text="30 / 30";seat.status.healthFill.fillAmount=1;seat.status.PreviewPose(manager.table.tableCamera);}
                }
                SceneView.RepaintAll();
            }
            else if(manager.section==ManagerSection.Interface&&manager.table!=null)
            {
                foreach(var seat in manager.table.seating.Seats)if(seat.status!=null){seat.status.healthAnchor.sizeDelta=manager.ui.healthSize;seat.status.healthFill.color=manager.ui.healthColor;seat.status.healthTrail.color=manager.ui.healthTrailColor;}
                SceneView.RepaintAll();
            }
        }
        public static void Play(AuthoringManager manager)
        {
            if(Application.isPlaying)throw new InvalidOperationException("Этот предпросмотр работает вне Play. Остановите игру; сохранённые настройки можно применить кнопкой ниже.");
            StopAll();Refresh(manager);
            if(manager.section==ManagerSection.World){Frame(manager.table.transform.position+Vector3.up*5,22);return;}
            current=new Session{manager=manager,started=EditorApplication.timeSinceStartup,root=new GameObject("Manager Preview — временные объекты")};
            current.root.AddComponent<PreviewOwned>();current.root.hideFlags=HideFlags.DontSaveInEditor|HideFlags.DontSaveInBuild;
            if(manager.gameObject.scene.IsValid())SceneManager.MoveGameObjectToScene(current.root,manager.gameObject.scene);
            Vector3 origin=manager.previewAnchor!=null?manager.previewAnchor.position:manager.transform.position+Vector3.up*2;
            if(manager.section==ManagerSection.Cards||manager.section==ManagerSection.Interface)origin+=Vector3.up*4;
            current.root.transform.position=origin;
            switch(manager.section)
            {
                case ManagerSection.Audio:Audio(manager.audio.cues[Mathf.Clamp(manager.previewIndex,0,manager.audio.cues.Length-1)],0);break;
                case ManagerSection.Vfx:
                    PreviewSpell(manager,origin);break;
                case ManagerSection.Events:
                    var e=manager.events.events[Mathf.Clamp(manager.previewIndex,0,manager.events.events.Length-1)];AddEffect(e.vfx,origin,origin,e.vfxScale,0,e.lifetime);Audio(e.sound,0);break;
                case ManagerSection.PlayerAnimations:
                    var definition=Resources.Load<HeroLibrary>("HeroLibrary").Find(manager.previewHero);current.hero=(GameObject)PrefabUtility.InstantiatePrefab(definition.prefab,current.root.transform);
                    current.hero.transform.localPosition=Vector3.zero;current.hero.transform.localRotation=Quaternion.identity;
                    foreach(var t in current.hero.GetComponentsInChildren<Transform>(true))if(t.name=="Weapon"||t.name=="Shield")t.gameObject.SetActive(false);
                    current.sequence=ConfigBundle.Clone(manager.playeranimations.states[Mathf.Clamp(manager.previewIndex,0,manager.playeranimations.states.Length-1)]);
                    AnimationMode.StartAnimationMode();NextStep(0);break;
                case ManagerSection.Cards:ShowCard(manager.cards.cards[Mathf.Clamp(manager.previewIndex,0,manager.cards.cards.Length-1)],origin);break;
                case ManagerSection.Interface:ShowInterface(manager.ui,origin);break;
                case ManagerSection.Presentation:
                    if(manager.table!=null){manager.table.PreviewCamera(ConfigBundle.Clone(manager.presentation.camera),0,manager.table.seating.previewCount);Frame(manager.table.tableCamera.transform.position+manager.table.tableCamera.transform.forward*10,12);}
                    break;
            }
            if(manager.section!=ManagerSection.Presentation)Frame(origin,manager.section==ManagerSection.PlayerAnimations?4:10);
        }
        static void Frame(Vector3 center,float size){if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.LookAt(center,Quaternion.Euler(20,0,0),size);}
        static void ShowCard(CardDef definition,Vector3 origin)
        {
            var canvas=new GameObject("Card preview",typeof(RectTransform),typeof(Canvas));canvas.transform.SetParent(current.root.transform,false);canvas.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;canvas.transform.localScale=Vector3.one*.01f;
            var card=Object.Instantiate(Resources.Load<CardLibrary>("CardLibrary").Find(definition.id),canvas.transform);var catalog=ConfigBundle.Read(ConfigAuthoring.Folder).Catalog();card.Import(definition,catalog);card.Mode("full");card.transform.localPosition=Vector3.zero;
        }
        static void ShowInterface(InterfaceConfig config,Vector3 origin)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<WorldArrowView>(ConfigAuthoring.Root+"World/AttackArrow.prefab");
            if(prefab==null)prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ConfigAuthoring.Root+"World/AttackArrow.prefab").GetComponent<WorldArrowView>();
            var arrow=Object.Instantiate(prefab,current.root.transform);arrow.previewStyle=config.arrow;arrow.Set(origin+Vector3.left*3,origin+Vector3.right*3,.6f,config.arrow.worldWidth,config.arrow.color);
            AddEffect(config.arrow.beginEffect,origin+Vector3.left*3,origin+Vector3.left*3,config.arrow.effectScale,0,config.arrow.effectLifetime);
            AddEffect(config.arrow.dragEffect,origin,origin,config.arrow.effectScale,0,config.arrow.effectLifetime);
            AddEffect(config.arrow.selectEffect,origin+Vector3.right*3,origin+Vector3.right*3,config.arrow.effectScale,.8f,config.arrow.effectLifetime);
            Audio(config.arrow.beginSound,0);Audio(config.arrow.dragSound,.1f);Audio(config.arrow.selectSound,.8f);
            var canvas=new GameObject("Interface preview",typeof(RectTransform),typeof(Canvas));canvas.transform.SetParent(current.root.transform,false);canvas.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;canvas.transform.localScale=Vector3.one*.01f;
            var library=Resources.Load<CardLibrary>("CardLibrary");var catalog=ConfigBundle.Read(ConfigAuthoring.Folder).Catalog();int count=current.manager.previewCards;
            for(int i=0;i<count;i++)
            {
                var card=Object.Instantiate(library.Find(catalog.cards[i%catalog.cards.Count].id),canvas.transform);card.Mode("compact");
                var slot=(RectTransform)card.transform;float angle=FanGeometry.Angle(i,count,config.fan.spread,config.fan.maximumStep),r=config.fan.radius*config.fan.uiUnitsPerMetre;
                slot.localPosition=new Vector3(Mathf.Sin(angle*Mathf.Deg2Rad)*r,-300+(Mathf.Cos(angle*Mathf.Deg2Rad)-1)*r*config.fan.uiPerspective,-i*.1f);
                slot.localRotation=Quaternion.Euler(0,0,-angle*config.fan.uiPerspective);var face=(RectTransform)card.compactFace.transform;slot.localScale=new Vector3(config.fan.cardSize.x/face.rect.width,config.fan.cardSize.y/face.rect.height,1);
                slot.sizeDelta=config.fan.cardSize;
            }
            var statusPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(ConfigAuthoring.Root+"UI/PlayerStatus.prefab");
            var status=Object.Instantiate(statusPrefab,canvas.transform).GetComponent<PlayerStatusView>();
            status.nameAnchor.localPosition=new Vector3(0,200,0);status.nickname.text="Предпросмотр HP";
            status.healthAnchor.localPosition=new Vector3(0,140,0);status.healthAnchor.sizeDelta=config.healthSize;status.healthFill.color=config.healthColor;status.healthTrail.color=config.healthTrailColor;status.healthFill.fillAmount=.6f;status.healthNumber.text="18 / 30";

        }
        static void PreviewSpell(AuthoringManager manager,Vector3 origin)
        {
            var v=manager.vfx;
            if(manager.previewIndex>=v.effects.Length)
            {
                var ids=new[]{v.attack,v.hit,v.death,v.qteFire,v.qteSmoke,v.qteAttempt};
                int index=Mathf.Clamp(manager.previewIndex-v.effects.Length,0,ids.Length-1);
                AddEffect(ids[index],origin,origin,index==0?v.attackScale:index<3?v.impactScale:v.qteScale,0,5);return;
            }
            var fx=v.effects[Mathf.Clamp(manager.previewIndex,0,v.effects.Length-1)];var from=origin+Vector3.left*3;var to=origin+Vector3.right*3;
            void Impact(Vector3 at,float delay)=>AddEffect(fx.impact,at,at,fx.impactScale,delay,fx.lifetime);
            void Move(Vector3 a,Vector3 b,bool card)
            {
                AddEffect(fx.travel,a,b,fx.travelScale,0,fx.travelSeconds,fx.travelSeconds,fx.arc);
                if(card)
                {
                    var source=Resources.Load<CardLibrary>("CardLibrary").Find(fx.id).spellEffect.cardBackPrefab;
                    current.effects.Add(new Effect{prefab=source,from=a,to=b,scale=1,cardSize=fx.cardSize,travel=fx.travelSeconds,life=fx.travelSeconds,arc=fx.arc});
                }
                Impact(b,fx.travelSeconds);
            }
            if(fx.motion==(int)SpellEffect.Motion.Area||fx.motion==(int)SpellEffect.Motion.Boost)Impact(to,0);
            else if(fx.motion==(int)SpellEffect.Motion.Exchange){Move(from,to,true);Move(to,from,true);}
            else if(fx.motion==(int)SpellEffect.Motion.Draw)for(int i=0;i<fx.cardCount;i++)Move(origin+Vector3.right*i*.35f,from+Vector3.right*i*.4f,true);
            else Move(from,to,fx.motion==(int)SpellEffect.Motion.Return);
            AddEffect(fx.persistent,origin,origin,fx.persistentScale,0,fx.lifetime);
            Audio(fx.id+".launch",0);Audio(fx.id+".impact",fx.motion==1||fx.motion==5?0:fx.travelSeconds);
        }
        static void NextStep(float elapsed)
        {
            if(current?.sequence==null)return;current.step++;
            if(current.step>=current.sequence.steps.Length){current.step=current.sequence.steps.Length-1;return;}
            current.stepAt=elapsed;var step=current.sequence.steps[current.step];var p=current.hero.transform.position+step.effectOffset;
            AddEffect(step.vfx,p,p,step.effectScale,elapsed,step.effectLifetime);Audio(step.sound,elapsed);
        }
        static void AddEffect(string id,Vector3 from,Vector3 to,float scale,float start,float life,float travel=0,float arc=0)
        {if(!string.IsNullOrEmpty(id))current.effects.Add(new Effect{id=id,from=from,to=to,scale=scale,start=start,life=life,travel=travel,arc=arc});}
        static void Audio(string action,float at)
        {
            if(string.IsNullOrEmpty(action))return;
            var data=current.manager.audio;
            if(current.manager.section!=ManagerSection.Audio||data?.cues==null)data=ConfigJson.Read<AudioConfig>(System.IO.File.ReadAllText(ConfigAuthoring.Folder+"/audio.json"));
            var cue=data.cues.FirstOrDefault(c=>c.action==action);if(cue!=null)Audio(cue,at);
        }
        static void Audio(AudioCue cue,float at){var sound=ConfigAudio.Select(cue);if(sound!=null)current.sounds.Add(new Sound{variant=ConfigBundle.Clone(sound),bus=cue.bus,start=at+sound.delay,loop=cue.action=="ambience"});}
        static void Update()
        {
            foreach(var m in pending.ToArray()){pending.Remove(m);if(m!=null)try{Refresh(m);}catch(Exception e){Debug.LogWarning("Manager preview: "+e.Message);}}
            if(current==null)return;
            if(current.manager==null||current.root==null){StopAll();return;}
            try
            {
                float elapsed=(float)(EditorApplication.timeSinceStartup-current.started);
                if(current.sequence!=null)
                {
                    var step=current.sequence.steps[current.step];var clip=ConfigRuntime.Assets.Get<AnimationClip>(step.clip);float time=(elapsed-current.stepAt)*step.speed;
                    if(time>=clip.length&&!step.loop&&current.step<current.sequence.steps.Length-1){NextStep(elapsed);step=current.sequence.steps[current.step];clip=ConfigRuntime.Assets.Get<AnimationClip>(step.clip);time=0;}
                    AnimationMode.BeginSampling();AnimationMode.SampleAnimationClip(current.hero,clip,step.loop?time%Mathf.Max(.01f,clip.length):Mathf.Min(time,clip.length));AnimationMode.EndSampling();
                }
                foreach(var effect in current.effects)
                {
                    float t=elapsed-effect.start;if(t<0)continue;
                    if(t>effect.life){if(effect.instance!=null)Object.DestroyImmediate(effect.instance);continue;}
                    if(effect.instance==null){var prefab=effect.prefab!=null?effect.prefab:ConfigRuntime.Assets.Get<GameObject>(effect.id);if(prefab==null)continue;effect.instance=Object.Instantiate(prefab,current.root.transform);effect.instance.transform.localScale=effect.cardSize.x>0?new Vector3(effect.cardSize.x,effect.cardSize.y,1):Vector3.one*effect.scale;foreach(var script in effect.instance.GetComponentsInChildren<MonoBehaviour>(true))script.enabled=false;}
                    float f=effect.travel>0?Mathf.Clamp01(t/effect.travel):0;effect.instance.transform.position=Vector3.Lerp(effect.from,effect.to,f)+Vector3.up*Mathf.Sin(f*Mathf.PI)*effect.arc;
                    foreach(var ps in effect.instance.GetComponentsInChildren<ParticleSystem>(true))ps.Simulate(t,false,true,false);
                }
                foreach(var sound in current.sounds)if(!sound.played&&elapsed>=sound.start){sound.played=true;PlayAudio(sound);}
                SceneView.RepaintAll();EditorApplication.QueuePlayerLoopUpdate();
                if(current.sequence==null&&current.manager.section!=ManagerSection.Cards&&current.manager.section!=ManagerSection.Interface&&elapsed>30)StopAll();
            }
            catch(Exception e){Debug.LogException(e);StopAll();}
        }
        static void PlayAudio(Sound sound)
        {
            var original=ConfigRuntime.Assets.Get<AudioClip>(sound.variant.clip);if(original==null)return;
            float pitch=ConfigAudio.Pitch(sound.variant),volume=sound.variant.volume*UserSettings.Data.master*UserSettings.Volume(sound.bus);
            original.LoadAudioData();var source=new float[original.samples*original.channels];
            if(!original.GetData(source,0))throw new Exception("Для предпросмотра звука используйте Load Type = Decompress On Load.");
            int frames=Mathf.Max(1,(int)(original.samples/pitch));var samples=new float[frames*original.channels];
            for(int i=0;i<frames;i++){float sample=i*pitch;int a=Mathf.Min((int)sample,original.samples-1),b=Mathf.Min(a+1,original.samples-1);for(int c=0;c<original.channels;c++)samples[i*original.channels+c]=Mathf.Lerp(source[a*original.channels+c],source[b*original.channels+c],sample-a)*volume;}
            var clip=AudioClip.Create("Manager audio preview",frames,original.channels,original.frequency,false);clip.hideFlags=HideFlags.HideAndDontSave;clip.SetData(samples,0);current.clips.Add(clip);
            var method=typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil")?.GetMethod("PlayPreviewClip",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static,null,new[]{typeof(AudioClip),typeof(int),typeof(bool)},null);
            if(method==null)throw new Exception("Audio preview API unavailable in this Unity Editor.");method.Invoke(null,new object[]{clip,0,sound.loop});
        }
        public static void Stop(AuthoringManager manager){if(current?.manager==manager)StopAll();}
        public static void StopAll()
        {
            if(current==null)return;
            var session=current;current=null;
            if(AnimationMode.InAnimationMode())AnimationMode.StopAnimationMode();
            typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil")?.GetMethod("StopAllPreviewClips",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static)?.Invoke(null,null);
            if(session.root!=null)Object.DestroyImmediate(session.root);
            foreach(var clip in session.clips)if(clip!=null)Object.DestroyImmediate(clip);
        }
    }
}
