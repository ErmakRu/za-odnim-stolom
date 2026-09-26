using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SummonersTable
{
    // One inheritance chain supplies all full, compact and tabletop faces.
    public sealed class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public CardDef definition = new CardDef();
        public LayeredCardView sharedFull,sharedCompact,sharedWorld;
        public SpellEffect spellEffect;
        public GameObject fullFace, compactFace, worldFace;
        public RawImage fullArtwork, compactArtwork;
        public Image fullBorder, compactBorder;
        public Image fullType, compactType, fullRole, compactRole, playableGlow, selectionFrame;
        public GameObject fullStatsBadge, compactStatsBadge;
        public RectTransform fullQteContainer, compactQteContainer;
        public Text fullName, fullStats, fullRules, fullTypeText, fullRoleText;
        public Text compactName, compactStats, compactTypeText, compactRoleText;
        public Renderer worldArtwork, worldType, worldRole;
        public LineRenderer worldOutline;
        public Color playableColor = new Color(.38f, .96f, .67f), reactionPlayableColor = new Color(.84f, .63f, 1);

        public static readonly Color QteEasyColor = new Color(0.22f, 0.96f, 0.56f, 1f);    // 1–3
        public static readonly Color QteMediumColor = new Color(1f, 0.79f, 0.18f, 1f);     // 4–6
        public static readonly Color QteHardColor = new Color(1f, 0.30f, 0.27f, 1f);       // 7–9 and higher modifiers

        [NonSerialized] public Action<PointerEventData> pressed, dragged, released, hovered, unhovered;
        [NonSerialized] public bool isHovered, isSelected;

        bool? rulesLimited;
        Catalog configCatalog;

        public void OnPointerEnter(PointerEventData e) { isHovered = true; hovered?.Invoke(e); }
        public void OnPointerExit(PointerEventData e) { isHovered = false; unhovered?.Invoke(e); }
        public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) pressed?.Invoke(e); }
        public void OnDrag(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) dragged?.Invoke(e); }
        public void OnPointerUp(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) released?.Invoke(e); }

        void Start() { if (ConfigRuntime.Available) ApplyDefinition(ConfigRuntime.ActiveCatalog); }

        public void ApplyDefinition(Catalog catalog)
        {
            if (configCatalog == catalog) return;
            var data = catalog.Card(definition.id);
            if (data == null) return;
            Import(data, catalog);
            configCatalog = catalog;
            rulesLimited = null;
        }

        void LateUpdate()
        {
            if(sharedFull!=null)return;
            bool limited = CardPresentationContext.Options.limitPower;
            if (rulesLimited != limited && fullRules != null)
            {
                fullRules.text = CardRulesText.For(definition, CardPresentationContext.Options);
                rulesLimited = limited;
            }
        }

        public void Mode(string mode)
        {
            if(sharedFull!=null)foreach(var holder in new[]{fullFace,compactFace})foreach(Transform child in holder.transform)if(child.name!="Shared card face")child.gameObject.SetActive(false);
            if (fullFace != null) fullFace.SetActive(mode == "full");
            if (compactFace != null) compactFace.SetActive(mode == "compact");
            if (worldFace != null) worldFace.SetActive(mode == "world");
            if (worldOutline != null) worldOutline.enabled = false;

            Vector2 size = ((RectTransform)(mode=="compact"?compactFace:fullFace).transform).sizeDelta;
            float padGlow = mode == "compact" ? 3f : 4f;
            float padSel = mode == "compact" ? 4f : 5f;

            if (playableGlow != null)
            {
                var r = (RectTransform)playableGlow.transform;
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = Vector2.zero;
                r.sizeDelta = new Vector2(size.x + padGlow * 2f, size.y + padGlow * 2f);
            }
            if (selectionFrame != null)
            {
                var r = (RectTransform)selectionFrame.transform;
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = Vector2.zero;
                r.sizeDelta = new Vector2(size.x + padSel * 2f, size.y + padSel * 2f);
            }
        }

        public void Highlight(bool playable, bool selected)
        {
            isSelected = selected;
            if (selectionFrame != null)
            {
                selectionFrame.gameObject.SetActive(selected);
                selectionFrame.color = new Color(1f, 0.85f, 0.35f, 0.95f);
            }
            if (playableGlow != null)
            {
                playableGlow.gameObject.SetActive(playable && !selected);
                playableGlow.color = new Color(0.22f, 0.96f, 0.56f, 0.65f);
            }
        }

        public static Color GetQteColor(int count)
        {
            if (count <= 3) return QteEasyColor;
            if (count <= 6) return QteMediumColor;
            return QteHardColor;
        }

        public void Import(CardDef card, Catalog catalog)
        {
            definition = JsonUtility.FromJson<CardDef>(JsonUtility.ToJson(card));
            if(sharedFull!=null)
            {
                var layers=(ConfigRuntime.Current??LayeredCardData.Current).layeredCards.Find(card.name);
                foreach(var face in new[]{sharedFull,sharedCompact,sharedWorld})if(face!=null)face.ApplyCard(definition,catalog,layers);
                return;
            }
            var type = catalog.typeColors.Find(c => c.id == card.kind);
            ColorUtility.TryParseHtmlString(type?.hex ?? "#38F58F", out var color);

            if (fullBorder != null) fullBorder.color = color;
            if (compactBorder != null) compactBorder.color = color;

            if (fullType != null) fullType.color = color;
            if (compactType != null) compactType.color = color;

            if (fullTypeText != null) fullTypeText.text = type != null ? type.name.ToUpperInvariant() : card.kind.ToUpperInvariant();
            if (compactTypeText != null) compactTypeText.text = type != null ? type.name.ToUpperInvariant() : card.kind.ToUpperInvariant();

            if (fullName != null) fullName.text = card.name;
            if (compactName != null) compactName.text = card.name;

            if (fullRules != null) fullRules.text = card.rules;

            var texture = ConfigRuntime.Artwork(card);
            if (fullArtwork != null) fullArtwork.texture = texture;
            if (compactArtwork != null) compactArtwork.texture = texture;

            if (worldArtwork != null)
            {
                var block = new MaterialPropertyBlock();
                worldArtwork.GetPropertyBlock(block);
                block.SetTexture("_MainTex", texture);
                worldArtwork.SetPropertyBlock(block);
            }
            if (worldType != null)
            {
                var block = new MaterialPropertyBlock();
                block.SetColor("_Color", color);
                worldType.SetPropertyBlock(block);
            }

            bool creature = card.kind == "creature";
            if (fullRole != null) fullRole.gameObject.SetActive(creature);
            if (fullRoleText != null) fullRoleText.gameObject.SetActive(creature);
            if (compactRole != null) compactRole.gameObject.SetActive(creature);
            if (compactRoleText != null) compactRoleText.gameObject.SetActive(creature);
            if (worldRole != null) worldRole.gameObject.SetActive(creature);

            if (creature)
            {
                var roleInfo = catalog.roleColors.Find(c => c.name == card.role);
                ColorUtility.TryParseHtmlString(roleInfo?.hex ?? "#4ade80", out var role);
                if (fullRole != null) fullRole.color = role;
                if (compactRole != null) compactRole.color = role;
                if (worldRole != null)
                {
                    var block = new MaterialPropertyBlock();
                    block.SetColor("_Color", role);
                    worldRole.SetPropertyBlock(block);
                }
                string factionRole = (!string.IsNullOrEmpty(card.faction) ? card.faction + " · " : "") + card.role;
                if (fullRoleText != null) fullRoleText.text = factionRole;
                if (compactRoleText != null) compactRoleText.text = factionRole;
            }

            // Stats badge overlay on bottom right of artwork
            if (fullStatsBadge != null) fullStatsBadge.SetActive(creature);
            if (compactStatsBadge != null) compactStatsBadge.SetActive(creature);

            if (fullStats != null)
            {
                fullStats.text = creature ? $"АТК {card.attack}   HP {card.health}" : card.kind == "reaction" ? "БЕЗ QTE" : $"QTE {card.qte}";
            }
            if (compactStats != null)
            {
                compactStats.text = creature ? $"{card.attack} / {card.health}" : card.kind == "reaction" ? "БЕЗ QTE" : $"Q{card.qte}";
            }

            // QTE indicators over artwork (top-left)
            int qteLength = card.kind == "reaction" ? 0 : Mathf.Max(0, card.qte);

            UpdateQteIndicators(fullQteContainer, qteLength, 16f, 4f);
            UpdateQteIndicators(compactQteContainer, qteLength, 10f, 3f);
        }

        static void UpdateQteIndicators(RectTransform container, int count, float boxSize, float spacing)
        {
            if (container == null) return;
            container.gameObject.SetActive(count > 0);
            if (count <= 0) return;

            Color boxColor = GetQteColor(count);
            int existing = container.childCount;

            for (int i = 0; i < count; i++)
            {
                RectTransform box;
                if (i < existing)
                {
                    box = (RectTransform)container.GetChild(i);
                    box.gameObject.SetActive(true);
                }
                else
                {
                    var go = new GameObject($"QTE_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    box = (RectTransform)go.transform;
                    box.SetParent(container, false);
                    box.anchorMin = box.anchorMax = box.pivot = new Vector2(0, 0.5f);
                }

                box.sizeDelta = new Vector2(boxSize, boxSize);
                box.anchoredPosition = new Vector2(i * (boxSize + spacing), 0);

                var img = box.GetComponent<Image>();
                img.color = boxColor;
                img.raycastTarget = false;
            }

            for (int i = count; i < existing; i++)
            {
                container.GetChild(i).gameObject.SetActive(false);
            }
        }
    }
}
