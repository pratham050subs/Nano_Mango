using System;
using System.Collections.Generic;
using UnityEngine;
using CardMatch.Models;
using CardMatch.Views;
using CardMatch.Utils;

namespace CardMatch.Managers
{
    /// <summary>
    /// Manages card creation, layout, and interactions
    /// </summary>
    public static class CardManager
    {
        public static event Action<CardData> OnCardClicked;

        public static List<CardView> CreateCards(
            int gridSize,
            GameObject cardPrefab,
            Transform parent,
            RectTransform panel,
            Sprite[] cardSprites,
            Sprite cardBackSprite,
            GameData savedData = null,
            GridShape shape = GridShape.Square)
        {
            List<CardView> cards = new List<CardView>();
            
            // Get shape data for the selected shape
            GridShapeData shapeData = GridShapeFactory.CreateShape(shape, gridSize);
            int cardCount = shapeData.CardCount;
            List<Vector2Int> positions = shapeData.Positions;

            Debug.Log($"[CardManager] Creating {shape} shape with size {gridSize}, {cardCount} cards");

            // Calculate layout
            // Use rect width/height for proper UI size calculations
            float panelWidth = panel.rect.width > 0 ? panel.rect.width : panel.sizeDelta.x;
            float panelHeight = panel.rect.height > 0 ? panel.rect.height : panel.sizeDelta.y;
            
            // If panel size is still 0, use sizeDelta as fallback
            if (panelWidth <= 0) panelWidth = panel.sizeDelta.x;
            if (panelHeight <= 0) panelHeight = panel.sizeDelta.y;
            
            Debug.Log($"[CardManager] Panel size: {panelWidth} x {panelHeight}, Grid: {gridSize}x{gridSize}");
            
            // Get card prefab's original size (assuming it's set in the prefab)
            RectTransform cardPrefabRect = cardPrefab.GetComponent<RectTransform>();
            float cardPrefabWidth = cardPrefabRect != null ? cardPrefabRect.sizeDelta.x : 400f; // Default fallback
            float cardPrefabHeight = cardPrefabRect != null ? cardPrefabRect.sizeDelta.y : 430f; // Default fallback
            
            // Add padding between cards and from edges (percentage of panel size)
            const float EDGE_PADDING_PERCENT = 0.05f; // 5% padding from edges
            const float CARD_SPACING_PERCENT = 0.02f; // 2% spacing between cards
            
            // Calculate edge padding
            float edgePaddingX = panelWidth * EDGE_PADDING_PERCENT;
            float edgePaddingY = panelHeight * EDGE_PADDING_PERCENT;
            
            // Calculate spacing between cards
            float cardSpacingX = panelWidth * CARD_SPACING_PERCENT;
            float cardSpacingY = panelHeight * CARD_SPACING_PERCENT;
            
            // Find bounds of the shape to calculate layout
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            foreach (var pos in positions)
            {
                minX = Mathf.Min(minX, pos.x);
                maxX = Mathf.Max(maxX, pos.x);
                minY = Mathf.Min(minY, pos.y);
                maxY = Mathf.Max(maxY, pos.y);
            }
            int shapeWidth = maxX - minX + 1;
            int shapeHeight = maxY - minY + 1;
            
            // Calculate available space for all cards (total width - edge padding - spacing between cards)
            float totalCardSpacingX = cardSpacingX * (shapeWidth - 1); // Spacing between gaps
            float totalCardSpacingY = cardSpacingY * (shapeHeight - 1);
            float availableWidth = panelWidth - (edgePaddingX * 2) - totalCardSpacingX;
            float availableHeight = panelHeight - (edgePaddingY * 2) - totalCardSpacingY;
            
            // Calculate available space per card
            float availableWidthPerCard = availableWidth / shapeWidth;
            float availableHeightPerCard = availableHeight / shapeHeight;
            
            // Calculate scale to fit cards within available space (maintain aspect ratio)
            float scaleX = availableWidthPerCard / cardPrefabWidth;
            float scaleY = availableHeightPerCard / cardPrefabHeight;
            float cardScale = Mathf.Min(scaleX, scaleY); // Use smaller scale to fit both dimensions
            
            // Calculate actual card size after scaling
            float cardWidth = cardPrefabWidth * cardScale;
            float cardHeight = cardPrefabHeight * cardScale;
            
            // Calculate total width/height used by all cards and spacing
            float totalGridWidth = (cardWidth * shapeWidth) + (cardSpacingX * (shapeWidth - 1));
            float totalGridHeight = (cardHeight * shapeHeight) + (cardSpacingY * (shapeHeight - 1));
            
            // Calculate spacing between card centers
            float xSpacing = cardWidth + cardSpacingX;
            float ySpacing = cardHeight + cardSpacingY;
            
            // Center the grid within the available space (after edge padding)
            // Available space = panel size - (edge padding * 2)
            float availableWidthForGrid = panelWidth - (edgePaddingX * 2);
            float availableHeightForGrid = panelHeight - (edgePaddingY * 2);
            
            // Calculate offset to center the grid
            float centerOffsetX = (availableWidthForGrid - totalGridWidth) / 2f;
            float centerOffsetY = (availableHeightForGrid - totalGridHeight) / 2f;
            
            // Calculate starting position relative to panel's local coordinate system
            // When panel anchor is at center (0.5, 0.5), anchoredPosition (0,0) is at center
            // So: left = -width/2, right = +width/2, top = +height/2, bottom = -height/2
            float leftEdge = -panelWidth / 2f;
            float topEdge = panelHeight / 2f;
            
            // Start from left edge + padding + center offset, positioned from top edge - padding - center offset
            // Position is the center of the card, so add half card width/height
            float startX = leftEdge + edgePaddingX + centerOffsetX + cardWidth / 2f;
            float startY = topEdge - edgePaddingY - centerOffsetY - cardHeight / 2f;
            
            Debug.Log($"[CardManager] Shape bounds: {shapeWidth}x{shapeHeight}, Card count: {cardCount}");
            Debug.Log($"[CardManager] Card prefab size: {cardPrefabWidth}x{cardPrefabHeight}");
            Debug.Log($"[CardManager] Available space per card: {availableWidthPerCard}x{availableHeightPerCard}");
            Debug.Log($"[CardManager] Card scale: {cardScale}, Actual card size: {cardWidth}x{cardHeight}");
            Debug.Log($"[CardManager] Total grid width: {totalGridWidth}, Available width: {availableWidthForGrid}");
            Debug.Log($"[CardManager] Center offset X: {centerOffsetX}, Y: {centerOffsetY}");
            Debug.Log($"[CardManager] Spacing: {xSpacing} x {ySpacing}, Start: ({startX}, {startY})");

            List<CardData> cardDataList;

            // If resuming, restore cards in exact saved order
            if (savedData != null && savedData.cardIds != null && savedData.cardIds.Length == cardCount)
            {
                Debug.Log($"[CardManager] Restoring cards from save data: {savedData.cardIds.Length} cards");
                cardDataList = new List<CardData>();
                
                // Create CardData objects in the exact saved order with saved sprite IDs
                for (int i = 0; i < savedData.cardIds.Length; i++)
                {
                    int cardId = savedData.cardIds[i];
                    int spriteId = savedData.cardSpriteIds[i];
                    cardDataList.Add(new CardData(cardId, spriteId));
                }
            }
            else
            {
                // Create card pairs (new game)
                // cardCount is always even (we subtract 1 for odd totals)
                int pairCount = cardCount / 2;
                List<int> spriteIds = AllocateSprites(pairCount, cardSprites.Length);
                cardDataList = CreateCardData(cardCount, spriteIds);

                // Shuffle card data
                ShuffleList(cardDataList);
            }

            // Instantiate cards at shape positions
            for (int cardIndex = 0; cardIndex < cardCount && cardIndex < positions.Count; cardIndex++)
            {
                Vector2Int pos = positions[cardIndex];
                
                GameObject cardObj = UnityEngine.Object.Instantiate(cardPrefab, parent);
                CardView cardView = cardObj.GetComponent<CardView>();
                
                if (cardView == null)
                {
                    Debug.LogError("Card prefab must have CardView component!");
                    continue;
                }

                CardData cardData = cardDataList[cardIndex];
                Sprite frontSprite = cardSprites[cardData.SpriteId];
                
                // Get RectTransform for UI positioning
                RectTransform cardRect = cardObj.GetComponent<RectTransform>();
                if (cardRect == null)
                {
                    Debug.LogError("Card prefab must have RectTransform component!");
                    continue;
                }
                
                // Position card relative to panel's coordinate system
                // Adjust position relative to shape bounds (normalize to start from 0,0)
                RectTransform parentRect = parent as RectTransform;
                
                // Calculate position relative to shape bounds
                int relativeX = pos.x - minX;
                int relativeY = pos.y - minY;
                
                float xPos = startX + relativeX * xSpacing;
                float yPos = startY - relativeY * ySpacing; // Rows go down (negative Y direction)
                
                if (parentRect != null && parentRect != panel)
                {
                    // Parent is different from panel - convert panel-relative position to parent-relative
                    // Get panel's center in world space
                    Vector3 panelCenterWorld = panel.position;
                    // Get parent's center in world space  
                    Vector3 parentCenterWorld = parentRect.position;
                    // Calculate offset
                    Vector3 offsetWorld = panelCenterWorld - parentCenterWorld;
                    // Convert to parent's local space
                    Vector2 offsetLocal = parentRect.InverseTransformPoint(panelCenterWorld) - parentRect.InverseTransformPoint(parentCenterWorld);
                    
                    // Adjust position by offset
                    xPos += offsetLocal.x;
                    yPos += offsetLocal.y;
                }
                
                cardRect.anchoredPosition = new Vector2(xPos, yPos);
                
                // Set scale BEFORE Initialize so originalScale is stored correctly
                cardRect.localScale = new Vector3(cardScale, cardScale, 1);
                
                cardView.Initialize(cardData, frontSprite, cardBackSprite);

                cards.Add(cardView);
            }

            return cards;
        }

        private static List<int> AllocateSprites(int pairCount, int availableSprites)
        {
            List<int> spriteIds = new List<int>();
            System.Random random = new System.Random();

            for (int i = 0; i < pairCount; i++)
            {
                int spriteId = random.Next(0, availableSprites);
                spriteIds.Add(spriteId);
            }

            return spriteIds;
        }

        private static List<CardData> CreateCardData(int cardCount, List<int> spriteIds)
        {
            List<CardData> cardDataList = new List<CardData>();
            int pairIndex = 0;

            // cardCount is always even, so we can safely create pairs
            for (int i = 0; i < cardCount; i += 2)
            {
                int spriteId = spriteIds[pairIndex];
                cardDataList.Add(new CardData(i, spriteId));
                cardDataList.Add(new CardData(i + 1, spriteId));
                pairIndex++;
            }

            return cardDataList;
        }

        private static void ShuffleList<T>(List<T> list)
        {
            System.Random random = new System.Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static void NotifyCardClicked(CardData cardData)
        {
            OnCardClicked?.Invoke(cardData);
        }
    }
}

