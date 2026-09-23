using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace SummonersTable
{
    // One inheritance chain supplies all full, compact and tabletop faces.
    public sealed class CardView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public CardDef definition=new CardDef();
        public SpellEffect spellEffect;
        public GameObject fullFace,compactFace,worldFace;
        public RawImage fullArtwork,compactArtwork;
        public Image fullType,compactType,fullRole,compactRole,playableGlow,selectionFrame;
        public Text fullName,fullStats,fullRules,fullTypeText,fullRoleText,compactName,compactStats,compactTypeText;
        public Renderer worldArtwork,worldType,worldRole;
        public LineRenderer worldOutline;
        public Color playableColor=new Color(.38f,.96f,.67f),reactionPlayableColor=new Color(.84f,.63f,1);
        [NonSerialized] public Action<PointerEventData> pressed,dragged,released;
        public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)pressed?.Invoke(e);}
        public void OnDrag(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)dragged?.Invoke(e);}
        public void OnPointerUp(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)released?.Invoke(e);}
        bool? rulesLimited;
        Catalog configCatalog;
        void Start(){if(ConfigRuntime.Available)ApplyDefinition(ConfigRuntime.ActiveCatalog);}
        public void ApplyDefinition(Catalog catalog){if(configCatalog==catalog)return;var data=catalog.Card(definition.id);if(data==null)return;Import(data,catalog);configCatalog=catalog;rulesLimited=null;}
        void LateUpdate(){bool limited=CardPresentationContext.Options.limitPower;if(rulesLimited!=limited){fullRules.text=CardRulesText.For(definition,CardPresentationContext.Options);rulesLimited=limited;}}
        public void Mode(string mode){fullFace.SetActive(mode=="full");compactFace.SetActive(mode=="compact");worldFace.SetActive(mode=="world");if(worldOutline!=null)worldOutline.enabled=false;}
        public void Highlight(bool playable,bool selected){playableGlow.gameObject.SetActive(playable);selectionFrame.gameObject.SetActive(selected);playableGlow.color=UserSettings.Data.highlightColor;selectionFrame.color=UserSettings.Data.highlightColor;if(ConfigRuntime.Available){var material=ConfigRuntime.Assets.Get<Material>(ConfigRuntime.Current.ui.highlightMaterial);playableGlow.material=selectionFrame.material=material;}}
        public void Import(CardDef card,Catalog catalog)
        {
            definition=JsonUtility.FromJson<CardDef>(JsonUtility.ToJson(card));
            var type=catalog.typeColors.Find(c=>c.id==card.kind);ColorUtility.TryParseHtmlString(type.hex,out var color);
            fullType.color=compactType.color=color;fullTypeText.text=type.name+" · "+card.id;compactTypeText.text=type.name;
            fullName.text=compactName.text=card.name;fullRules.text=card.rules;fullArtwork.texture=compactArtwork.texture=ConfigRuntime.Artwork(card);
            var block=new MaterialPropertyBlock();worldArtwork.GetPropertyBlock(block);block.SetTexture("_MainTex",fullArtwork.texture);worldArtwork.SetPropertyBlock(block);
            block=new MaterialPropertyBlock();block.SetColor("_Color",color);worldType.SetPropertyBlock(block);
            bool creature=card.kind=="creature";fullRole.gameObject.SetActive(creature);fullRoleText.gameObject.SetActive(creature);compactRole.gameObject.SetActive(creature);worldRole.gameObject.SetActive(creature);
            if(creature){ColorUtility.TryParseHtmlString(catalog.roleColors.Find(c=>c.name==card.role).hex,out var role);fullRole.color=compactRole.color=role;block=new MaterialPropertyBlock();block.SetColor("_Color",role);worldRole.SetPropertyBlock(block);fullRoleText.text=card.faction+" / "+card.role;}
            fullStats.text=creature?"АТК "+card.attack+"   HP "+card.health+"   QTE "+card.qte:card.kind=="reaction"?"БЕЗ QTE":"QTE "+card.qte;
            compactStats.text=creature?card.attack+" / "+card.health+"  ·  Q"+card.qte:card.kind=="reaction"?"БЕЗ QTE":"QTE "+card.qte;
        }
    }
}
