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
        CardView card;Material full,compact,world,originalWorld,originalFull,originalCompact;bool applied,layered;
        LayeredCardArt lastArt;float fullAspect,compactAspect;
        void Awake(){card=GetComponent<CardView>();originalWorld=card.worldArtwork.sharedMaterial;originalFull=card.fullArtwork.material;originalCompact=card.compactArtwork.material;}
        void LateUpdate()
        {
            bool enabled=CardPresentationContext.Options.cards3D;
            var art=ConfigRuntime.Current?.layeredCards?.Find(card.definition.name);
            if(full!=null&&layered!=(art!=null)){if(applied)Restore();ReleaseMaterials();}
            if(enabled&&!applied)
            {
                layered=art!=null;
                if(full==null){full=layered?LayeredArtMaterial.Create():new Material(uiMaterial);compact=layered?LayeredArtMaterial.Create():new Material(uiMaterial);world=layered?LayeredArtMaterial.Create(true):new Material(worldMaterial);world.mainTexture=originalWorld.mainTexture;}
                card.fullArtwork.material=full;card.compactArtwork.material=compact;card.worldArtwork.sharedMaterial=world;applied=true;
            }
            else if(!enabled&&applied){Restore();}
            if(enabled&&layered)
            {
                float a=Aspect(card.fullArtwork),b=Aspect(card.compactArtwork);
                if(lastArt!=art||a!=fullAspect||b!=compactAspect){LayeredArtMaterial.Configure(full,art,a);LayeredArtMaterial.Configure(compact,art,b);var size=card.worldArtwork.transform.localScale;LayeredArtMaterial.Configure(world,art,Mathf.Abs(size.x/Mathf.Max(.001f,size.y)));lastArt=art;fullAspect=a;compactAspect=b;}
            }
            else if(enabled&&ConfigRuntime.Available){foreach(var m in new[]{full,compact,world}){m.SetFloat("_Depth",ConfigRuntime.Current.presentation.cardDepth);m.SetFloat("_Foil",ConfigRuntime.Current.presentation.cardFoil);}}
            if(enabled){SetView(card.fullArtwork,full);SetView(card.compactArtwork,compact);}
        }
        static float Aspect(RawImage image)=>Mathf.Max(.01f,image.rectTransform.rect.width/Mathf.Max(1,image.rectTransform.rect.height));
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
        void ReleaseMaterials(){Release(full);Release(compact);Release(world);full=compact=world=null;lastArt=null;}
        static void Release(Material material){if(material!=null){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}}
        void OnDestroy(){ReleaseMaterials();}
    }
}
