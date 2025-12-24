using CardMatch.Models;

namespace CardMatch.Core
{
    /// <summary>
    /// Interface for save/load functionality
    /// </summary>
    public interface ISaveService
    {
        void SaveGameData(GameData data);
        GameData LoadGameData();
        bool HasSaveData();
        void DeleteSaveData();
    }
}

