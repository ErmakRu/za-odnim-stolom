#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.IO;
using UnityEngine;
namespace SummonersTable
{
    public sealed partial class GameApp
    {
        public bool EditorBotSmokeDone {get;private set;}
        public void BeginEditorBotSmoke(){StartCoroutine(BotSmoke());}
        IEnumerator BotSmoke()
        {
            void Check(bool ok,string message){if(!ok)throw new Exception("BOT PLAY: "+message);}
            captureMode=true;UserSettings.TestPath=Path.GetFullPath("../tmp/bot-user-settings.json");UserSettings.Load();
            var original=ConfigRuntime.Current.bots;ConfigRuntime.Current.bots=ConfigBundle.Clone(original);
            try
            {
                foreach(var profile in ConfigRuntime.Current.bots.profiles)
                {profile.timing.thinkMin=profile.timing.thinkMax=.01f;profile.timing.qteMin=profile.timing.qteMax=.01f;profile.timing.reactionMin=profile.timing.reactionMax=.01f;}
                FrontEndAction("local");localCount=4;SyncFrontEnd();yield return null;
                Check(localBots.Skip(1).All(b=>b),"default opponents are bots");
                var controls=lobbyCanvas.GetComponentInChildren<BotLobbyControls>(true);Check(controls!=null,"prefab attached");
                controls.seats[1].onClick.Invoke();Check(!localBots[1],"toggle bot to human");controls.seats[1].onClick.Invoke();Check(localBots[1],"toggle human to bot");SyncFrontEnd();yield return null;
                EditorCapture?.Invoke("../output/editor-bots/bot-lobby.png");
                int games=0;
                foreach(string mode in new[]{MatchOptions.Wizards,MatchOptions.Commanders})
                foreach(int count in new[]{2,3,4})
                {
                    localOptions.mode=mode;StartLocal(count);Check(!handoff&&seat==0,"single human needs no handoff");int steps=0;
                    while(local.State.phase!="matchEnd"&&steps<40000)
                    {
                        for(int i=0;i<30&&local.State.phase!="matchEnd";i++)
                        {
                            localTime+=.2;UpdateSession();steps++;
                            Check(seat==0&&!handoff,"never reveal/switch to bot seat");Check(state.players.Skip(1).All(p=>p.hand.Count==0),"bot hand stays hidden");
                            if(state.phase=="action"&&state.activeSeat==0&&state.players[0].alive)Send(new GameCommand{kind="end"});
                            else if(state.pending!=null&&state.cast!=null&&state.pending.source!=0&&!state.pending.responded.Contains(0))Send(new GameCommand{kind="pass",phaseId=state.cast.id});
                        }
                        yield return null;
                    }
                    Check(local.State.phase=="matchEnd","match finishes from Play");Check(localBotDirector.Rejected==0,localBotDirector.LastError);
                    for(int i=0;i<10;i++){localTime+=.2;UpdateSession();}
                    string old=local.State.matchId;ChoosePostMatch("again");Check(local.State.matchId!=old,"human rematch restarts with bots");games++;
                }
                localBots[1]=false;localBots[2]=true;StartLocal(3);handoff=false;local.State.activeSeat=2;state=local.View(0,0);UpdateSession();Check(seat==0,"mixed hotseat never switches to bot");
                ExitMatch();Check(local==null&&state==null&&page=="menu","clean exit");
                Directory.CreateDirectory("../output/tests");File.WriteAllText("../output/tests/bots-editor-play.txt",$"PASS Unity Editor Play: prefab lobby buttons, default/toggled seats, {games} complete human+bot matches across both modes and 2/3/4 players, privacy, handoff, zero rejected bot commands, rematch and menu exit. No Player build.\n");
                EditorBotSmokeDone=true;
            }
            finally{ConfigRuntime.Current.bots=original;}
        }
    }
}
#endif
