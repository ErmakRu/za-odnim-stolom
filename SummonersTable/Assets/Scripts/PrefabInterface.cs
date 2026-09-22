using UnityEngine;
namespace SummonersTable
{
    public sealed class PrefabInterface : MonoBehaviour
    {
        public WidgetScreen hud,settings,results,handoff,rules,quit,search,browser;
        public HandFan hand;public HistoryListView history;public TargetArrowGraphic arrow;
        public PlayerStatusView[] playerStatus;
        public RectTransform worldOverlay;
        public WidgetScreen unitBadgePrefab,roomRowPrefab;
        public CardDisplaySlot browserCardPrefab;
    }
}
