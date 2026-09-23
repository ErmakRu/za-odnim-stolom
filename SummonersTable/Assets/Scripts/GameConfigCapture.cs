using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace SummonersTable
{
    public sealed partial class GameApp
    {
        IEnumerator CaptureConfigPreview()
        {
            var args=Environment.GetCommandLineArgs();int arg=Array.IndexOf(args,"--capture-dir");string directory=arg>=0?args[arg+1]:Path.GetFullPath("captures-config");Directory.CreateDirectory(directory);
            string fixture=Path.Combine(directory,"Config-fixture");Directory.CreateDirectory(fixture);foreach(string file in ConfigBundle.Files)File.Copy(Path.Combine(ConfigRuntime.DirectoryPath,file),Path.Combine(fixture,file),true);
            yield return Shot(directory,"01-config-menu");
            settingsOpen=true;yield return Shot(directory,"01-settings-from-menu");if(!ui.settings.gameObject.activeInHierarchy)throw new Exception("Main menu settings not visible");settingsOpen=false;
            if(announcements.style.gameObject.activeSelf)throw new Exception("Unrequested default motion title visible");
            announcements.Preview("ПРОВЕРКА");yield return null;if(!announcements.style.IsPlaying||announcements.style.textItems.Any(t=>t.text!="ПРОВЕРКА"))throw new Exception("Configured motion title did not play");announcements.style.Stop();
            ConfigRuntime.TestDirectory=fixture;
            try
            {
                StartLocal(4);handoff=false;yield return Shot(directory,"02-shared-seats");
                var original=ConfigRuntime.ActiveCatalog.Card("C02");string oldName=original.name;int oldAttack=original.attack;int oldHp=local.State.players[0].hp;string oldHash=ConfigRuntime.ActiveHash;
                var cards=ConfigJson.Read<CardsConfig>(File.ReadAllText(Path.Combine(fixture,"cards.json")));var bear=cards.cards.First(c=>c.id=="C02");bear.name="Медведь из JSON";bear.attack=9;
                File.WriteAllText(Path.Combine(fixture,"cards.json"),JsonUtility.ToJson(cards,true));
                var rules=ConfigJson.Read<RulesConfig>(File.ReadAllText(Path.Combine(fixture,"rules.json")));rules.rules.heroHp=19;rules.rules.commandersQte=7;File.WriteAllText(Path.Combine(fixture,"rules.json"),JsonUtility.ToJson(rules,true));
                var world=ConfigJson.Read<LocationConfig>(File.ReadAllText(Path.Combine(fixture,"world.json")));world.scale*=1.15f;File.WriteAllText(Path.Combine(fixture,"world.json"),JsonUtility.ToJson(world,true));
                var audio=ConfigJson.Read<AudioConfig>(File.ReadAllText(Path.Combine(fixture,"audio.json")));audio.cues.First(c=>c.action=="card.hover").sounds[0].volume=.11f;File.WriteAllText(Path.Combine(fixture,"audio.json"),JsonUtility.ToJson(audio,true));
                if(!ConfigRuntime.Reload())throw new Exception(ConfigRuntime.Error);
                if(ConfigRuntime.ActiveHash!=oldHash||catalog.Card("C02").attack!=oldAttack||local.State.players[0].hp!=oldHp)throw new Exception("Reload mutated live balance");
                var chair=board.seating.Seats[0].body;if(Mathf.Abs(chair.localScale.x-world.scale)>.001f)throw new Exception("World did not reload");
                if(ConfigRuntime.Current.audio.cues.First(c=>c.action=="card.hover").sounds[0].volume!=.11f)throw new Exception("Audio did not reload");
                yield return Shot(directory,"03-live-world-reload");
                local.State.phase="matchEnd";state=local.View(seat,localTime);localOptions.mode=MatchOptions.Commanders;StartLocal(4);handoff=false;OptionsHand();yield return null;
                if(state.players[0].hp!=19||state.rules.commandersQte!=7||catalog.Card("C02").attack!=9)throw new Exception("Next match did not use pending JSON");
                previewPointer=CaptureHandPoint("options-card-0");yield return Shot(directory,"04-json-card-next-match");
                if(cardCanvas.rightSlot.view.fullName.text!="Медведь из JSON")throw new Exception("Card view ignored JSON");
                string valid=File.ReadAllText(Path.Combine(fixture,"rules.json"));File.WriteAllText(Path.Combine(fixture,"rules.json"),"{bad json");
                var snapshot=ConfigRuntime.Current;if(ConfigRuntime.Reload()||ConfigRuntime.Current!=snapshot)throw new Exception("Invalid config was partially applied");
                settingsOpen=true;yield return Shot(directory,"05-invalid-json-rejected");File.WriteAllText(Path.Combine(fixture,"rules.json"),valid);
                ConfigRuntime.TestDirectory=null;if(!ConfigRuntime.Reload())throw new Exception("Defaults did not reload");settingsOpen=false;page="local";StartLocal(4);handoff=false;
                yield return Shot(directory,"06-defaults-restored");
                var actor=board.Actor(0);var sequence=actor.GetComponent<HeroAnimationSequence>();var definition=ConfigBundle.Clone(ConfigRuntime.Current.animations.states.First(s=>s.id=="hit"));var nextStep=ConfigBundle.Clone(ConfigRuntime.Current.animations.states.First(s=>s.id=="turn").steps[0]);nextStep.loop=true;definition.steps[0].speed=4;definition.steps=new[]{definition.steps[0],nextStep};sequence.Play(definition,actor.Animator,true);
                float delay=ConfigRuntime.Assets.Get<AnimationClip>(definition.steps[0].clip).length/4+.2f;yield return new WaitForSecondsRealtime(delay);
                if(sequence.ClipName!=ConfigRuntime.Assets.Get<AnimationClip>(nextStep.clip).name||!sequence.Busy)throw new Exception("Configured animation sequence did not reach looping step");sequence.Stop();actor.Play("Idle_Normal",0);
                if(FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l=>l.isActiveAndEnabled)!=1)throw new Exception("Expected one shared AudioRig listener");
                File.WriteAllText(Path.Combine(directory,"capture-report.txt"),"PASS shared chair/seat instances; external JSON reload; world and audio immediately change; live cards/HP/rules unchanged; next match receives HP19, QTE7, renamed bear and attack9; invalid JSON preserves whole snapshot; defaults restored. No production Config edited.\n");
                File.Copy(Path.Combine(directory,"capture-report.txt"),Path.Combine(directory,"config-report.txt"),true);Debug.Log("CONFIG_CAPTURE_COMPLETE "+directory);
            }
            finally{ConfigRuntime.TestDirectory=null;}
            #if UNITY_EDITOR
            if(editorSmokeNoCapture){EditorConfigSmokeDone=true;yield break;}
#endif
            Application.Quit();
        }
    }
}
