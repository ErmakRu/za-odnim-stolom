using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    // Inherited from CardBase by every creature, spell and reaction variant.
    public sealed class CardDepthVisual:MonoBehaviour
    {
        public Material uiMaterial,worldMaterial;
        [Range(0,1)] public float pointerInfluence=.8f;
        public static Vector2? PreviewView;
        CardView card;Material full,compact,world,originalWorld,originalFull,originalCompact;bool applied;
        void Awake(){card=GetComponent<CardView>();originalWorld=card.worldArtwork.sharedMaterial;originalFull=card.fullArtwork.material;originalCompact=card.compactArtwork.material;}
        void LateUpdate()
        {
            bool enabled=CardPresentationContext.Options.cards3D;
            if(enabled&&!applied)
            {
                if(full==null){full=new Material(uiMaterial);compact=new Material(uiMaterial);world=new Material(worldMaterial);world.mainTexture=originalWorld.mainTexture;}
                card.fullArtwork.material=full;card.compactArtwork.material=compact;card.worldArtwork.sharedMaterial=world;applied=true;
            }
            else if(!enabled&&applied){Restore();}
            if(enabled&&ConfigRuntime.Available){foreach(var m in new[]{full,compact,world}){m.SetFloat("_Depth",ConfigRuntime.Current.presentation.cardDepth);m.SetFloat("_Foil",ConfigRuntime.Current.presentation.cardFoil);}}
            if(enabled){SetView(card.fullArtwork,full);SetView(card.compactArtwork,compact);}
        }
        void SetView(RawImage image,Material material)
        {
            if(material==null||!image.gameObject.activeInHierarchy)return;Vector2 look;
            if(PreviewView.HasValue)look=PreviewView.Value;
            else
            {
                var rect=image.rectTransform;RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,Input.mousePosition,null,out var local);
                look=new Vector2(Mathf.Clamp(local.x/Mathf.Max(1,rect.rect.width)*2,-1,1),Mathf.Clamp(local.y/Mathf.Max(1,rect.rect.height)*2,-1,1))*pointerInfluence;
            }
            material.SetVector("_ViewOffset",new Vector4(look.x,look.y,0,0));
        }
        void Restore(){card.fullArtwork.material=originalFull;card.compactArtwork.material=originalCompact;card.worldArtwork.sharedMaterial=originalWorld;applied=false;}
        void OnDestroy(){if(full!=null)Destroy(full);if(compact!=null)Destroy(compact);if(world!=null)Destroy(world);}
    }
}
