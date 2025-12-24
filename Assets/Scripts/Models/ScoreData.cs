namespace CardMatch.Models
{
    /// <summary>
    /// Score calculation data
    /// </summary>
    public class ScoreData
    {
        public int BaseScore { get; set; }
        public int ComboMultiplier { get; set; }
        public int TimeBonus { get; set; }
        public int TotalScore { get; set; }
        public int Moves { get; set; }
        public int Combos { get; set; }

        public ScoreData()
        {
            BaseScore = 0;
            ComboMultiplier = 1;
            TimeBonus = 0;
            TotalScore = 0;
            Moves = 0;
            Combos = 0;
        }

        public void CalculateTotal()
        {
            TotalScore = (BaseScore * ComboMultiplier) + TimeBonus;
        }
    }
}




