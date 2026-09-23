using System;
using System.IO;
using System.Linq;
using UnityEngine;
namespace SummonersTable
{
    public static class ConfigRuntime
    {
        public static ConfigBundle Current {get;private set;}
        public static Catalog ActiveCatalog {get;private set;}
        public static string ActiveHash {get;private set;}="";
        public static string Error {get;private set;}="";
        public static string Message {get;private set;}="";
        public static ConfigAssets Assets=>Resources.Load<ConfigAssets>("ConfigAssets");
        public static string TestDirectory;
        public static string DirectoryPath=>TestDirectory??(Application.isEditor?Path.Combine(Application.dataPath,"StreamingAssets/Config"):Path.GetFullPath(Path.Combine(Application.dataPath,"../Config")));
        public static bool Available=>Current!=null;
        public static string GameplaySummary(RulesDef r)=>"Раундов: "+r.rounds+". Победа +"+r.roundWinPoints+", устранение +"+r.eliminationPoints+".\n\nВолшебники: "+r.wizardSpells+" заклинаний либо "+r.wizardCreatures+" существ и "+r.wizardMixedSpells+" заклинаний. Полководцы: "+r.commandersQte+" QTE за ход.\n\nHP: "+r.heroHp+"; рука: "+r.handLimit+"; слоты: "+r.boardSlots+". QTE: "+r.qteMistakes+" ошибки или тайм-аут передают карту противнику.\n\nСущества атакуют в конце хода. Реакции доступны во время розыгрыша, до завершения QTE. Стоимость расходуется и при срыве.\n\nКамера: колесо и ПКМ. Центр стола означает случайного соперника.";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){Current=null;ActiveCatalog=null;ActiveHash="";Error="";Message="";TestDirectory=null;}
        public static bool LoadInitial()
        {
            try{Current=ConfigBundle.Read(DirectoryPath);ActivateGameplay();return true;}
            catch(Exception e)
            {
                Error=e.Message;Message="Ошибка Config: "+e.Message;
                // A valid bundled snapshot keeps the error UI usable, but starting a game is blocked.
                Current=ConfigBundle.Read(name=>Resources.Load<TextAsset>("ConfigDefaults/"+Path.GetFileNameWithoutExtension(name)).text);ActivateGameplay();return false;
            }
        }
        public static bool Reload()
        {
            try{var next=ConfigBundle.Read(DirectoryPath);Current=next;Error="";ApplyScene();Message="Звук и оформление обновлены. Правила применятся к следующему матчу.";return true;}
            catch(Exception e){Error=e.Message;Message="Не загружено: "+e.Message;return false;}
        }
        public static bool ActivateGameplay()
        {
            if(Current==null||ActiveHash==Current.gameplayHash)return false;
            ActiveCatalog=Current.Catalog();ActiveHash=Current.gameplayHash;return true;
        }
        public static PresentationSettings Settings()
        {
            var result=UnityEngine.Object.Instantiate(Resources.Load<PresentationSettings>("PresentationSettings"));
            result.hideFlags=HideFlags.DontSave;Configure(result);return result;
        }
        static void Configure(PresentationSettings s)
        {
            if(Current==null)return;var a=Assets;var v=Current.vfx;
            s.data=ConfigBundle.Clone(Current.presentation.camera);
            s.cardHover=Clip("card.hover");s.invalidAction=Clip("action.invalid");s.attack=Clip("creature.attack");s.hit=Clip("damage.hit");s.qteSuccess=Clip("qte.correct");s.qteError=Clip("qte.error");s.turnNotice=Clip("turn.start");s.music=Clip("ambience");
            s.attackEffect=a.Get<GameObject>(v.attack);s.impactEffect=a.Get<GameObject>(v.hit);s.deathEffect=a.Get<GameObject>(v.death);s.qteFire=a.Get<GameObject>(v.qteFire);s.qteSmoke=a.Get<GameObject>(v.qteSmoke);s.qteAttempt=a.Get<GameObject>(v.qteAttempt);s.motionTitle=a.Get<GameObject>(v.motionTitle);
            s.attackEffectScale=v.attackScale;s.impactEffectScale=v.impactScale;s.qteEffectScale=v.qteScale;
        }
        public static AudioClip Clip(string action)=>Assets?.Get<AudioClip>(Current?.audio.cues.FirstOrDefault(c=>c.action==action)?.sounds.FirstOrDefault()?.clip);
        public static void ApplyScene()
        {
            if(Current==null)return;
            foreach(var board in UnityEngine.Object.FindObjectsByType<TableBoard>(FindObjectsInactive.Include,FindObjectsSortMode.None))ApplyWorld(board,Current.world);
            foreach(var binding in UnityEngine.Object.FindObjectsByType<PrefabConfigBinding>(FindObjectsInactive.Include,FindObjectsSortMode.None))binding.Apply();
            foreach(var camera in UnityEngine.Object.FindObjectsByType<ManualTableCamera>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {if(camera.settings!=null&&(camera.settings.hideFlags&HideFlags.DontSave)!=0)Configure(camera.settings);camera.Apply(ConfigBundle.Clone(Current.presentation.camera));}
            foreach(var title in UnityEngine.Object.FindObjectsByType<MotionAnnouncements>(FindObjectsInactive.Include,FindObjectsSortMode.None))title.Configure(Current.vfx.motionTitle);
            ConfigAudio.RefreshAmbience();
        }
        public static void ApplyWorld(TableBoard board,LocationConfig world)
        {
            if(board==null)return;
            if(board.seating!=null){board.seating.bodyScale=world.scale;if(Application.isPlaying){foreach(var seat in board.seating.Seats)if(seat.body!=null)seat.body.localScale=Vector3.one*world.scale;}else board.seating.Build(board.seating.previewCount,world.scale);}
            if(!string.IsNullOrEmpty(world.scene))board.ApplyInterior(world.scene);
        }
        public static void ConfigureSpell(SpellEffect fx,string id)
        {
            if(Current==null)return;var v=Current.vfx.effects.FirstOrDefault(e=>e.id==id);if(v==null)return;var a=Assets;
            fx.configId=id;fx.motion=(SpellEffect.Motion)v.motion;fx.travelPrefab=a.Get<GameObject>(v.travel);fx.impactPrefab=a.Get<GameObject>(v.impact);fx.persistentPrefab=a.Get<GameObject>(v.persistent);
            fx.travelScale=v.travelScale;fx.impactScale=v.impactScale;fx.persistentScale=v.persistentScale;fx.travelSeconds=v.travelSeconds;fx.lifetime=v.lifetime;fx.arc=v.arc;fx.cardCount=v.cardCount;fx.cardSize=v.cardSize;
            fx.launchSound=Clip(id+".launch");fx.impactSound=Clip(id+".impact");
        }
        public static Texture2D Artwork(CardDef card)=>Assets?.Get<Texture2D>(card.art)??Resources.Load<Texture2D>("Art/"+card.id);
    }
}
