using UnityEngine;
using UnityEngine.UI;

namespace SummonersTable
{
    public sealed class CardDisplaySlot : MonoBehaviour
    {
        public Image background,typeBand,roleBand;
        public RawImage artwork;
        public Text typeLabel,nameLabel,statsLabel,rulesLabel,roleLabel;
        string current="";
        public void Show(CardDef card,Catalog catalog,Font font)
        {
            gameObject.SetActive(card!=null);if(card==null)return;
            if(current==card.id&&nameLabel.font==font)return;current=card.id;
            foreach(var label in new[]{typeLabel,nameLabel,statsLabel,rulesLabel,roleLabel})label.font=font;
            ColorUtility.TryParseHtmlString(catalog.typeColors.Find(c=>c.id==card.kind).hex,out var color);
            typeBand.color=color;typeLabel.text=catalog.typeColors.Find(c=>c.id==card.kind).name+" · "+card.id;
            artwork.texture=Resources.Load<Texture2D>("Art/"+card.id);nameLabel.text=card.name;
            bool creature=card.kind=="creature";roleBand.gameObject.SetActive(creature);roleLabel.gameObject.SetActive(creature);
            if(creature){ColorUtility.TryParseHtmlString(catalog.roleColors.Find(c=>c.name==card.role).hex,out color);roleBand.color=color;roleLabel.text=card.faction+" / "+card.role;}
            statsLabel.text=creature?"АТК "+card.attack+"   HP "+card.health+"   QTE "+card.qte:card.kind=="reaction"?"РЕАКЦИЯ · БЕЗ QTE":"ЗАКЛИНАНИЕ · QTE "+card.qte;
            rulesLabel.text=card.rules;
        }
    }
}
