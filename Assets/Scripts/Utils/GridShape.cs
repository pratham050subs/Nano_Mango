using System.Collections.Generic;
using UnityEngine;

namespace CardMatch.Utils
{
    /// <summary>
    /// Defines different grid shapes for card layouts
    /// </summary>
    public enum GridShape
    {
        Square,     // Traditional square grid (2x2, 3x3, etc.)
        VShape,     // V-shaped pattern
        OShape,     // O-shaped (hollow square)
        LShape,     // L-shaped pattern
        Plus,       // Plus/Cross shape
        Diamond,    // Diamond shape
        Heart       // Heart shape
    }

    /// <summary>
    /// Defines card positions for a specific shape
    /// </summary>
    public class GridShapeData
    {
        public GridShape Shape { get; private set; }
        public List<Vector2Int> Positions { get; private set; }
        public int CardCount => Positions.Count;

        public GridShapeData(GridShape shape, List<Vector2Int> positions)
        {
            Shape = shape;
            Positions = positions;
        }
    }

    /// <summary>
    /// Factory for creating grid shapes
    /// </summary>
    public static class GridShapeFactory
    {
        /// <summary>
        /// Creates a grid shape based on the shape type and size
        /// </summary>
        public static GridShapeData CreateShape(GridShape shape, int size)
        {
            switch (shape)
            {
                case GridShape.Square:
                    return CreateSquare(size);
                case GridShape.VShape:
                    return CreateVShape(size);
                case GridShape.OShape:
                    return CreateOShape(size);
                case GridShape.LShape:
                    return CreateLShape(size);
                case GridShape.Plus:
                    return CreatePlus(size);
                case GridShape.Diamond:
                    return CreateDiamond(size);
                case GridShape.Heart:
                    return CreateHeart(size);
                default:
                    return CreateSquare(size);
            }
        }

        private static GridShapeData CreateSquare(int size)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            int totalCards = size * size;
            int isOdd = totalCards % 2;
            int cardCount = totalCards - isOdd;

            int cardIndex = 0;
            for (int row = 0; row < size; row++)
            {
                for (int col = 0; col < size; col++)
                {
                    if (isOdd == 1 && row == size - 1 && col == size - 1)
                        continue;
                    if (cardIndex >= cardCount)
                        break;
                    positions.Add(new Vector2Int(col, row));
                    cardIndex++;
                }
            }
            return new GridShapeData(GridShape.Square, positions);
        }

        private static GridShapeData CreateVShape(int size)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            
            // Create V shape: narrow at top, wide at bottom
            // For size 5: row 0 has 1 card, row 1 has 2, row 2 has 3, etc.
            for (int row = 0; row < size; row++)
            {
                // Calculate width for this row (increases as we go down)
                int width = row + 1;
                // Center the row
                int startCol = (size - width) / 2;
                
                for (int col = startCol; col < startCol + width; col++)
                {
                    if (col >= 0 && col < size) // Ensure within bounds
                    {
                        positions.Add(new Vector2Int(col, row));
                    }
                }
            }
            
            // Ensure even number of cards
            if (positions.Count % 2 != 0)
            {
                positions.RemoveAt(positions.Count - 1);
            }
            
            return new GridShapeData(GridShape.VShape, positions);
        }

        private static GridShapeData CreateOShape(int size)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            
            // Create O shape: hollow square (border only)
            for (int row = 0; row < size; row++)
            {
                for (int col = 0; col < size; col++)
                {
                    // Only add positions on the border
                    if (row == 0 || row == size - 1 || col == 0 || col == size - 1)
                    {
                        positions.Add(new Vector2Int(col, row));
                    }
                }
            }
            
            // Ensure even number of cards
            if (positions.Count % 2 != 0)
            {
                positions.RemoveAt(positions.Count - 1);
            }
            
            return new GridShapeData(GridShape.OShape, positions);
        }

        private static GridShapeData CreateLShape(int size)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            
            // Create L shape: vertical line + horizontal line at bottom
            // Vertical line (left side)
            for (int row = 0; row < size; row++)
            {
                positions.Add(new Vector2Int(0, row));
            }
            // Horizontal line (bottom, excluding the corner already added)
            for (int col = 1; col < size; col++)
            {
                positions.Add(new Vector2Int(col, size - 1));
            }
            
            // Ensure even number of cards
            if (positions.Count % 2 != 0)
            {
                // Remove the last position to make it even
                positions.RemoveAt(positions.Count - 1);
            }
            
            return new GridShapeData(GridShape.LShape, positions);
        }

        private static GridShapeData CreatePlus(int size)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            int center = size / 2;
            
            // Create Plus shape: vertical and horizontal lines crossing
            for (int row = 0; row < size; row++)
            {
                positions.Add(new Vector2Int(center, row)); // Vertical line
            }
            for (int col = 0; col < size; col++)
            {
                if (col != center) // Avoid duplicate at center
                {
                    positions.Add(new Vector2Int(col, center)); // Horizontal line
                }
            }
            
            // Ensure even number of cards
            if (positions.Count % 2 != 0)
            {
                positions.RemoveAt(positions.Count - 1);
            }
            
            return new GridShapeData(GridShape.Plus, positions);
        }

        private static GridShapeData CreateDiamond(int size)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            int center = size / 2;
            
            // Create Diamond shape
            for (int row = 0; row < size; row++)
            {
                int distanceFromCenter = Mathf.Abs(row - center);
                int width = size - 2 * distanceFromCenter;
                int startCol = center - (width - 1) / 2;
                
                for (int col = startCol; col < startCol + width; col++)
                {
                    positions.Add(new Vector2Int(col, row));
                }
            }
            
            // Ensure even number of cards
            if (positions.Count % 2 != 0)
            {
                positions.RemoveAt(positions.Count - 1);
            }
            
            return new GridShapeData(GridShape.Diamond, positions);
        }

        private static GridShapeData CreateHeart(int size)
        {
            List<Vector2Int> positions = new List<Vector2Int>();

            if (size % 2 == 0)
                size--;

            int center = size / 2;

            for (int row = 0; row < size; row++)
            {
                float t = row / (float)(size - 1);
                int width;

                if (t < 0.25f)
                    width = 2;
                else if (t < 0.55f)
                    width = size;
                else
                    width = Mathf.RoundToInt(Mathf.Lerp(size, 1, (t - 0.55f) / 0.45f));

                width = Mathf.Clamp(width, 1, size);

                // ? SPECIAL CASE: top heart lobes
                if (row == 0 && width == 2)
                {
                    positions.Add(new Vector2Int(center - 1, row));
                    positions.Add(new Vector2Int(center + 1, row));
                    continue;
                }

                int startCol = center - width / 2;

                for (int col = startCol; col < startCol + width; col++)
                {
                    if (col >= 0 && col < size)
                        positions.Add(new Vector2Int(col, row));
                }
            }

            if (positions.Count % 2 != 0)
                positions.RemoveAt(positions.Count - 1);

            return new GridShapeData(GridShape.Heart, positions);
        }



    }
}

