using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed class LocalHeroHud : MonoBehaviour
    {
        public RawImage portrait;
        public UrgentCountdown countdown;
        public Image healthFill;
        public Text healthText,scoreText;
        public void Present(PlayerState player,int maximumHp)
        {
            var bar=healthFill.GetComponentInParent<HeroHealthBar>();
            if(bar!=null)bar.Present(player.hp,maximumHp,player.hp);
            else healthFill.fillAmount=Mathf.Clamp01(player.hp/(float)Mathf.Max(1,maximumHp));
            healthText.text="HP  "+player.hp+" / "+maximumHp;healthText.color=Color.black;
            scoreText.text=new string('●',Mathf.Clamp(player.score,0,12));
        }
    }
}
