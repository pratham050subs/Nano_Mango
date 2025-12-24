namespace CardMatch.Utils
{
    /// <summary>
    /// Game constants and configuration values
    /// </summary>
    public static class GameConstants
    {
        public const int MIN_GRID_SIZE = 2;
        public const int MAX_GRID_SIZE = 6;
        public const int DEFAULT_GRID_SIZE = 2;

        public const float CARD_FLIP_DURATION = 0.25f;
        public const float CARD_FADE_DURATION = 2.5f;
        public const float CARD_REVEAL_DURATION = 0.3f;
        public const float CARD_SELECTION_DELAY = 0.5f;
        
        // Audio timing - delay before playing match/mismatch sounds
        // Allows flip sound to play fully before next sound
        public const float AUDIO_RESULT_DELAY = 0.3f;

        public const int BASE_SCORE_PER_MATCH = 100;
        public const int COMBO_MULTIPLIER_INCREMENT = 1;
        public const int MAX_COMBO_MULTIPLIER = 5;
        public const int TIME_BONUS_PER_SECOND = 10;

        public const string SAVE_FILE_NAME = "CardMatchSave.json";
    }
}




