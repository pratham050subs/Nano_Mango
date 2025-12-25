using UnityEngine;
using CardMatch.Services;

namespace CardMatch.Testing
{
    /// <summary>
    /// Helper script to test audio functionality
    /// Attach to any GameObject in the scene
    /// Press 1, 2, 3, or 4 to test each sound
    /// </summary>
    public class AudioTestHelper : MonoBehaviour
    {
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Debug.Log("=== Testing Card Flip Sound (Key 1) ===");
                if (AudioService.Instance != null)
                {
                    AudioService.Instance.PlayCardFlip();
                }
                else
                {
                    Debug.LogError("AudioService.Instance is NULL!");
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Debug.Log("=== Testing Card Match Sound (Key 2) ===");
                if (AudioService.Instance != null)
                {
                    AudioService.Instance.PlayCardMatch();
                }
                else
                {
                    Debug.LogError("AudioService.Instance is NULL!");
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                Debug.Log("=== Testing Card Mismatch Sound (Key 3) ===");
                if (AudioService.Instance != null)
                {
                    AudioService.Instance.PlayCardMismatch();
                }
                else
                {
                    Debug.LogError("AudioService.Instance is NULL!");
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                Debug.Log("=== Testing Game Over Sound (Key 4) ===");
                if (AudioService.Instance != null)
                {
                    AudioService.Instance.PlayGameOver();
                }
                else
                {
                    Debug.LogError("AudioService.Instance is NULL!");
                }
            }
        }

        void OnGUI()
        {
            // Show instructions on screen
            GUI.Label(new Rect(10, 10, 400, 100), 
                "Audio Test Helper:\n" +
                "Press 1 = Card Flip\n" +
                "Press 2 = Card Match\n" +
                "Press 3 = Card Mismatch\n" +
                "Press 4 = Game Over");
        }
    }
}

