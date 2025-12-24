using UnityEngine;
using CardMatch.Models;
using CardMatch.Utils;

namespace CardMatch.Managers
{
    /// <summary>
    /// Manages scoring and combo calculations
    /// </summary>
    public class ScoreManager
    {
        private ScoreData scoreData;
        private int currentComboCount = 0;

        public ScoreData ScoreData => scoreData;
        public int CurrentComboCount => currentComboCount;

        public ScoreManager()
        {
            scoreData = new ScoreData();
        }

        public void Reset()
        {
            scoreData = new ScoreData();
            currentComboCount = 0;
        }

        public void OnCardMatched()
        {
            scoreData.Moves++;

            // Increment combo count (based on match frequency, not time)
            // Combo increases with each consecutive match
            currentComboCount++;
            scoreData.Combos++;
            
            Debug.Log($"[ScoreManager] Match! Combo count: {currentComboCount}");

            // Calculate combo multiplier (capped at max)
            // Combo starts at 2x after 2 consecutive matches
            if (currentComboCount >= 2)
            {
                scoreData.ComboMultiplier = Mathf.Min(
                    currentComboCount, // 2x, 3x, 4x, etc.
                    GameConstants.MAX_COMBO_MULTIPLIER
                );
            }
            else
            {
                // No combo yet (only 1 match)
                scoreData.ComboMultiplier = 1;
            }

            // Calculate base score
            scoreData.BaseScore += GameConstants.BASE_SCORE_PER_MATCH;

            // Calculate total
            scoreData.CalculateTotal();
        }

        public void OnCardMismatch()
        {
            scoreData.Moves++;
            
            // Reset combo on mismatch (match frequency broken)
            if (currentComboCount > 0)
            {
                Debug.Log($"[ScoreManager] Mismatch! Combo reset from {currentComboCount} to 0");
            }
            currentComboCount = 0;
            scoreData.ComboMultiplier = 1;
        }

        public int CalculateTimeBonus(float elapsedTime)
        {
            // More time = less bonus (incentivize speed)
            int bonus = Mathf.Max(0, (int)(GameConstants.TIME_BONUS_PER_SECOND * (100f - elapsedTime)));
            scoreData.TimeBonus = bonus;
            scoreData.CalculateTotal();
            return bonus;
        }

        public int GetFinalScore(float elapsedTime)
        {
            CalculateTimeBonus(elapsedTime);
            return scoreData.TotalScore;
        }
    }
}

