using System.Linq;
using Michsky.UI.MTP;
using UnityEngine;
namespace SummonersTable
{
    public sealed class MotionAnnouncements : MonoBehaviour
    {
        public StyleManager style;string last="";
        public void Present(string page,MatchState state,int members)
        {
            string key="",text="";
            if(page=="local"||page=="steam"&&members>0){key="lobby-"+members;text="СОБИРАЕМСЯ ЗА СТОЛОМ";}
            else if(page=="game"&&state!=null)
            {
                if(state.phase=="matchEnd"){key="finish-"+state.matchId;text="ИГРА ЗАВЕРШЕНА";}
                else if(state.phase=="roundEnd"){key="roundEnd-"+state.matchId+state.round;text="РАУНД ЗАВЕРШЁН";}
                else{key="turn-"+state.matchId+state.turnNumber;text=(state.turnNumber==1?"ИГРА НАЧИНАЕТСЯ":"ХОД "+state.players[state.activeSeat].name).ToUpperInvariant();}
            }
            if(key==last)return;last=key;if(key==""){style.Stop();return;}
            foreach(var item in style.textItems.Where(t=>t!=null)){item.text=text;item.UpdateText();}
            style.Play();
        }
        public void Preview(string text){foreach(var item in style.textItems){item.text=text;item.UpdateText();}style.Play();}
    }
}
