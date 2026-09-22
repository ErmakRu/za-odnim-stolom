using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed partial class GameApp
    {
        IEnumerator CaptureEditableFeatures(string directory)
        {
            StartLocal(4);handoff=false;seat=0;state=local.View(seat,localTime);board.Sync(state,seat,Clock);board.SnapCamera(0,false);
            local.State.players[1].hp=16;state=local.View(seat,localTime);
            yield return Shot(directory,"16-persistent-names-and-health");
            if(ui.playerStatus[1].healthNumber.text!="16 / 30"||Mathf.Abs(ui.playerStatus[1].healthFill.fillAmount-16/30f)>.01f)throw new Exception("Persistent HP UI mismatch");
            float originalMaster=AudioSettings.Master,originalEffects=AudioSettings.Effects;
            ui.hud.Get<Button>("settings").onClick.Invoke();ui.settings.Get<Slider>("master").value=.55f;ui.settings.Get<Slider>("effects").value=0;
            yield return Shot(directory,"17-master-and-effects-settings");
            if(!settingsOpen||Mathf.Abs(AudioListener.volume-.55f)>.001f||ui.GetComponent<AudioSource>().volume!=0)throw new Exception("Audio sliders not applied");
            ui.settings.Get<Slider>("master").value=originalMaster;ui.settings.Get<Slider>("effects").value=originalEffects;ui.settings.Get<Button>("resume").onClick.Invoke();
            for(int i=1;i<=8;i++)
            {
                board.ClearMatchVisuals();board.PreviewSpell("S"+i.ToString("00"));yield return Shot(directory,"18-spell-effect-S"+i.ToString("00"));
                if(!board.GetComponentsInChildren<SpellEffect>().Any())throw new Exception("Spell effect not spawned");
            }
            board.ClearMatchVisuals();local.State.phase="matchEnd";local.State.result="Победитель: Игрок 1";local.State.players[0].score=4;state=local.View(seat,localTime);
            yield return Shot(directory,"19-results-and-rematch");string previous=state.matchId;
            ui.results.Get<Button>("again").onClick.Invoke();yield return null;
            if(local.State.matchId!=previous||local.State.players.Count(p=>p.postMatchChoice=="again")!=1)throw new Exception("Rematch skipped unanimous confirmation");
            yield return Shot(directory,"20-waiting-for-rematch-votes");
            for(int i=0;i<3;i++)ui.results.Get<Button>("again").onClick.Invoke();handoff=false;yield return null;
            if(local.State.matchId==previous||local.State.phase!="action"||local.State.players.Any(p=>p.score!=0)||local.State.round!=1)throw new Exception("Fresh rematch did not start");
            yield return Shot(directory,"21-fresh-rematch");
            local.State.phase="matchEnd";state=local.View(seat,localTime);yield return null;ui.results.Get<Button>("deck").onClick.Invoke();
            yield return Shot(directory,"22-return-to-deck-lobby");if(page!="local"||!lobbyCanvas.gameObject.activeInHierarchy)throw new Exception("Deck button did not return to lobby");
            StartLocal(2);handoff=false;local.State.phase="matchEnd";state=local.View(0,localTime);yield return null;ui.results.Get<Button>("menu").onClick.Invoke();yield return Shot(directory,"23-return-to-menu");if(page!="menu")throw new Exception("Results menu exit failed");
        }
    }
}
