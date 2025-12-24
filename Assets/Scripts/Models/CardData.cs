namespace CardMatch.Models
{
    /// <summary>
    /// Data model for a card
    /// </summary>
    public class CardData
    {
        public int CardId { get; set; }
        public int SpriteId { get; set; }
        public bool IsMatched { get; set; }
        public bool IsFlipped { get; set; }
        public bool IsFlipping { get; set; }

        public CardData(int cardId, int spriteId)
        {
            CardId = cardId;
            SpriteId = spriteId;
            IsMatched = false;
            IsFlipped = false;
            IsFlipping = false;
        }
    }
}




