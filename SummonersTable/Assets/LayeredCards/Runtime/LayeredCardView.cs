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
        public Text title,type,role,stats,description,flavor,qteLabel;
        public Image[] qteSymbols;
        Material material;LayeredCardArt art;
        public Material ArtMaterial=>material;
        public void Apply(ConfigBundle bundle,LayeredCardsConfig draft=null)
        {
            if(string.IsNullOrEmpty(cardName))return;
            var catalog=bundle.Catalog();var card=catalog.cards.SingleOrDefault(c=>c.name==cardName);
            if(card==null)throw new InvalidOperationException("Card name not found in cards.json: "+cardName);
            art=(draft??bundle.layeredCards).Find(cardName);
            if(art==null)throw new InvalidOperationException("Layered illustration not found for "+cardName);
            if(material==null)material=LayeredArtMaterial.Create();
            artwork.texture=Texture2D.whiteTexture;artwork.material=material;
            LayeredArtMaterial.Configure(material,art,artwork.rectTransform.rect.width/artwork.rectTransform.rect.height);
            title.text=card.name;
            var color=catalog.typeColors.First(c=>c.id==card.kind);ColorUtility.TryParseHtmlString(color.hex,out var tint);
            type.text=color.name.ToUpperInvariant();type.color=new Color(.025f,.055f,.065f);accent.color=tint;
            if(titleFill!=null){titleFill.color=tint;title.color=type.color;}
            bool creature=card.kind=="creature";
            role.text=creature?card.faction+" · "+card.role:"";role.gameObject.SetActive(creature);roleAccent.gameObject.SetActive(creature);
            if(creature){ColorUtility.TryParseHtmlString(catalog.roleColors.First(c=>c.name==card.role).hex,out var roleTint);roleAccent.color=roleTint;role.color=roleTint;}
            stats.text=creature?$"АТАКА  {card.attack}     ЗДОРОВЬЕ  {card.health}":card.kind=="reaction"?"РЕАКЦИЯ · БЕЗ QTE":"";
            description.text=CardRulesText.For(card,CardPresentationContext.Options);
            flavor.text=card.flavor;
            SetQteCost(card.kind=="reaction"?0:card.qte);
            SetLook(Vector2.zero);
        }
        // Recolour the entire row whenever modifiers change the effective cost.
        public void SetQteCost(int cost)
        {
            cost=Mathf.Max(0,cost);qteLabel.text=cost==0?"БЕЗ QTE":"QTE  "+cost;
            Color color=CardView.GetQteColor(cost);qteLabel.color=color;
            for(int i=0;i<qteSymbols.Length;i++){qteSymbols[i].gameObject.SetActive(i<cost);qteSymbols[i].color=color;}
        }
        public void SetLook(Vector2 look){if(material!=null)LayeredArtMaterial.View(material,Vector2.ClampMagnitude(look,1.4f));}
        void OnEnable(){if(!string.IsNullOrEmpty(cardName)&&artwork!=null){try{Apply(LayeredCardData.Current);}catch(Exception e){Debug.LogWarning(e.Message,this);}}}
        void OnDestroy(){if(material!=null){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}}
    }
    public static class LayeredCardData
    {
        static ConfigBundle cached;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset(){cached=null;}
        public static ConfigBundle Current=>Application.isPlaying&&ConfigRuntime.Current!=null?ConfigRuntime.Current:(cached??=ConfigBundle.Read(ConfigRuntime.DirectoryPath));
        public static void Reload(){cached=ConfigBundle.Read(ConfigRuntime.DirectoryPath);}
    }
}
