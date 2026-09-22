using System.Collections.Generic;
using System.Linq;
namespace SummonersTable
{
    public static class RematchRules
    {
        public static bool Ready(PlayerState p,LobbyMember m,string matchId){return p!=null&&p.connected&&(p.postMatchChoice=="again"||p.postMatchChoice=="deck"&&m.ready&&m.readyMatch==matchId);}
        public static bool CanRestart(MatchState s,IList<LobbyMember> members){return s!=null&&s.phase=="matchEnd"&&members.Count>=2&&members.Count<=4&&members.All(m=>Ready(s.players.Find(p=>p.id==m.id),m,s.matchId));}
        public static bool AcceptNext(MatchState current,WireMessage incoming){if(current==null)return true;if(current.matchId==incoming.matchId)return incoming.state.revision>=current.revision;return current.phase=="matchEnd"&&incoming.previousMatchId==current.matchId;}
    }
}
