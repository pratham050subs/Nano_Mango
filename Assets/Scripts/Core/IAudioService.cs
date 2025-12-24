namespace CardMatch.Core
{
    /// <summary>
    /// Interface for audio playback
    /// </summary>
    public interface IAudioService
    {
        void PlayCardFlip();
        void PlayCardMatch();
        void PlayCardMismatch();
        void PlayGameOver();
        void SetVolume(float volume);
    }
}




