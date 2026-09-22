using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed class HistoryRowView : MonoBehaviour
    {
        public GameObject header,body;public Text heading,actor,sourceName,verb;
        public CardDisplaySlot sourceCard;public RectTransform sourceHover;
        public GameObject[] targetGroups;public Text[] targetOwners,targetNames,amounts;public CardDisplaySlot[] targetCards;
        [System.NonSerialized]public HistoryEntry entry;
        public string Inspect(Vector2 p)
        {
            if(entry==null||!body.activeSelf)return "";
            if(RectTransformUtility.RectangleContainsScreenPoint(sourceHover,p))return entry.cardId;
            for(int i=0;i<entry.targets.Count&&i<targetGroups.Length;i++)if(RectTransformUtility.RectangleContainsScreenPoint((RectTransform)targetGroups[i].transform,p))return entry.targets[i].cardId;
            return "";
        }
    }
}
