using System;
using System.IO;
using UnityEngine;

namespace SummonersTable
{
    [Serializable] public sealed class CameraModeSettings
    {
        public string name;
        public float distance,height,focusHeight=1.2f,fieldOfView=55,yawLimit=30,downLimit=10,upLimit=10;
    }
    [Serializable] public sealed class PresentationData
    {
        public float lookSensitivity=3, cameraSmoothing=8, hoverScale=1.1f, hoverTilt=7, cardFlightSeconds=.5f;
        public int initialCameraMode=1;
        public CameraModeSettings[] cameraModes={
            new CameraModeSettings{name="От первого лица",distance=6.3f,height=2.3f,yawLimit=90,downLimit=45,upLimit=90,fieldOfView=66},
            new CameraModeSettings{name="Над головой",distance=8.4f,height=7,fieldOfView=60},
            new CameraModeSettings{name="Над столом",distance=2.4f,height=18,fieldOfView=57}
        };
        public void Validate()
        {
            if(cameraModes==null||cameraModes.Length!=3)throw new ArgumentException("Exactly three camera modes required.");
            if(!Finite(lookSensitivity)||lookSensitivity<=0||lookSensitivity>20||!Finite(cameraSmoothing)||cameraSmoothing<=0)throw new ArgumentException("Invalid camera sensitivity/smoothing.");
            if(!Finite(hoverScale)||hoverScale<1||hoverScale>1.5f||!Finite(hoverTilt)||Mathf.Abs(hoverTilt)>30||!Finite(cardFlightSeconds)||cardFlightSeconds<0||cardFlightSeconds>1.5f)throw new ArgumentException("Invalid card presentation settings.");
            if(initialCameraMode<0||initialCameraMode>2)throw new ArgumentException("Invalid initial camera mode.");
            foreach(var m in cameraModes)
                if(m==null||!Finite(m.distance)||!Finite(m.height)||!Finite(m.focusHeight)||!Finite(m.fieldOfView)||!Finite(m.yawLimit)||!Finite(m.downLimit)||!Finite(m.upLimit)||m.distance<0||m.height<1.1f||m.fieldOfView<20||m.fieldOfView>110||m.yawLimit<0||m.yawLimit>180||m.downLimit<0||m.downLimit>90||m.upLimit<0||m.upLimit>90)
                    throw new ArgumentException("Invalid camera mode.");
        }
        static bool Finite(float f){return !float.IsNaN(f)&&!float.IsInfinity(f);}
    }
    [CreateAssetMenu(menuName="Summoners Table/Presentation settings")]
    public sealed class PresentationSettings : ScriptableObject
    {
        public PresentationData data=new PresentationData();
        [Tooltip("Optional replacement assets from Audio/Card_Game. Synthesized previews are used until assigned.")]
        public AudioClip cardHover,invalidAction,attack,hit,music;
        public GameObject attackEffect,impactEffect,deathEffect;
        public void ImportJson(string json){var next=JsonUtility.FromJson<PresentationData>(json);if(next==null)throw new ArgumentException("Empty settings.");next.Validate();data=next;}
        public string ToJson(){data.Validate();return JsonUtility.ToJson(data,true);}
    }
}
