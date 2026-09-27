using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed class LocalHeroHud : MonoBehaviour
    {
        public RawImage portrait;
        public Image healthFill;
        public Text healthText,scoreText;
        public void Present(PlayerState player,int maximumHp)
        {
            healthFill.fillAmount=Mathf.Clamp01(player.hp/(float)Mathf.Max(1,maximumHp));
            healthText.text="HP  "+player.hp+" / "+maximumHp;
            scoreText.text=new string('●',Mathf.Clamp(player.score,0,12));
        }
    }
}
