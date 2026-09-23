using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
namespace SummonersTable
{
    public sealed class HandFan : MonoBehaviour
    {
        public CardDisplaySlot cardSlotPrefab;
        public Vector2 cardSize=new Vector2(150,210);
        public float spacing=132,maxSpread=830,arc=18,angle=3.2f,hoverLift=35;
        readonly Dictionary<string,CardDisplaySlot> cards=new Dictionary<string,CardDisplaySlot>();
        public IEnumerable<CardDisplaySlot> Slots=>cards.Values;
        public Action<HandCard,PointerEventData> onPress,onDrag,onRelease;
        public CardDisplaySlot Slot(string uid){return cards.TryGetValue(uid,out var s)?s:null;}
        public void Present(MatchState state,int seat,Catalog catalog,Font font,string selected,string hover,string shake,float shakeUntil,PresentationData settings)
        {
            var hand=state.players[seat].hand;var style=ConfigRuntime.Current?.ui.fan;if(style!=null)cardSize=style.cardSize;
            foreach(var key in cards.Keys.Where(k=>!hand.Any(h=>h.uid==k)).ToList()){Destroy(cards[key].gameObject);cards.Remove(key);}
            float middle=(hand.Count-1)*.5f,spread=Mathf.Min(spacing,maxSpread/Mathf.Max(1,hand.Count-1));
            for(int i=0;i<hand.Count;i++)
            {
                var h=hand[i];if(!cards.TryGetValue(h.uid,out var slot)){slot=Instantiate(cardSlotPrefab,transform,false);slot.name="Hand "+h.uid;cards[h.uid]=slot;}
                slot.presentation="compact";slot.Show(catalog.Card(h.cardId),catalog,font);var rect=(RectTransform)slot.transform;
                bool raised=h.uid==selected||h.uid==hover;float factor=raised?settings.hoverScale:1;
                rect.sizeDelta=cardSize;rect.anchoredPosition=new Vector2((i-middle)*spread,-Mathf.Pow(Mathf.Abs(i-middle)/Mathf.Max(1,middle),2)*arc+(raised?hoverLift:0));
                if(h.uid==shake&&Time.unscaledTime<shakeUntil)rect.anchoredPosition+=Vector2.right*Mathf.Sin(Time.unscaledTime*70)*9;
                float tilt=0;if(h.uid==hover){RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,Input.mousePosition,null,out var cursor);tilt=Mathf.Clamp(cursor.x/cardSize.x*2,-1,1)*settings.hoverTilt;}
                if(style!=null){float degrees=FanGeometry.Angle(i,hand.Count,style.spread,style.maximumStep);float radians=degrees*Mathf.Deg2Rad;float radius=style.radius*style.uiUnitsPerMetre;rect.anchoredPosition=new Vector2(Mathf.Sin(radians)*radius,(Mathf.Cos(radians)-1)*radius*style.uiPerspective+(raised?hoverLift:0));}
                rect.localRotation=Quaternion.Euler(0,tilt,raised?0:style!=null?-FanGeometry.Angle(i,hand.Count,style.spread,style.maximumStep)*style.uiPerspective:-(i-middle)*angle);rect.localScale=Vector3.one*factor;slot.Fit();rect.SetSiblingIndex(i);
                slot.view.Highlight(MatchRules.CanUse(catalog,state,seat,catalog.Card(h.cardId)),h.uid==selected);
                slot.view.pressed=e=>onPress?.Invoke(h,e);slot.view.dragged=e=>onDrag?.Invoke(h,e);slot.view.released=e=>onRelease?.Invoke(h,e);
            }
            var lifted=Slot(hover)??Slot(selected);if(lifted!=null)lifted.transform.SetAsLastSibling();
        }
        public string Hit(Vector2 screen)
        {
            return cards.OrderByDescending(p=>p.Value.transform.GetSiblingIndex()).FirstOrDefault(p=>p.Value.gameObject.activeInHierarchy&&RectTransformUtility.RectangleContainsScreenPoint((RectTransform)p.Value.transform,screen)).Key??"";
        }
    }
}
