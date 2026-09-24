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
        public Vector2 cardSize = new Vector2(150, 210);
        public float spacing = 132, maxSpread = 830, arc = 18, angle = 3.2f, hoverLift = 35;
        public float autoTiltAmount = 1.5f, manualTiltAmount = 25f;

        readonly Dictionary<string, CardDisplaySlot> cards = new Dictionary<string, CardDisplaySlot>();
        readonly Dictionary<string, float> currentScale = new Dictionary<string, float>();
        readonly Dictionary<string, Vector2> currentOffset = new Dictionary<string, Vector2>();

        public IEnumerable<CardDisplaySlot> Slots => cards.Values;
        public Action<HandCard, PointerEventData> onPress, onDrag, onRelease;
        public CardDisplaySlot Slot(string uid) => cards.TryGetValue(uid, out var s) ? s : null;

        public static float NormalizedIndex(int index, int count) => count <= 1 ? 0.5f : (float)index / (count - 1);

        public void Present(MatchState state, int seat, Catalog catalog, Font font, string selected, string hover, string shake, float shakeUntil, PresentationData settings)
        {
            var hand = state.players[seat].hand;
            var style = ConfigRuntime.Current?.ui.fan;
            if (style != null) cardSize = style.cardSize;

            foreach (var key in cards.Keys.Where(k => !hand.Any(h => h.uid == k)).ToList())
            {
                if (cards.TryGetValue(key, out var s) && s != null) Destroy(s.gameObject);
                cards.Remove(key);
                currentScale.Remove(key);
                currentOffset.Remove(key);
            }

            int count = hand.Count;
            float middle = (count - 1) * 0.5f;
            float spread = count <= 1 ? 0 : Mathf.Min(spacing, maxSpread / Mathf.Max(1, count - 1));

            for (int i = 0; i < count; i++)
            {
                var h = hand[i];
                if (!cards.TryGetValue(h.uid, out var slot) || slot == null)
                {
                    slot = Instantiate(cardSlotPrefab, transform, false);
                    slot.name = "Hand " + h.uid;
                    cards[h.uid] = slot;
                    currentScale[h.uid] = 1f;
                    currentOffset[h.uid] = Vector2.zero;
                }

                slot.presentation = "compact";
                slot.Show(catalog.Card(h.cardId), catalog, font);
                var rect = (RectTransform)slot.transform;

                bool isHovered = h.uid == hover;
                bool isSelected = h.uid == selected;
                bool raised = isSelected || isHovered;

                float targetScale = raised ? (settings != null ? settings.hoverScale : 1.1f) : 1f;
                float curScale = currentScale.TryGetValue(h.uid, out var sc) ? sc : 1f;
                curScale = Mathf.Lerp(curScale, targetScale, Time.unscaledDeltaTime * 20f);
                currentScale[h.uid] = curScale;

                // Base positioning along the fan arc
                Vector2 targetPos;
                float rotZ;
                if (style != null)
                {
                    float degrees = FanGeometry.Angle(i, count, style.spread, style.maximumStep);
                    float radians = degrees * Mathf.Deg2Rad;
                    float radius = style.radius * style.uiUnitsPerMetre;
                    targetPos = new Vector2(Mathf.Sin(radians) * radius, (Mathf.Cos(radians) - 1) * radius * style.uiPerspective + (raised ? hoverLift : 0));
                    rotZ = raised ? 0 : -degrees * style.uiPerspective;
                }
                else
                {
                    float normMiddle = count <= 1 ? 0 : (i - middle);
                    float arcNorm = middle > 0 ? Mathf.Abs(normMiddle) / middle : 0;
                    targetPos = new Vector2(normMiddle * spread, -Mathf.Pow(arcNorm, 2) * arc + (raised ? hoverLift : 0));
                    rotZ = raised ? 0 : -normMiddle * angle;
                }

                // Shake punch on invalid action
                if (h.uid == shake && Time.unscaledTime < shakeUntil)
                {
                    targetPos += Vector2.right * (Mathf.Sin(Time.unscaledTime * 70f) * 9f);
                }

                // 3D Tilt calculation (Balatro Feel)
                float tiltX = 0, tiltY = 0;
                float idleFactor = raised ? 0.2f : 1f;
                float sine = Mathf.Sin(Time.unscaledTime * 2f + i) * idleFactor * autoTiltAmount;
                float cosine = Mathf.Cos(Time.unscaledTime * 2f + i) * idleFactor * autoTiltAmount;

                if (isHovered)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, Input.mousePosition, null, out var cursor);
                    tiltY = Mathf.Clamp(cursor.x / Mathf.Max(1, cardSize.x) * 2f, -1f, 1f) * manualTiltAmount;
                    tiltX = -Mathf.Clamp(cursor.y / Mathf.Max(1, cardSize.y) * 2f, -1f, 1f) * manualTiltAmount;
                }

                rect.sizeDelta = cardSize;
                rect.anchoredPosition = targetPos;
                rect.localRotation = Quaternion.Euler(tiltX + sine, tiltY + cosine, rotZ);
                rect.localScale = Vector3.one * curScale;
                slot.Fit();
                rect.SetSiblingIndex(i);

                if (slot.view != null)
                {
                    slot.view.Highlight(MatchRules.CanUse(catalog, state, seat, catalog.Card(h.cardId)), isSelected);
                    slot.view.pressed = e => onPress?.Invoke(h, e);
                    slot.view.dragged = e => onDrag?.Invoke(h, e);
                    slot.view.released = e => onRelease?.Invoke(h, e);
                }
            }

            var lifted = Slot(hover) ?? Slot(selected);
            if (lifted != null) lifted.transform.SetAsLastSibling();
        }

        public string Hit(Vector2 screen)
        {
            return cards.OrderByDescending(p => p.Value.transform.GetSiblingIndex())
                .FirstOrDefault(p => p.Value != null && p.Value.gameObject.activeInHierarchy &&
                                     RectTransformUtility.RectangleContainsScreenPoint((RectTransform)p.Value.transform, screen)).Key ?? "";
        }
    }
}
