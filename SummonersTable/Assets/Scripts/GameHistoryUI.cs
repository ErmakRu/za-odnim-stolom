using UnityEngine;
namespace SummonersTable
{
    public sealed partial class GameApp
    {
        readonly HistoryArchive journal=new HistoryArchive();
        bool historyOpen;string journalMatch="";
        Vector2 LogicalPointer(Vector2 screen)
        {
            float s=Mathf.Min(Screen.width/W,Screen.height/H);
            return new Vector2((screen.x-(Screen.width-W*s)/2)/s,(Screen.height-screen.y-(Screen.height-H*s)/2)/s);
        }
        void UpdateJournal()
        {
            if(journalMatch!=(state?.matchId??"")){journalMatch=state?.matchId??"";historyOpen=false;}
            journal.Observe(state);
        }
    }
}
