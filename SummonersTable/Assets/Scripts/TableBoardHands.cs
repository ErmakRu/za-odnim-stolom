using System.Collections.Generic;
using UnityEngine;

namespace SummonersTable
{
    public sealed partial class TableBoard
    {
        readonly Dictionary<int,List<GameObject>> handBacks=new Dictionary<int,List<GameObject>>();
        WorldHandFanSettings handFanSettings;
        public Transform VisibleHandCard(int seat,int index)=>handBacks[seat][index].transform;
        public int VisibleHandCount(int seat){return handBacks.TryGetValue(seat,out var cards)?cards.Count:0;}
        void SyncHandBacks(MatchState state,int viewer)
        {
            if(handFanSettings==null)handFanSettings=GetComponent<WorldHandFanSettings>();
            foreach(var pair in handBacks)if(pair.Key>=state.players.Count){foreach(var card in pair.Value)Destroy(card);pair.Value.Clear();}
            foreach(var player in state.players)
            {
                handFanSettings=Seat(player.seat)?.fan??GetComponent<WorldHandFanSettings>();
                if(!handBacks.TryGetValue(player.seat,out var cards)){cards=new List<GameObject>();handBacks[player.seat]=cards;}
                int wanted=player.connected&&player.alive?Mathf.Clamp(player.handCount,0,catalog.rules.handLimit):0;
                while(cards.Count>wanted){Destroy(cards[cards.Count-1]);cards.RemoveAt(cards.Count-1);}
                while(cards.Count<wanted)cards.Add(Card("Visible hand back "+player.seat+" / "+cards.Count,"card_back",Vector3.zero,Quaternion.identity,handFanSettings.cardSize));
                // The semicircle belongs to the seat, never to the camera. Both sides use
                // the back texture: turning the camera cannot disclose a private hand.
                for(int i=0;i<cards.Count;i++)
                {
                    cards[i].transform.GetChild(0).localScale=new Vector3(handFanSettings.cardSize.x,handFanSettings.cardSize.y,1);
                    handFanSettings.Pose(i,cards.Count,HeroPosition(player.seat,state.players.Count),Away(player.seat,state.players.Count),out var position,out var rotation);
                    cards[i].transform.SetPositionAndRotation(position,rotation);
                    cards[i].SetActive(!(player.seat==viewer&&CameraRig.Mode==0));
                }
            }
        }
    }
}
