using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace SummonersTable.Editor
{
    public static class ManagerEditorTests
    {
        static int checks;
        static void Check(bool value,string label){checks++;if(!value)throw new Exception("Manager test: "+label);}
        static void Reject(Action action,string label){bool rejected=false;try{action();}catch{rejected=true;}Check(rejected,label);}
        static AuthoringManager Manager(ManagerSection section){var obj=new GameObject("Test "+section);var m=obj.AddComponent<AuthoringManager>();m.section=section;m.Import(File.ReadAllText(ConfigAuthoring.Folder+"/"+m.FileName));return m;}
        public static void Run()
        {
            checks=0;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            ConfigTests.Run();Seating();Storage();Rules();Preferences();Preview();
            CoreTests.Run();RuleOptionsTests.Run();PrefabTests.Run();ShaderStyleTests.Run();PresentationTests.Run();TableScaleAuthoring.Validate();
            Directory.CreateDirectory("../output/tests");
            File.WriteAllText("../output/tests/managers-v0.8.txt","PASS "+checks+" manager assertions: 1–8 shared seats, fixed room, linked scale/anchors, section snapshots and rollback, transactional validation, event player-turn frequency/duration, QTE damage, location timers, wire privacy, audio variants, user JSON roundtrip, effect/animation/card previews and cleanup. No Player build.\n");
            Debug.Log("MANAGER_TESTS_PASSED "+checks);
        }
        public static void ValidateSeatingPrefab()
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(ConfigAuthoring.WorldPath);
            Check(source.GetComponentsInChildren<PlayerSeatView>(true).Length==0,"no baked duplicate seat banks");
            var root=(GameObject)PrefabUtility.InstantiatePrefab(source);var board=root.GetComponent<TableBoard>();
            try
            {
                Check(board.authoredEnvironment!=null&&PrefabUtility.IsPartOfPrefabInstance(board.authoredEnvironment),"fixed authored room");
                foreach(int count in new[]{1,2,3,4,5,8})
                {
                    var layout=board.seating.Build(count,1,true);var seats=board.seating.Seats;
                    Check(seats.Length==count&&layout.slotAnchors.Length==count*5,"generated count "+count);
                    for(int i=0;i<count;i++)
                    {
                        var seat=seats[i];
                        Check(Source(seat.gameObject).Contains("/HeroSittingPlace.prefab")&&Source(seat.gameObject).Contains("/PlayerSeat.prefab"),"seat variant chain");
                        Check(Source(seat.chair.gameObject).Contains("/Chair.prefab"),"common chair");
                        Check(Mathf.Abs(seat.transform.localPosition.magnitude-board.seating.radius)<.001f,"on circle");
                        Check(Vector3.Dot(seat.transform.forward,-seat.transform.localPosition.normalized)>.999f,"looks at table");
                        Check(seat.status!=null&&seat.status.seatOwned&&seat.status.seatHealthPoint==seat.healthAnchor&&seat.fan.anchor==seat.handAnchor,"seat owns UI and hand anchors");
                        foreach(var slot in seat.slots)Check(Source(slot.gameObject).Contains("/CreatureSlot.prefab")&&slot.GetComponent<BoardTarget>().seat==i,"common assigned slot");
                    }
                }
                board.seating.Build(5,1.4f,true);foreach(var seat in board.seating.Seats){Check(Mathf.Abs(seat.body.localScale.x-1.4f)<.001f,"body scale");Check(seat.avatar.transform.parent==seat.body&&seat.chair.parent==seat.body,"avatar and chair scale together");}
                board.seating.Clear();Check(board.seating.Seats.Length==0,"clear generated places");
            }
            finally{Object.DestroyImmediate(root);}
        }
        static string Source(GameObject obj){string text="";for(int i=0;i<12&&obj!=null;i++){obj=PrefabUtility.GetCorrespondingObjectFromSource(obj);if(obj!=null)text+="|"+AssetDatabase.GetAssetPath(obj);}return text;}
        static void Seating(){ValidateSeatingPrefab();}
        static void Storage()
        {
            string directory=Path.GetFullPath("../tmp/config-work/test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            foreach(string name in ConfigBundle.Files)File.Copy(ConfigAuthoring.Folder+"/"+name,Path.Combine(directory,name));
            var m=Manager(ManagerSection.World);
            try
            {
                string audio=File.ReadAllText(directory+"/audio.json");m.world.name="Копия до правок";
                ManagerStorage.Copy(m,directory,DateTime.UtcNow.AddMinutes(-2));m.world.name="Последняя копия";
                string latest=ManagerStorage.Copy(m,directory,DateTime.UtcNow.AddMinutes(-1));m.world.name="Будущее";
                ManagerStorage.Copy(m,directory,DateTime.UtcNow.AddDays(1));m.world.name="Черновик";
                Check(ManagerStorage.Latest(m,directory)==latest&&m.world.name=="Последняя копия","latest non-future copy");
                Check(File.ReadAllText(directory+"/audio.json")==audio,"rollback isolated");
                ManagerStorage.Save(m,directory);Check(File.ReadAllText(directory+"/world.json").Contains("Последняя копия")&&File.ReadAllText(directory+"/main.json").Contains("Последняя копия"),"save module plus main");
                string before=File.ReadAllText(directory+"/main.json");m.world.scale=-1;Reject(()=>ManagerStorage.Save(m,directory),"invalid save rejected");
                Check(File.ReadAllText(directory+"/main.json")==before&&File.ReadAllText(directory+"/audio.json")==audio,"invalid transaction no writes");
                var inspector=UnityEditor.Editor.CreateEditor(m);Check(inspector is AuthoringManagerInspector,"typed inspector");Object.DestroyImmediate(inspector);
            }
            finally{Object.DestroyImmediate(m.gameObject);}
        }
        static GameEngine Engine(Catalog catalog,int count=2)=>new GameEngine(catalog,Enumerable.Range(0,count).Select(i=>new LobbyMember{id="p"+i,name="Player "+i,deckId=catalog.decks[0].id}).ToArray(),91);
        static void Advance(GameEngine e,ref int sequence)
        {
            foreach(var p in e.State.players)p.units.Clear();
            Check(e.Submit(e.State.activeSeat,new GameCommand{kind="end",seq=++sequence},e.State.serverTime).ok,"end player turn");
            int guard=30;while(e.State.phase=="combat"&&guard-->0)e.Tick(e.State.deadline+.001);
            Check(guard>0,"combat completed");
        }
        static void Rules()
        {
            var bundle=ConfigBundle.Read(ConfigAuthoring.Folder);var catalog=bundle.Catalog();
            catalog.world.timeChanges=true;catalog.world.playersTurnTime=41;catalog.world.qte=15;catalog.world.revealTime=1;catalog.world.gameTimer=200;
            catalog.world.events=new[]{new LocationEventRule{id=0,enabled=true,eventId=3,chance=100,everyTurns=4,duration=2}};
            var engine=Engine(catalog);Check(engine.State.worldEvents.Count==1&&engine.State.worldEvents[0].expiresTurn==3,"event starts on first player turn");
            Check(engine.State.deadline==41&&engine.State.matchDeadline==200,"location timers");
            var creature=new UnitState{uid="event-target",cardId="C02",hp=8,slot=0};engine.State.players[1].units.Add(creature);
            engine.State.players[0].hand.Add(new HandCard{uid="event-spell",cardId="S01"});int sequence=1;
            Check(engine.Submit(0,new GameCommand{kind="play",seq=sequence,cardUid="event-spell",targetSeat=1},0).ok,"play under location rules");
            Check(engine.State.cast.revealUntil==1,"reveal override");engine.Tick(1);var q=engine.State.qte;
            Check(q!=null&&q.duration==15,"QTE total duration override");
            Check(engine.Submit(0,new GameCommand{kind="key",seq=++sequence,phaseId=q.id,key=q.sequence[q.index].ToString()},1).ok&&creature.hp==8,"first QTE symbol no damage");
            string wrong=q.sequence[q.index]=='A'?"S":"A";
            engine.Submit(0,new GameCommand{kind="key",seq=++sequence,phaseId=q.id,key=wrong},1);Check(creature.hp==8,"mistake no event damage");
            engine.Submit(0,new GameCommand{kind="key",seq=++sequence,phaseId=q.id,key=q.sequence[q.index].ToString()},1);Check(creature.hp==7&&engine.State.worldNotices.Count==1,"second correct symbol damages once");
            var wire=JsonUtility.FromJson<WireMessage>(JsonUtility.ToJson(new WireMessage{state=engine.View(1,1)}));wire.state.RestoreViewPrivacy(1);
            Check(wire.protocol==9&&wire.state.qte==null&&wire.state.worldEvents.Count==1&&wire.state.worldNotices.Count==1,"world state shared, QTE private");
            wire.state.worldEvents[0].expiresTurn=99;Check(engine.State.worldEvents[0].expiresTurn==3,"world views independent");
            while(engine.State.qte!=null){q=engine.State.qte;engine.Submit(0,new GameCommand{kind="key",seq=++sequence,phaseId=q.id,key=q.sequence[q.index].ToString()},1);}
            Advance(engine,ref sequence);Check(engine.State.turnNumber==2&&engine.State.worldEvents.Count==1,"duration includes next player's turn");
            Advance(engine,ref sequence);Check(engine.State.turnNumber==3&&engine.State.worldEvents.Count==0,"expires after two individual turns");
            Advance(engine,ref sequence);Check(engine.State.worldEvents.Count==0,"frequency skips turn four");
            Advance(engine,ref sequence);Check(engine.State.turnNumber==5&&engine.State.worldEvents.Count==1,"every four player turns");
            engine.Tick(201);Check(engine.State.phase=="matchEnd"&&engine.State.result.Contains("Время партии"),"global timeout finishes match");
            catalog.world.playerRange=new Vector2Int(1,8);Reject(()=>Engine(catalog,5),"preview range does not expand online rules");
            catalog.world.playerRange=new Vector2Int(3,4);Reject(()=>Engine(catalog,2),"location min enforced");
            var hash=bundle.gameplayHash;var world=ConfigBundle.Clone(bundle.world);world.scale=1.2f;
            Check(ConfigBundle.Read(ConfigAuthoring.Folder,"world.json",JsonUtility.ToJson(world)).gameplayHash==hash,"body scale cosmetic");
            world.qte=10;world.timeChanges=true;Check(ConfigBundle.Read(ConfigAuthoring.Folder,"world.json",JsonUtility.ToJson(world)).gameplayHash!=hash,"location mechanics hash");
            var audio=ConfigBundle.Clone(bundle.audio);var cue=audio.cues.First(c=>c.action=="card.hover");cue.sounds=Array.Empty<SoundVariant>();
            Check(ConfigAudio.Select(cue)==null,"zero variants is silence");ConfigBundle.Read(ConfigAuthoring.Folder,"audio.json",JsonUtility.ToJson(audio));
            cue.sounds=new[]{new SoundVariant{clip=bundle.audio.cues[0].sounds[0].clip,volume=.3f,randomPitch=true,pitchRange=new Vector2(.5f,1.5f)},new SoundVariant{clip=bundle.audio.cues[0].sounds[0].clip,volume=.7f}};
            bool first=false,second=false;for(int i=0;i<200;i++){var sound=ConfigAudio.Select(cue);first|=sound.volume==.3f;second|=sound.volume==.7f;float pitch=ConfigAudio.Pitch(sound);Check(pitch>=.5f&&pitch<=1.5f,"pitch bounds");}Check(first&&second,"all variants selectable");
            ConfigBundle.Read(ConfigAuthoring.Folder,"audio.json",JsonUtility.ToJson(audio));
        }
        static void Preferences()
        {
            string old=UserSettings.TestPath;float master=AudioListener.volume;int fps=Application.targetFrameRate;
            try
            {
                UserSettings.TestPath=Path.GetFullPath("../tmp/config-work/user-settings-test.json");UserSettings.Load();
                UserSettings.Data.master=.4f;UserSettings.Data.effects=.5f;UserSettings.Data.voices=.3f;UserSettings.Data.screenMode=1;UserSettings.Data.width=1920;UserSettings.Data.frameLimit=120;
                UserSettings.Apply();UserSettings.Load();Check(UserSettings.Data.width==1920&&UserSettings.Data.screenMode==1&&UserSettings.Data.frameLimit==120,"display JSON roundtrip");
                Check(Mathf.Abs(AudioListener.volume*UserSettings.Volume(AudioBus.Effects)-.2f)<.0001f&&Mathf.Abs(UserSettings.Volume(AudioBus.Voices)-.3f)<.0001f,"master times independent buses");
            }
            finally{UserSettings.TestPath=old;UserSettings.Load();AudioListener.volume=master;Application.targetFrameRate=fps;}
        }
        static void Preview()
        {
            foreach(var section in new[]{ManagerSection.Vfx,ManagerSection.Events,ManagerSection.Audio,ManagerSection.PlayerAnimations,ManagerSection.Cards,ManagerSection.Interface})
            {
                var manager=Manager(section);
                try
                {
                    ManagerPreview.Play(manager);Check(ManagerPreview.IsRunning&&ManagerPreview.OwnedObjects==1,"preview owns one transient root "+section);
                    typeof(ManagerPreview).GetMethod("Update",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
                    Check(ManagerPreview.IsRunning,"preview survives update "+section);
                    ManagerPreview.Stop(manager);Check(!ManagerPreview.IsRunning&&ManagerPreview.OwnedObjects==0&&!AnimationMode.InAnimationMode(),"stop/cleanup "+section);
                }
                finally{ManagerPreview.StopAll();Object.DestroyImmediate(manager.gameObject);}
            }
            foreach(string name in new[]{"LocationLab","EffectsLab","AnimationLab","CardsLab"})Check(File.Exists("Assets/Scenes/Authoring/"+name+".unity"),"authoring scene "+name);
            var settings=AssetDatabase.LoadAssetAtPath<GameObject>(ConfigAuthoring.Root+"UI/SettingsPanel.prefab").GetComponent<UserSettingsView>();
            Check(settings!=null&&settings.ambience!=null&&settings.voices!=null&&settings.music!=null&&settings.screenMode!=null&&settings.resolution!=null&&settings.refreshRate!=null&&settings.frameLimit!=null&&settings.red!=null,"settings prefab controls");
            var menu=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MainMenuCanvas.prefab").GetComponent<FrontEndCanvas>();Check(menu.actions.Contains("settings"),"main menu settings");
        }
    }
}
