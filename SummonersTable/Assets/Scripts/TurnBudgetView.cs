using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed class TurnBudgetView:MonoBehaviour
    {
        public Text mode,spellRoute,mixedRoute,remaining,either;
        public Image spellPanel,mixedPanel;
        public Color active=new Color(.98f,.79f,.42f),inactive=new Color(.43f,.49f,.5f);
        public void Present(MatchState state,int viewer)
        {
            bool mine=state.activeSeat==viewer;mode.text=state.options.ModeName+(mine?"":" · ход соперника");bool commanders=state.options.IsCommanders;
            spellPanel.gameObject.SetActive(!commanders);mixedPanel.gameObject.SetActive(!commanders);
            if(either!=null)either.gameObject.SetActive(!commanders);
            remaining.gameObject.SetActive(commanders);
            if(commanders){remaining.text="QTE: "+state.qteSpent+" / "+state.rules.commandersQte+"\nОсталось: "+(state.rules.commandersQte-state.qteSpent);remaining.color=state.qteSpent<state.rules.commandersQte?active:inactive;return;}
            bool spells=state.creaturePlayed==0,mixed=state.spellsPlayed<=state.rules.wizardMixedSpells;
            spellRoute.text=state.spellsPlayed+" / "+state.rules.wizardSpells+" заклинаний";
            mixedRoute.text=Mathf.Min(state.rules.wizardMixedSpells,state.spellsPlayed)+" / "+state.rules.wizardMixedSpells+" заклинаний\n"+state.creaturePlayed+" / "+state.rules.wizardCreatures+" существ";
            spellRoute.color=spells?active:inactive;mixedRoute.color=mixed?active:inactive;
            spellPanel.color=spells?new Color(.17f,.19f,.16f,.98f):new Color(.045f,.07f,.08f,.85f);mixedPanel.color=mixed?new Color(.17f,.19f,.16f,.98f):new Color(.045f,.07f,.08f,.85f);
        }
    }
}
