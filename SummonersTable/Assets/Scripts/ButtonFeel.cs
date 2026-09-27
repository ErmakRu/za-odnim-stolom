using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace SummonersTable
{
    [DisallowMultipleComponent,RequireComponent(typeof(Button))]
    public sealed class ButtonFeel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        [Header("Response — unscaled time")]
        [Range(1,1.12f)] public float hoverScale=1.035f;
        [Range(.85f,1)] public float pressScale=.955f;
        [Min(.01f)] public float responseTime=.07f;
        public AudioClip hoverSound,clickSound;
        [Range(0,1)] public float soundVolume=.35f;
        Button button;Vector3 original;float velocity,current=1,pulseUntil;bool hover,selected,pressed;AudioSource source;
        void Awake(){button=GetComponent<Button>();original=transform.localScale;source=GetComponent<AudioSource>();}
        void OnEnable(){current=1;velocity=0;hover=selected=pressed=false;}
        bool Usable=>button!=null&&button.IsActive()&&button.IsInteractable();
        void Update()
        {
            float target=Usable?(pressed||Time.unscaledTime<pulseUntil?pressScale:hover||selected?hoverScale:1):1;
            current=Mathf.SmoothDamp(current,target,ref velocity,responseTime,Mathf.Infinity,Mathf.Min(Time.unscaledDeltaTime,.05f));
            transform.localScale=original*current;
        }
        void OnDisable(){if(!Application.isPlaying)return;transform.localScale=original;pressed=hover=selected=false;current=1;velocity=0;pulseUntil=0;}
        void Play(AudioClip clip){if(clip!=null&&source!=null){source.pitch=1;source.PlayOneShot(clip,soundVolume*UserSettings.Volume(AudioBus.Effects));}}
        public void OnPointerEnter(PointerEventData e){hover=true;if(Usable)Play(hoverSound);}
        public void OnPointerExit(PointerEventData e){hover=false;pressed=false;}
        public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left&&Usable)pressed=true;}
        public void OnPointerUp(PointerEventData e){if(e.button!=PointerEventData.InputButton.Left)return;if(pressed&&hover&&Usable)Play(clickSound);pressed=false;}
        public void OnSelect(BaseEventData e){selected=true;}
        public void OnDeselect(BaseEventData e){selected=false;pressed=false;}
        public void OnSubmit(BaseEventData e){if(Usable){pulseUntil=Time.unscaledTime+.09f;Play(clickSound);}}
    }
}
