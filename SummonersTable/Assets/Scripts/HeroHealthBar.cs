using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed class HeroHealthBar:MonoBehaviour
    {
        public Image fill,trail;
        public Text value;
        public Color fillColor=new Color(.18f,.68f,.62f,1);
        public void Present(int hp,int maximum,float displayed)
        {
            fill.color=fillColor;fill.fillAmount=Mathf.Clamp01(hp/(float)Mathf.Max(1,maximum));
            if(trail!=null)trail.fillAmount=Mathf.Clamp01(displayed/Mathf.Max(1,maximum));
            value.color=Color.black;value.text="HP  "+hp+" / "+maximum;
        }
    }
}
