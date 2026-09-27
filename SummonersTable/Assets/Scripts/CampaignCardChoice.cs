using System;
using UnityEngine;
namespace SummonersTable
{
    public sealed class CampaignCardChoice:MonoBehaviour
    {
        public RectTransform illustration,choices;
        public Action dismiss;public bool IsOpen{get;private set;}
        float phase;int changedFrame;bool previousSide;
        public void SetOpen(bool value){if(IsOpen==value)return;IsOpen=value;changedFrame=Time.frameCount;}
        void OnEnable(){phase=0;previousSide=false;if(illustration!=null){illustration.gameObject.SetActive(true);illustration.localScale=Vector3.one;}if(choices!=null)choices.gameObject.SetActive(false);IsOpen=false;}
        void Update()
        {
            phase=Mathf.MoveTowards(phase,IsOpen?1:0,Time.unscaledDeltaTime/.32f);bool back=phase>=.5f;
            illustration.gameObject.SetActive(!back);choices.gameObject.SetActive(back);
            var active=back?choices:illustration;active.localScale=new Vector3(Mathf.Max(.02f,Mathf.Abs(1-2*phase)),1,1);
            previousSide=back;
            if(IsOpen&&Time.frameCount>changedFrame&&(Input.GetMouseButtonDown(1)||(Input.GetMouseButtonDown(0)&&!RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform,Input.mousePosition,null))))dismiss?.Invoke();
        }
    }
}
