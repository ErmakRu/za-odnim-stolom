using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed class PlayerStatusView : MonoBehaviour
    {
        public RectTransform nameAnchor,healthAnchor;
        public Text nickname,healthNumber;
        public Image healthFill,healthTrail;
        public Vector3 nameOffset=new Vector3(0,1.25f,0);
        [Range(0,1)]public float healthBetweenHeroAndSlots=.5f;
        public float healthHeight=1.25f;
        public float minimumNameGap=48;
        public void Present(PlayerState player,MatchState state,TableBoard board,int maxHp)
        {
            nickname.text=player.name;nickname.color=TableBoard.SeatColors[player.seat];healthNumber.text=player.hp+" / "+maxHp;
            healthFill.fillAmount=player.hp/(float)maxHp;healthFill.color=TableBoard.SeatColors[player.seat];healthTrail.fillAmount=board.DisplayHp("hero-"+player.seat,player.hp)/maxHp;
            var hero=TableBoard.HeroPosition(player.seat,state.players.Count);Place(nameAnchor,board.ViewCamera,hero+nameOffset);
            var p=Vector3.Lerp(hero,TableBoard.SlotPosition(player.seat,2,state.players.Count),healthBetweenHeroAndSlots);p.y=healthHeight;Place(healthAnchor,board.ViewCamera,p);
            // A top-down projection flattens vertical offsets: keep the name above the head in screen space too.
            var head=board.ViewCamera.WorldToScreenPoint(hero+nameOffset);
            var body=board.ViewCamera.WorldToScreenPoint(hero);
            float uiScale=nameAnchor.lossyScale.y;
            if(Mathf.Abs(head.y-body.y)<minimumNameGap*uiScale)nameAnchor.anchoredPosition+=Vector2.up*(minimumNameGap-Mathf.Abs(head.y-body.y)/uiScale);
        }
        static void Place(RectTransform r,Camera camera,Vector3 p){var s=camera.WorldToScreenPoint(p);r.gameObject.SetActive(s.z>0);RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)r.parent,s,null,out var local);r.anchoredPosition=local;}
    }
}
