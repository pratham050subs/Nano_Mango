using System.IO;
using UnityEngine;
using CardMatch.Core;
using CardMatch.Models;
using CardMatch.Utils;

namespace CardMatch.Services
{
    /// <summary>
    /// Service responsible for saving and loading game data
    /// </summary>
    public class SaveService : ISaveService
    {
        private string SaveFilePath => Path.Combine(Application.persistentDataPath, GameConstants.SAVE_FILE_NAME);

        public void SaveGameData(GameData data)
        {
            if (data == null)
            {
                Debug.LogError("Cannot save null game data.");
                return;
            }

            try
            {
                data.saveDateTime = System.DateTime.Now;
                string jsonData = JsonUtility.ToJson(data, true);
                File.WriteAllText(SaveFilePath, jsonData);
                Debug.Log($"Game data saved successfully to {SaveFilePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save game data: {e.Message}");
            }
        }

        public GameData LoadGameData()
        {
            if (!HasSaveData())
            {
                Debug.LogWarning("No save data found.");
                return null;
            }

            try
            {
                string jsonData = File.ReadAllText(SaveFilePath);
                GameData data = JsonUtility.FromJson<GameData>(jsonData);
                Debug.Log($"Game data loaded successfully from {SaveFilePath}");
                return data;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load game data: {e.Message}");
                return null;
            }
        }

        public bool HasSaveData()
        {
            return File.Exists(SaveFilePath);
        }

        public void DeleteSaveData()
        {
            try
            {
                if (HasSaveData())
                {
                    File.Delete(SaveFilePath);
                    Debug.Log("Save data deleted successfully.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to delete save data: {e.Message}");
            }
        }
    }
}




