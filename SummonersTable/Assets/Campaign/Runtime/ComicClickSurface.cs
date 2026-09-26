using UnityEngine;
using UnityEngine.EventSystems;
namespace SummonersTable
{
    public sealed class ComicClickSurface:MonoBehaviour,IPointerClickHandler
    {
        public CampaignComicView view;
        public void OnPointerClick(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)view.PointerClick(e.clickCount);}
    }
}
