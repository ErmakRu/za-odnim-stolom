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
        public Text title,type,role,faction,stats,description,flavor,qteLabel,restrictions;
        public Image statsBackground;
        public Image factionFill,roleFill;
        public RectTransform attackBadge,healthBadge;
        public Text attackValue,healthValue;
        public RectTransform artFrame,artGrain;
        public int rulesFontSize=46,limitsFontSize=34;
        public float artTop=397,maximumArtHeight=374,minimumArtHeight=196,rulesBottom=-440;
        public float RulesHeight{get;private set;}
        public bool runtimeMode;
        public Image[] qteSymbols;
        Material material;LayeredCardArt art;CardDef definition;Texture flatArt;bool? limited;bool flatMaterial;string lastStats;
        CardArtFraming framing;
        public Material ArtMaterial=>material;
        public void Apply(ConfigBundle bundle,LayeredCardsConfig draft=null)
        {
            if(string.IsNullOrEmpty(cardName))return;
            var catalog=bundle.Catalog();var card=catalog.cards.SingleOrDefault(c=>c.name==cardName);
            if(card==null)throw new InvalidOperationException("Card name not found in cards.json: "+cardName);
            var visuals=draft??bundle.layeredCards;
            ApplyCard(card,catalog,visuals.Find(cardName),visuals.Frame(cardName));
        }
        public void ApplyCard(CardDef card,Catalog catalog,LayeredCardArt layers,CardArtFraming frame=null)
        {
            framing=frame??LayeredCardData.Current.layeredCards.Frame(card.name);
            definition=card;art=layers;flatArt=ConfigRuntime.Artwork(card);artwork.texture=flatArt;
            title.text=card.name;
            var color=catalog.typeColors.First(c=>c.id==card.kind);ColorUtility.TryParseHtmlString(color.hex,out var tint);
            bool typeBand=card.kind=="reaction"||card.kind=="spell";
            type.text=typeBand?color.name.ToUpperInvariant():"";type.gameObject.SetActive(typeBand);accent.gameObject.SetActive(typeBand);accent.color=tint;type.color=new Color(.025f,.055f,.065f);
            if(titleFill!=null){titleFill.color=tint;title.color=type.color;}
            bool creature=card.kind=="creature";
            role.text=creature?card.role:"";role.gameObject.SetActive(creature);roleAccent.gameObject.SetActive(false);
            if(faction!=null){faction.text=creature?card.faction:"";faction.gameObject.SetActive(creature);}
            if(creature)
            {
                ColorUtility.TryParseHtmlString(catalog.roleColors.First(c=>c.name==card.role).hex,out var roleTint);role.color=roleTint;
                if(faction!=null){ColorUtility.TryParseHtmlString(catalog.factionColors.First(c=>c.name==card.faction).hex,out var factionTint);faction.color=factionTint;}
            }
            if(roleFill!=null){roleFill.gameObject.SetActive(creature);roleFill.color=role.color;role.color=ChipInk(roleFill.color);}
            if(factionFill!=null){factionFill.gameObject.SetActive(creature);factionFill.color=faction.color;faction.color=ChipInk(factionFill.color);}
            stats.text=creature?$"АТК {card.attack}   HP {card.health}":"";
            stats.gameObject.SetActive(creature);if(statsBackground!=null)statsBackground.gameObject.SetActive(creature);FitStats();
            if(attackBadge!=null)attackBadge.gameObject.SetActive(creature);
            if(healthBadge!=null)healthBadge.gameObject.SetActive(creature);
            ApplyRules();
            flavor.text=card.flavor;flavor.gameObject.SetActive(false);
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
                description.fontSize=rulesFontSize;restrictions.fontSize=limitsFontSize;
                const float row=48;
                float Height()=>description.preferredHeight+(notes.Length>0?restrictions.preferredHeight+16:0);
                // Preserve the larger letters; borrow space from art instead of shrinking the copy.
                while(Height()>artTop-minimumArtHeight-row-12-rulesBottom&&description.fontSize>44)description.fontSize--;
                RulesHeight=Height();float artHeight=Mathf.Clamp(artTop-row-12-rulesBottom-RulesHeight,minimumArtHeight,maximumArtHeight);
                Place(artwork.rectTransform,0,artTop-artHeight*.5f,590,artHeight);
                if(artFrame!=null){artFrame.gameObject.SetActive(true);Place(artFrame,0,artTop-artHeight*.5f,590,artHeight);}
                if(artGrain==null)artGrain=transform.Find("Art paper grain") as RectTransform;
                if(artGrain!=null)Place(artGrain,0,artTop-artHeight*.5f,590,artHeight);
                Place(qteLabel.rectTransform,194,artTop-26,188,38);
                for(int i=0;i<qteSymbols.Length;i++)Place(qteSymbols[i].rectTransform,-267+(i%10)*33,artTop-26-(i/10)*35,28,30);
                float edge=artTop-artHeight;
                Place(statsBackground.rectTransform,0,edge+27,statsBackground.rectTransform.rect.width,statsBackground.rectTransform.rect.height);Place(stats.rectTransform,0,edge+27,stats.rectTransform.rect.width,stats.rectTransform.rect.height);
                if(attackBadge!=null)Place(attackBadge,-74,edge+34,144,64);
                if(healthBadge!=null)Place(healthBadge,74,edge+34,144,64);
                if(factionFill!=null)Place(factionFill.rectTransform,-148.5f,edge-24,293,40);
                if(roleFill!=null)Place(roleFill.rectTransform,148.5f,edge-24,293,40);
                if(faction!=null)Place(faction.rectTransform,-148.5f,edge-24,281,40);
                Place(role.rectTransform,148.5f,edge-24,281,40);
                Place(accent.rectTransform,0,edge-24,590,40);Place(type.rectTransform,0,edge-24,562,36);
                float top=edge-row-12,height=description.preferredHeight;
                Place(description.rectTransform,0,top,548,height+3);description.rectTransform.pivot=new Vector2(.5f,1);
                Place(restrictions.rectTransform,0,top-height-16,548,Mathf.Max(48,restrictions.preferredHeight+4));restrictions.rectTransform.pivot=new Vector2(.5f,1);
                if(artwork.material==null||flatMaterial)SetFlatArt();
            }
        }
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);}
        static Color ChipInk(Color fill)=>fill.grayscale>.48f?new Color(.025f,.045f,.055f):new Color(.98f,.97f,.92f);
        public void FitStats()
        {
            if(statsBackground==null||!stats.gameObject.activeSelf)return;
            if(attackValue!=null&&healthValue!=null)
            {
                // Keep the legacy value as a data bridge for existing CardView references.
                stats.enabled=false;statsBackground.color=Color.clear;lastStats=stats.text;
                var values=System.Text.RegularExpressions.Regex.Matches(stats.text,@"-?\d+");
                attackValue.text=values.Count>0?values[0].Value:(definition?.attack??0).ToString();
                healthValue.text=values.Count>1?values[1].Value:(definition?.health??0).ToString();return;
            }
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
            artwork.uvRect=(framing??new CardArtFraming()).Crop(window,source);
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
        // Gallery-only diagnostic; default zero leaves game materials unchanged.
        public void SetPreviewLayer(int layer)
        {
            if(material==null||flatMaterial)return;
            material.SetFloat("_PreviewLayer",Mathf.Clamp(layer,0,2));
            var draw=artwork.materialForRendering;if(draw!=material)draw.SetFloat("_PreviewLayer",Mathf.Clamp(layer,0,2));
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
