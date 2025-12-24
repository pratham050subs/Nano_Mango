namespace CardMatch.Utils
{
    /// <summary>
    /// Game-related enumerations
    /// </summary>
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        GameOver,
        Victory
    }

    public enum AudioClipType
    {
        CardFlip = 0,
        CardMatch = 1,
        CardMismatch = 2,
        GameOver = 3
    }
}




