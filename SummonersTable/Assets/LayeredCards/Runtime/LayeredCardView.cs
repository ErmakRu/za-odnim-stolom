using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SummonersTable
{
    // The base prefab contains layout references only. An instance needs only the JSON card name.
    [ExecuteAlways]
    public sealed class LayeredCardView:MonoBehaviour
    {
        public string cardName="";
        public RawImage artwork;
        public Image accent,roleAccent,titleFill;
        public Text title,type,role,stats,description,flavor,qteLabel,restrictions;
        public Image statsBackground;
        public bool runtimeMode;
        public Image[] qteSymbols;
        Material material;LayeredCardArt art;CardDef definition;Texture flatArt;bool? limited;bool flatMaterial;string lastStats;
        public Material ArtMaterial=>material;
        public void Apply(ConfigBundle bundle,LayeredCardsConfig draft=null)
        {
            if(string.IsNullOrEmpty(cardName))return;
            var catalog=bundle.Catalog();var card=catalog.cards.SingleOrDefault(c=>c.name==cardName);
            if(card==null)throw new InvalidOperationException("Card name not found in cards.json: "+cardName);
            ApplyCard(card,catalog,(draft??bundle.layeredCards).Find(cardName));
        }
        public void ApplyCard(CardDef card,Catalog catalog,LayeredCardArt layers)
        {
            definition=card;art=layers;flatArt=ConfigRuntime.Artwork(card);artwork.texture=flatArt;
            title.text=card.name;
            var color=catalog.typeColors.First(c=>c.id==card.kind);ColorUtility.TryParseHtmlString(color.hex,out var tint);
            type.text=color.name.ToUpperInvariant();type.color=new Color(.025f,.055f,.065f);accent.color=tint;
            if(titleFill!=null){titleFill.color=tint;title.color=type.color;}
            bool creature=card.kind=="creature";
            role.text=creature?card.faction+" · "+card.role:"";role.gameObject.SetActive(creature);roleAccent.gameObject.SetActive(creature);
            if(creature){ColorUtility.TryParseHtmlString(catalog.roleColors.First(c=>c.name==card.role).hex,out var roleTint);roleAccent.color=roleTint;role.color=roleTint;}
            stats.text=creature?$"АТК {card.attack}   HP {card.health}":"";
            stats.gameObject.SetActive(creature);if(statsBackground!=null)statsBackground.gameObject.SetActive(creature);FitStats();
            ApplyRules();
            flavor.text=card.flavor;
            SetQteCost(card.kind=="reaction"?0:card.qte);
            if(!runtimeMode)SetGameArt(true,Vector2.zero,null);
        }
        public void ApplyRules()
        {
            if(definition==null)return;
            CardRulesText.Split(definition,CardPresentationContext.Options,out var main,out var notes);
            description.text=main;limited=CardPresentationContext.Options.limitPower;
            if(restrictions!=null)
            {
                restrictions.text=notes;restrictions.gameObject.SetActive(notes.Length>0);
                description.fontSize=29;restrictions.fontSize=25;
                while(description.preferredHeight+(notes.Length>0?restrictions.preferredHeight+18:0)>292&&description.fontSize>22){description.fontSize--;restrictions.fontSize=Mathf.Max(19,description.fontSize-4);}
                float height=description.preferredHeight;
                description.rectTransform.sizeDelta=new Vector2(534,height+3);
                var r=restrictions.rectTransform;r.anchoredPosition=new Vector2(0,-106-height-18);r.sizeDelta=new Vector2(534,Mathf.Max(48,restrictions.preferredHeight+4));
            }
        }
        public void FitStats()
        {
            if(statsBackground==null||!stats.gameObject.activeSelf)return;
            float width=Mathf.Min(534,stats.preferredWidth+32),height=stats.preferredHeight+14;
            statsBackground.rectTransform.sizeDelta=new Vector2(width,height);stats.rectTransform.sizeDelta=new Vector2(width-20,height-8);lastStats=stats.text;
        }
        public void SetGameArt(bool enabled,Vector2 look,Material fallback)
        {
            if(!enabled){artwork.material=null;SetFlatArt();return;}
            bool simple=art==null;if(simple&&fallback==null){artwork.material=null;SetFlatArt();return;}
            if(material==null||flatMaterial!=simple){Release();material=simple?new Material(fallback):LayeredArtMaterial.Create();flatMaterial=simple;}
            if(simple){material.SetFloat("_Depth",ConfigRuntime.Current?.presentation.cardDepth??.03f);material.SetFloat("_Foil",ConfigRuntime.Current?.presentation.cardFoil??.2f);SetFlatArt();}
            else{LayeredArtMaterial.Configure(material,art,artwork.rectTransform.rect.width/artwork.rectTransform.rect.height);artwork.texture=Texture2D.whiteTexture;artwork.uvRect=new Rect(0,0,1,1);}
            artwork.material=material;
            // UI masks use a stencil copy. Keep its JSON parameters and movement
            // synchronized without overwriting the stencil state itself.
            var draw=artwork.materialForRendering;
            if(draw!=material)
            {
                if(simple){draw.SetFloat("_Depth",material.GetFloat("_Depth"));draw.SetFloat("_Foil",material.GetFloat("_Foil"));}
                else LayeredArtMaterial.Configure(draw,art,artwork.rectTransform.rect.width/artwork.rectTransform.rect.height);
            }
            SetLook(look);
        }
        void SetFlatArt()
        {
            artwork.texture=flatArt;artwork.uvRect=new Rect(0,0,1,1);if(flatArt==null)return;
            float window=artwork.rectTransform.rect.width/artwork.rectTransform.rect.height,source=(float)flatArt.width/flatArt.height;
            // Centre crop the flat fallback instead of distorting the original art.
            float width=Mathf.Min(1,window/source),height=Mathf.Min(1,source/window);
            artwork.uvRect=new Rect((1-width)*.5f,(1-height)*.5f,width,height);
        }
        // Recolour the entire row whenever modifiers change the effective cost.
        public void SetQteCost(int cost)
        {
            cost=Mathf.Max(0,cost);qteLabel.text=cost==0?"БЕЗ QTE":"QTE  "+cost;
            Color color=CardView.GetQteColor(cost);qteLabel.color=color;
            for(int i=0;i<qteSymbols.Length;i++){qteSymbols[i].gameObject.SetActive(i<cost);qteSymbols[i].color=color;}
        }
        public void SetLook(Vector2 look)
        {
            if(material==null||artwork.material!=material)return;look=Vector2.ClampMagnitude(look,1.4f);
            LayeredArtMaterial.View(material,look);var draw=artwork.materialForRendering;if(draw!=material)LayeredArtMaterial.View(draw,look);
        }
        void LateUpdate(){if(definition!=null&&limited!=CardPresentationContext.Options.limitPower)ApplyRules();if(lastStats!=stats.text)FitStats();}
        void OnEnable(){if(!runtimeMode&&!string.IsNullOrEmpty(cardName)&&artwork!=null){try{Apply(LayeredCardData.Current);}catch(Exception e){Debug.LogWarning(e.Message,this);}}}
        void Release(){if(material!=null){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}material=null;}
        void OnDestroy(){Release();}
    }
    public static class LayeredCardData
    {
        static ConfigBundle cached;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset(){cached=null;}
        public static ConfigBundle Current=>Application.isPlaying&&ConfigRuntime.Current!=null?ConfigRuntime.Current:(cached??=ConfigBundle.Read(ConfigRuntime.DirectoryPath));
        public static void Reload(){cached=ConfigBundle.Read(ConfigRuntime.DirectoryPath);}
    }
}
