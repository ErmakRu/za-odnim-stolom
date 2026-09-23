using System.Linq;
namespace SummonersTable
{
    public static class MatchRules
    {
        public const int QteBudget=10;
        // The printed QTE is the resource cost. Interference changes the ritual, not its price.
        public static int Cost(CardDef card)=>card.kind=="reaction"?0:System.Math.Max(0,card.qte);
        public static int Stack(MatchState state,int amount,int cap)=>state.options.limitPower?System.Math.Min(cap,amount):amount;
        public static bool CanSpend(MatchState state,CardDef card)
        {
            if(card==null||card.kind=="reaction")return false;
            if(state.options.IsCommanders)return Cost(card)<=state.rules.commandersQte-state.qteSpent;
            return card.kind=="creature"?state.creaturePlayed<state.rules.wizardCreatures&&state.spellsPlayed<=state.rules.wizardMixedSpells:state.spellsPlayed<(state.creaturePlayed>0?state.rules.wizardMixedSpells:state.rules.wizardSpells);
        }
        public static bool CanUse(Catalog catalog,MatchState state,int seat,CardDef card)
        {
            if(state==null||seat<0||seat>=state.players.Count||card==null||!state.players[seat].connected)return false;
            return card.kind=="reaction"?state.pending!=null&&state.pending.targets.Any(t=>CanReact(catalog,state,seat,card,t)):CanPlay(catalog,state,seat,card);
        }
        // Shared by the host and UI. A glowing button is advice, never an extra gate.
        public static bool CanPlay(Catalog catalog,MatchState state,int seat,CardDef card)
        {
            if(state.phase!="action"||state.activeSeat!=seat||!state.players[seat].alive||card==null||card.kind=="reaction")return false;
            var p=state.players[seat];
            if(!CanSpend(state,card))return false;
            if(card.kind=="creature")return p.units.Count<catalog.rules.boardSlots;
            if(card.target=="enemyUnit")return state.players.Any(x=>x.seat!=seat&&x.alive&&x.units.Count>0);
            if(card.target=="unit")return state.players.Any(x=>x.alive&&x.units.Count>0);
            if(card.effect=="swap")return p.hand.Count>=2&&state.players.Any(x=>x.seat!=seat&&x.alive&&x.handCount>0);
            return true;
        }
        public static bool ReadyToEnd(Catalog catalog,MatchState state,int seat)
        {
            return state.phase=="action"&&state.activeSeat==seat&&state.players[seat].units.All(u=>u.targetAssigned)&&
                !state.players[seat].hand.Any(h=>CanPlay(catalog,state,seat,catalog.Card(h.cardId)));
        }
        public static bool CanReact(Catalog catalog,MatchState state,int seat,CardDef card,TargetRef target)
        {
            var a=state.pending;
            if((state.phase!="reveal"&&state.phase!="qte")||state.cast==null||a==null||a.source==seat||a.responded.Contains(seat)||!state.players[seat].alive||!state.players[seat].connected||card==null||card.kind!="reaction"||target==null||!a.targets.Any(t=>t.seat==target.seat&&(t.unit??"")==(target.unit??"")))return false;
            if(catalog.Card(a.cardId).kind=="creature")return card.effect=="returnSummon"||(card.effect=="copySummon"&&!state.players[seat].units.Any(u=>u.slot==a.slot));
            bool damage=a.kind=="damage"||a.kind=="areaDamage";
            switch(card.effect)
            {
                case "reduce":case "reflect":return damage&&target.seat==seat;
                case "rescue":return damage;
                case "deny":return target.seat==seat&&(damage||new[]{"stun","swap","bounce","heal"}.Contains(a.kind));
                default:return false;
            }
        }
    }
}
