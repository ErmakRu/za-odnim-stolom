using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    // Authored HUD object: animation never changes its saved position.
    public sealed class UrgentCountdown : MonoBehaviour
    {
        public Text number;
        public RectTransform animatedDigit;
        public Color normal = new Color(1f,.8f,.35f), urgent = new Color(1f,.38f,.2f);
        [Range(.1f,.8f)] public float bounceDuration=.42f;
        int previous; string context=""; float pulseStart;
        public float qteGap=18;
        Vector3 authoredPosition;readonly Vector3[] corners=new Vector3[4];
        void Awake(){authoredPosition=transform.localPosition;}
        public void PlaceBelowQte(RectTransform panel,bool qte)
        {
            transform.localPosition=authoredPosition;
            if(!qte||panel==null||!panel.gameObject.activeInHierarchy)return;
            panel.GetWorldCorners(corners);
            var parent=(RectTransform)transform.parent;
            var canvas=panel.GetComponentInParent<Canvas>();
            var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            var screen=RectTransformUtility.WorldToScreenPoint(camera,corners[0]);
            var ownCanvas=GetComponentInParent<Canvas>();
            var ownCamera=ownCanvas.renderMode==RenderMode.ScreenSpaceOverlay?null:ownCanvas.worldCamera;
            Vector2 local;RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,screen,ownCamera,out local);
            var rect=(RectTransform)transform;var position=authoredPosition;
            position.y=local.y-qteGap-rect.rect.height*(1-rect.pivot.y);transform.localPosition=position;
        }
        public static int Seconds(MatchState state,int seat,double clock,bool allowed)
        {
            if(!allowed||state==null)return 0;
            double remaining;
            if(state.phase=="action"&&state.activeSeat==seat)remaining=state.deadline-clock;
            else if(state.phase=="qte"&&state.qte!=null&&state.qte.owner==seat)remaining=state.qte.deadline-clock;
            else return 0;
            return remaining>0&&remaining<10?Mathf.CeilToInt((float)remaining):0;
        }
        public void Present(MatchState state,int seat,double clock,bool allowed,float now)
        {
            int seconds=Seconds(state,seat,clock,allowed);
            string nextContext=state==null?"":state.matchId+":"+state.turnNumber+":"+state.phase;
            if(seconds==0){number.enabled=false;previous=0;context=nextContext;animatedDigit.localScale=Vector3.one;animatedDigit.localRotation=Quaternion.identity;return;}
            if(seconds!=previous||context!=nextContext){pulseStart=now;previous=seconds;context=nextContext;}
            number.enabled=true;number.text=seconds.ToString();number.color=seconds<=3?urgent:normal;
            float t=Mathf.Clamp01((now-pulseStart)/bounceDuration);
            float bounce=Mathf.Sin(t*Mathf.PI*2)*Mathf.Pow(1-t,2);
            animatedDigit.localScale=new Vector3(1+.25f*bounce,1+.38f*(1-t)-.18f*bounce,1);
            animatedDigit.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*Mathf.PI*3)*(1-t)*8);
        }
        void OnDisable(){previous=0;if(number!=null)number.enabled=false;}
    }
}
