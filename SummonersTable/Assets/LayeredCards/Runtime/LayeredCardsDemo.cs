using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed class LayeredCardsDemo:MonoBehaviour
    {
        public LayeredCardView[] cards;
        public Text hint;
        public bool autoRotate=true;
        public Vector2 look;
        [Tooltip("Only this preview. Zero uses the card's JSON cost; balance is never changed.")]
        public int[] previewQteCosts;
        public void Pose(float time)
        {
            look=new Vector2(Mathf.Sin(time*.83f),Mathf.Sin(time*1.17f)*.7f);
            ApplyLook();
        }
        public void ApplyLook()
        {
            for(int i=0;i<cards.Length;i++){var card=cards[i];card.transform.localRotation=Quaternion.Euler(-look.y*17,look.x*23,-look.x*2);card.SetLook(look);if(previewQteCosts!=null&&i<previewQteCosts.Length&&previewQteCosts[i]>0)card.SetQteCost(previewQteCosts[i]);}
            if(hint!=null)hint.text=$"НАКЛОН   X {look.x*23:+0;-0;0}°   Y {look.y*17:+0;-0;0}°     ·     ФОН + ЕДИНЫЙ ПЛАН";
        }
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Space))autoRotate=!autoRotate;
            if(Input.GetMouseButton(0)){autoRotate=false;look=new Vector2(Mathf.Clamp((Input.mousePosition.x/Screen.width-.5f)*2,-1,1),Mathf.Clamp((Input.mousePosition.y/Screen.height-.5f)*2,-1,1));ApplyLook();}
            else if(autoRotate)Pose(Time.time);
        }
    }
}
