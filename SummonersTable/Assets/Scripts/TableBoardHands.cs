using System.Collections.Generic;
using UnityEngine;

namespace SummonersTable
{
    public sealed partial class TableBoard
    {
        readonly Dictionary<int,List<GameObject>> handBacks=new Dictionary<int,List<GameObject>>();
        public int VisibleHandCount(int seat){return handBacks.TryGetValue(seat,out var cards)?cards.Count:0;}
        void SyncHandBacks(MatchState state,int viewer)
        {
            foreach(var pair in handBacks)if(pair.Key>=state.players.Count){foreach(var card in pair.Value)Destroy(card);pair.Value.Clear();}
            foreach(var player in state.players)
            {
                if(!handBacks.TryGetValue(player.seat,out var cards)){cards=new List<GameObject>();handBacks[player.seat]=cards;}
                int wanted=player.connected&&player.alive?Mathf.Clamp(player.handCount,0,catalog.rules.handLimit):0;
                while(cards.Count>wanted){Destroy(cards[cards.Count-1]);cards.RemoveAt(cards.Count-1);}
                while(cards.Count<wanted)cards.Add(Card("Visible hand back "+player.seat+" / "+cards.Count,"card_back",Vector3.zero,Quaternion.identity,new Vector2(.65f,.92f)));
                // World-space cards face the viewing camera, including its overhead mode.
                // Each separate edge remains visible from every seat; no hidden face textures.
                var right=ViewCamera.transform.right;var up=ViewCamera.transform.up;
                Vector3 anchor=HeroPosition(player.seat,state.players.Count)+right*1.48f+Vector3.up*.45f;
                for(int i=0;i<cards.Count;i++)
                {
                    float offset=i-(cards.Count-1)*.5f;
                    cards[i].transform.position=anchor+right*(offset*.31f)+up*(-Mathf.Abs(offset)*.055f)-ViewCamera.transform.forward*(i*.008f);
                    cards[i].transform.rotation=ViewCamera.transform.rotation*Quaternion.Euler(0,0,-offset*5);
                    cards[i].SetActive(!(player.seat==viewer&&CameraRig.Mode==0));
                }
            }
        }
    }
}
