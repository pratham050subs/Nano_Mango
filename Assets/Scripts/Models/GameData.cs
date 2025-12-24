using System;
using UnityEngine;

namespace CardMatch.Models
{
    /// <summary>
    /// Serializable game data for save/load functionality
    /// </summary>
    [Serializable]
    public class GameData
    {
        public int gridSize;
        public int gridShape; // Save GridShape as int (enum value)
        public int[] cardSpriteIds;
        public int[] cardIds;
        public bool[] cardMatchedStates;
        public bool[] cardFlippedStates; // Save flipped state explicitly
        public float elapsedTime;
        public int score;
        public int moves;
        public int combos;
        public DateTime saveDateTime;

        public GameData()
        {
            gridSize = 2;
            gridShape = 0; // Default to Square (GridShape.Square = 0)
            cardSpriteIds = new int[0];
            cardIds = new int[0];
            cardMatchedStates = new bool[0];
            cardFlippedStates = new bool[0];
            elapsedTime = 0f;
            score = 0;
            moves = 0;
            combos = 0;
            saveDateTime = DateTime.Now;
        }
    }
}

