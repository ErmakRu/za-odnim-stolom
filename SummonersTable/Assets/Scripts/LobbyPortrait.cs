using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed class LobbyPortrait : MonoBehaviour
    {
        public Camera portraitCamera;public HeroActor actor;public RawImage image;
        RenderTexture texture;
        void OnEnable()
        {
            if(portraitCamera==null||image==null)return;
            if(texture==null){texture=new RenderTexture(512,640,24,RenderTextureFormat.ARGB32);texture.antiAliasing=2;texture.Create();}
            image.texture=texture;portraitCamera.targetTexture=texture;portraitCamera.gameObject.SetActive(true);
        }
        void OnDisable(){if(portraitCamera!=null)portraitCamera.gameObject.SetActive(false);}
        void OnDestroy(){if(texture!=null){texture.Release();Destroy(texture);}}
        public void Present(LobbyMember member)
        {
            actor.gameObject.SetActive(member!=null);image.color=member==null?Color.clear:Color.white;
            if(member!=null){actor.Configure(member.heroId,member.outfit,member.palette,false);actor.Idle(false);}
        }
    }
}
