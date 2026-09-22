using System.Linq;
using UnityEngine;
namespace SummonersTable
{
    [CreateAssetMenu(menuName="Summoners Table/Card prefab library")]
    public sealed class CardLibrary : ScriptableObject
    {
        public CardView[] cards;
        public GameObject cardBackPrefab;
        public CardView Find(string id){return cards?.FirstOrDefault(c=>c!=null&&c.definition.id==id);}
        public void Apply(Catalog catalog){if(cards==null||cards.Length!=catalog.cards.Count)return;catalog.cards=cards.Select(c=>JsonUtility.FromJson<CardDef>(JsonUtility.ToJson(c.definition))).ToList();catalog.Validate();}
        public static Catalog LoadCatalog(){var c=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("Data/catalog").text);Resources.Load<CardLibrary>("CardLibrary")?.Apply(c);return c;}
    }
}
