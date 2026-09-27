using UnityEngine;
namespace SummonersTable
{
    [DisallowMultipleComponent,RequireComponent(typeof(CanvasGroup))]
    public sealed class UiEntrance:MonoBehaviour
    {
        public float duration=.24f,offset=18;
        CanvasGroup group;RectTransform rect;Vector2 origin;float elapsed;
        void Awake(){group=GetComponent<CanvasGroup>();rect=transform as RectTransform;if(rect!=null)origin=rect.anchoredPosition;}
        public void Replay(){if(!Application.isPlaying||group==null)return;elapsed=0;group.alpha=0;}
        void OnEnable(){Replay();}
        void LateUpdate(){if(!Application.isPlaying||elapsed>=duration)return;elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/Mathf.Max(.01f,duration));float e=1-Mathf.Pow(1-t,3);group.alpha=e;if(rect!=null)rect.anchoredPosition=origin+Vector2.down*offset*(1-e);}
        void OnDisable(){if(group!=null)group.alpha=1;if(rect!=null)rect.anchoredPosition=origin;}
    }
}
