using UnityEngine;
namespace SummonersTable
{
    public sealed class PlayerSeatView:MonoBehaviour
    {
        public Transform chair,heroTarget,body,handAnchor,nameAnchor,healthAnchor;
        public HeroActor avatar;public PlayerStatusView status;
        public Transform[] slots;
        public WorldHandFanSettings fan;
        public void Assign(int seat)
        {
            foreach(var target in GetComponentsInChildren<BoardTarget>(true))target.seat=seat;
            for(int i=0;i<slots.Length;i++){var target=slots[i].GetComponent<BoardTarget>();target.slot=i;target.kind="slot";}
            if(status!=null){status.seatOwned=true;status.seatNamePoint=nameAnchor;status.seatHealthPoint=healthAnchor;}
        }
        public void Apply(WorldConfig world){world.chair.Apply(chair);world.avatar.Apply(avatar.transform);world.heroTarget.Apply(heroTarget);avatar.seatedHipHeight=world.avatarHipHeight;for(int i=0;i<slots.Length;i++)world.slots[i].Apply(slots[i]);}
    }
}
