using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardMatch.Core;
using CardMatch.Models;
using CardMatch.Services;
using CardMatch.Utils;
using CardMatch.Views;

namespace CardMatch.Managers
{
    /// <summary>
    /// Main game manager handling game flow, state, and coordination
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Game Configuration")]
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private Transform cardParent;
        [SerializeField] private RectTransform gamePanel;
        [SerializeField] private Sprite cardBackSprite;
        [SerializeField] private Sprite[] cardSprites;
        [SerializeField] private GameUIView gameUIView;
        [SerializeField] private bool enableSplashScreen = true;

        private int gridSize = GameConstants.DEFAULT_GRID_SIZE;
        private GridShape currentShape = GridShape.Square;
        private List<CardView> cards = new List<CardView>();
        private HashSet<int> cardsBeingProcessed = new HashSet<int>(); // Track cards currently in match processing
        private bool isGameActive = false;
        private float elapsedTime = 0f;

        private ScoreManager scoreManager;
        private ISaveService saveService;
        private IAudioService audioService;

        private void Awake()
        {
            InitializeServices();
            InitializeUI();
            InitializeSplashScreen();
        }

        private void Update()
        {
            if (isGameActive)
            {
                elapsedTime += Time.deltaTime;
                gameUIView?.UpdateScore(scoreManager.ScoreData.TotalScore);
                gameUIView?.UpdateMoves(scoreManager.ScoreData.Moves);
                
                // Update combo (only updates UI when combo value changes)
                int currentCombo = scoreManager.CurrentComboCount;
                int comboMultiplier = scoreManager.ScoreData.ComboMultiplier;
                gameUIView?.UpdateCombo(currentCombo, comboMultiplier);
            }
        }

        private void InitializeServices()
        {
            saveService = new SaveService();
            
            // Try to get AudioService instance, if not found try to find it in scene
            audioService = AudioService.Instance;
            
            if (audioService == null)
            {
                // AudioService might not have initialized yet, try to find it
                var audioServiceObj = FindObjectOfType<AudioService>();
                if (audioServiceObj != null)
                {
                    audioService = audioServiceObj;
                    Debug.Log("[GameManager] Found AudioService via FindObjectOfType");
                }
                else
                {
                    Debug.LogError("[GameManager] AudioService instance not found! Make sure AudioService GameObject exists in the scene.");
                }
            }
            else
            {
                Debug.Log("[GameManager] AudioService instance found successfully");
            }

            scoreManager = new ScoreManager();
        }

        private void InitializeSplashScreen()
        {
            if (!enableSplashScreen)
            {
                gameUIView?.ShowMenuPanel(true);
                return;
            }

            // Create splash controller at runtime (uses mango sprite from Resources)
            SplashScreenController.Create(gameUIView);
        }

        private void InitializeUI()
        {
            if (gameUIView == null)
            {
                Debug.LogError("GameUIView is not assigned!");
                return;
            }

            // Setup shape selection buttons (using grid size buttons)
            gameUIView.SetupGridSizeButtons(OnShapeChanged);

            gameUIView.UpdateGridSize(gridSize);
            gameUIView.ShowGamePanel(false);
            gameUIView.ShowMenuPanel(true);

            if (gameUIView.StartButton != null)
            {
                gameUIView.StartButton.onClick.AddListener(StartNewGame);
            }

            if (gameUIView.BackButton != null)
            {
                gameUIView.BackButton.onClick.AddListener(GiveUpGame);
            }

            // Check for save data
            if (gameUIView.ResumeButton != null)
            {
                gameUIView.ResumeButton.onClick.AddListener(ResumeGame);
            }
            UpdateResumeButtonVisibility();
        }

        public void StartNewGame()
        {
            // Get selected shape from UI view (set by button clicks)
            currentShape = gameUIView?.SelectedShape ?? GridShape.Square;
            // Use a larger grid size for shapes to ensure enough cards
            // Different shapes need different sizes to look good
            gridSize = GetOptimalGridSizeForShape(currentShape);
            StartGame(false);
            // Update resume button visibility after starting new game
            UpdateResumeButtonVisibility();
        }

        /// <summary>
        /// Gets the optimal grid size for a given shape
        /// </summary>
        private int GetOptimalGridSizeForShape(GridShape shape)
        {
            switch (shape)
            {
                case GridShape.Square:
                    return 4; // 4x4 = 16 cards
                case GridShape.VShape:
                    return 5; // V shape needs more space
                case GridShape.OShape:
                    return 4; // 4x4 border = 12 cards
                case GridShape.LShape:
                    return 4; // L shape: 4 vertical + 3 horizontal = 7 cards, then -1 = 6 cards
                case GridShape.Plus:
                    return 5; // Plus: 5 vertical + 4 horizontal = 9 cards, then -1 = 8 cards
                case GridShape.Diamond:
                    return 5; // Diamond needs more space
                case GridShape.Heart:
                    return 5; // Heart needs more space
                default:
                    return 4;
            }
        }

        public void ResumeGame()
        {
            GameData savedData = saveService.LoadGameData();
            if (IsSaveCompleted(savedData))
            {
                Debug.Log("[GameManager] Save data already completed. Hiding resume.");
                saveService.DeleteSaveData();
                UpdateResumeButtonVisibility();
                return;
            }

            if (savedData != null)
            {
                // Validate saved data
                if (savedData.gridSize < GameConstants.MIN_GRID_SIZE || 
                    savedData.gridSize > GameConstants.MAX_GRID_SIZE)
                {
                    Debug.LogWarning($"Invalid grid size in save data: {savedData.gridSize}. Starting new game.");
                    saveService.DeleteSaveData();
                    StartNewGame();
                    return;
                }

                if (savedData.cardIds == null || savedData.cardIds.Length == 0)
                {
                    Debug.LogWarning("Save data has no cards. Starting new game.");
                    saveService.DeleteSaveData();
                    StartNewGame();
                    return;
                }

                // Restore grid size and shape from saved data
                gridSize = savedData.gridSize;
                currentShape = (GridShape)savedData.gridShape; // Restore saved shape
                
                // Update UI to reflect the saved shape
                if (gameUIView != null)
                {
                    gameUIView.SetupGridSizeButtons(OnShapeChanged, currentShape);
                }
                
                Debug.Log($"[GameManager] Resuming game with shape: {currentShape}, gridSize: {gridSize}");
                StartGame(true, savedData);
            }
            else
            {
                Debug.LogWarning("Failed to load save data. Starting new game instead.");
                StartNewGame();
            }
        }

        private void StartGame(bool isResume, GameData savedData = null)
        {
            if (isGameActive) return;

            isGameActive = true;
            elapsedTime = savedData?.elapsedTime ?? 0f;
            scoreManager.Reset();

            if (savedData != null)
            {
                scoreManager.ScoreData.Moves = savedData.moves;
                scoreManager.ScoreData.Combos = savedData.combos;
                scoreManager.ScoreData.TotalScore = savedData.score;
            }

            gameUIView?.ShowMenuPanel(false);
            gameUIView?.ShowGamePanel(true);

            ClearCards();
            CreateCards(savedData); // Pass savedData to restore exact card arrangement

            if (savedData != null)
            {
                RestoreGameState(savedData);

                gameUIView?.UpdateScore(scoreManager.ScoreData.TotalScore);
                gameUIView?.UpdateMoves(scoreManager.ScoreData.Moves);
                gameUIView?.UpdateCombo(scoreManager.CurrentComboCount, scoreManager.ScoreData.ComboMultiplier);
                gameUIView?.UpdateGridSize(gridSize);
            }
            else
            {
                StartCoroutine(RevealCardsBriefly());
            }
        }

        private void CreateCards(GameData savedData = null)
        {
            if (cardPrefab == null || cardParent == null || gamePanel == null)
            {
                Debug.LogError("Card prefab, parent, or panel not assigned!");
                return;
            }

            cards = CardManager.CreateCards(
                gridSize,
                cardPrefab,
                cardParent,
                gamePanel,
                cardSprites,
                cardBackSprite,
                savedData, // Pass saved data to restore exact card arrangement
                currentShape // Pass current shape
            );

            // Subscribe to card click events
            foreach (var cardView in cards)
            {
                if (cardView == null)
                {
                    Debug.LogError("[GameManager] CardView is null in cards list!");
                    continue;
                }

                Debug.Log($"[GameManager] Subscribing to card: {cardView.name}");
                cardView.OnCardClicked += HandleCardClicked;
            }

            Debug.Log($"[GameManager] Subscribed to {cards.Count} card click events");
        }


        private void ClearCards()
        {
            foreach (var cardView in cards)
            {
                if (cardView != null)
                {
                    cardView.OnCardClicked -= HandleCardClicked;
                    Destroy(cardView.gameObject);
                }
            }
            cards.Clear();
            cardsBeingProcessed.Clear();
        }

        private void HandleCardClicked(CardData cardData)
        {
            Debug.Log($"[GameManager] HandleCardClicked called. Game active: {isGameActive}");

            if (!isGameActive)
            {
                Debug.Log("[GameManager] Click ignored - game not active");
                return;
            }

            CardView cardView = cards.Find(c => c.CardData == cardData);
            if (cardView == null)
            {
                Debug.LogError("[GameManager] CardView not found for clicked card!");
                return;
            }

            // Only block if card is already flipping, flipped, or matched
            if (cardView.IsFlipping || cardData.IsMatched)
            {
                Debug.Log($"[GameManager] Click ignored - card state: Flipping={cardView.IsFlipping}, Matched={cardData.IsMatched}");
                return;
            }

            // Allow clicking even if card is flipped (continuous flipping allowed)
            // But if it's already flipped and not matched, flip it back first
            if (cardView.IsFlipped && !cardData.IsMatched)
            {
                // Card is flipped but not matched - flip it back
                Debug.Log($"[GameManager] Flipping back card: {cardView.name}");
                
                audioService?.PlayCardFlip();
                cardView.FlipBack();
                
                // Note: No need to remove from queue since we're using parallel processing
                // The ProcessPair coroutine will validate the card state and abort if needed
                return;
            }

            Debug.Log($"[GameManager] Processing card click for {cardView.name}");
            audioService?.PlayCardFlip();
            cardView.FlipCard();

            // Find the most recently flipped card waiting for comparison (do not skip over an older flipped card).
            CardData matchingCard = null;
            CardView matchingCardView = null;
            for (int i = cards.Count - 1; i >= 0; i--)
            {
                var card = cards[i];
                if (card.CardData != cardData &&
                    card.CardData.IsFlipped &&
                    !card.CardData.IsMatched &&
                    !cardsBeingProcessed.Contains(card.CardData.CardId))
                {
                    matchingCard = card.CardData;
                    matchingCardView = card;
                    Debug.Log($"[GameManager] Found waiting flipped card: Card {matchingCard.CardId} (Sprite {matchingCard.SpriteId})");
                    break;
                }
            }

            if (matchingCard != null)
            {
                // Check if either card is already being processed
                if (cardsBeingProcessed.Contains(matchingCard.CardId) || 
                    cardsBeingProcessed.Contains(cardData.CardId))
                {
                    Debug.Log($"[GameManager] Cards already being processed, skipping pair");
                    return;
                }

                // Mark cards as being processed
                cardsBeingProcessed.Add(matchingCard.CardId);
                cardsBeingProcessed.Add(cardData.CardId);
                
                Debug.Log($"[GameManager] Starting parallel processing for pair: Card {matchingCard.CardId} (Sprite {matchingCard.SpriteId}) and Card {cardData.CardId} (Sprite {cardData.SpriteId})");
                
                // Process this pair immediately in parallel (don't wait for other pairs)
                StartCoroutine(ProcessPair(matchingCard, matchingCardView, cardData, cardView));
            }
            else
            {
                Debug.Log($"[GameManager] No other flipped card found to pair with Card {cardData.CardId}");
            }
        }

        /// <summary>
        /// Process a single card pair independently (allows parallel processing of multiple pairs)
        /// </summary>
        private IEnumerator ProcessPair(CardData firstCard, CardView firstCardView, CardData secondCard, CardView secondCardView)
        {
            Debug.Log($"[GameManager] ProcessPair started for Cards {firstCard.CardId} and {secondCard.CardId}");

            // Wait for flip animations to complete
            yield return new WaitForSeconds(GameConstants.CARD_SELECTION_DELAY);

            // Validate cards are still valid for comparison
            if (firstCardView == null || secondCardView == null)
            {
                Debug.Log($"[GameManager] ProcessPair aborted - card views are null");
                cardsBeingProcessed.Remove(firstCard.CardId);
                cardsBeingProcessed.Remove(secondCard.CardId);
                yield break;
            }

            // Ensure we don't try to evaluate/flip back while either card is still animating.
            // This prevents FlipBack() from being ignored due to CardView's isFlipping guard.
            const float maxWaitSeconds = 2f;
            float waitElapsed = 0f;
            while ((firstCardView.IsFlipping || secondCardView.IsFlipping) && waitElapsed < maxWaitSeconds)
            {
                waitElapsed += Time.deltaTime;
                yield return null;

                if (firstCardView == null || secondCardView == null)
                {
                    Debug.Log("[GameManager] ProcessPair aborted while waiting - card view destroyed");
                    cardsBeingProcessed.Remove(firstCard.CardId);
                    cardsBeingProcessed.Remove(secondCard.CardId);
                    yield break;
                }
            }

            // Check if cards are already matched (might have been matched by another pair)
            if (firstCard.IsMatched || secondCard.IsMatched)
            {
                Debug.Log($"[GameManager] ProcessPair aborted - cards already matched");
                // Important: If one card got matched elsewhere, the other can be left face-up forever.
                // Clean up by flipping back any non-matched card that is still face-up.
                if (firstCardView != null && !firstCard.IsMatched && firstCardView.IsFlipped && !firstCardView.IsFlipping)
                {
                    Debug.Log($"[GameManager] ProcessPair cleanup: flipping back leftover first card {firstCard.CardId}");
                    firstCardView.FlipBack();
                }
                if (secondCardView != null && !secondCard.IsMatched && secondCardView.IsFlipped && !secondCardView.IsFlipping)
                {
                    Debug.Log($"[GameManager] ProcessPair cleanup: flipping back leftover second card {secondCard.CardId}");
                    secondCardView.FlipBack();
                }
                cardsBeingProcessed.Remove(firstCard.CardId);
                cardsBeingProcessed.Remove(secondCard.CardId);
                yield break;
            }

            // Check if cards are still flipped (user might have flipped them back)
            if (!firstCardView.IsFlipped || !secondCardView.IsFlipped)
            {
                Debug.Log($"[GameManager] ProcessPair aborted - cards no longer flipped");
                cardsBeingProcessed.Remove(firstCard.CardId);
                cardsBeingProcessed.Remove(secondCard.CardId);
                yield break;
            }

            // Process the match
            if (firstCard.SpriteId == secondCard.SpriteId)
            {
                // Match found!
                Debug.Log($"[GameManager] Match found! Cards: {firstCard.CardId} (Sprite {firstCard.SpriteId}) and {secondCard.CardId} (Sprite {secondCard.SpriteId})");
                
                // Small delay to let flip sound finish before playing match sound
                yield return new WaitForSeconds(GameConstants.AUDIO_RESULT_DELAY);
                
                audioService?.PlayCardMatch();
                scoreManager.OnCardMatched();

                // Set matched state BEFORE calling SetMatched (to prevent race conditions)
                firstCard.IsMatched = true;
                secondCard.IsMatched = true;

                firstCardView.SetMatched();
                secondCardView.SetMatched();

                // Check for win
                if (CheckGameWin())
                {
                    OnGameWin();
                }
            }
            else
            {
                // No match - flip back
                // We've already validated that cards are flipped and not matched above
                Debug.Log($"[GameManager] No match. Cards: {firstCard.CardId} (Sprite {firstCard.SpriteId}) and {secondCard.CardId} (Sprite {secondCard.SpriteId})");
                
                // Small delay to let flip sound finish before playing mismatch sound
                yield return new WaitForSeconds(GameConstants.AUDIO_RESULT_DELAY);
                
                audioService?.PlayCardMismatch();
                scoreManager.OnCardMismatch();

                // Flip back both cards (we've already validated they're valid, flipped, and not matched)
                // Only check if views are still valid (race condition protection)
                if (firstCardView != null && secondCardView != null)
                {
                    Debug.Log($"[GameManager] Flipping back both cards: {firstCard.CardId} and {secondCard.CardId}");
                    firstCardView.FlipBack();
                    secondCardView.FlipBack();
                }
                else
                {
                    Debug.Log($"[GameManager] Cannot flip back - views are null. First: {firstCardView == null}, Second: {secondCardView == null}");
                }

                // Keep the cards "reserved" until flip-back finishes to prevent re-pairing while animating.
                waitElapsed = 0f;
                while (firstCardView != null && secondCardView != null &&
                       (firstCardView.IsFlipping || secondCardView.IsFlipping) && waitElapsed < maxWaitSeconds)
                {
                    waitElapsed += Time.deltaTime;
                    yield return null;
                }
            }

            // Remove from processing set
            cardsBeingProcessed.Remove(firstCard.CardId);
            cardsBeingProcessed.Remove(secondCard.CardId);
            Debug.Log($"[GameManager] ProcessPair completed for Cards {firstCard.CardId} and {secondCard.CardId}");

            // Auto-save after each match check
            SaveGame();
        }

        private bool CheckGameWin()
        {
            foreach (var cardView in cards)
            {
                if (!cardView.CardData.IsMatched)
                {
                    return false;
                }
            }
            return true;
        }

        private void OnGameWin()
        {
            isGameActive = false;
            int finalScore = scoreManager.GetFinalScore(elapsedTime);

            audioService?.PlayGameOver();

            Debug.Log($"Game Won! Final Score: {finalScore}, Time: {elapsedTime:F1}s, Moves: {scoreManager.ScoreData.Moves}");

            // Delete save data on win
            saveService.DeleteSaveData();

            // Hide resume button immediately (no save data after win)
            if (gameUIView?.ResumeButton != null)
            {
                gameUIView.ResumeButton.gameObject.SetActive(false);
            }

            // Show win animation, then return to menu after 3 seconds
            gameUIView?.ShowWinAnimation(() => {
                // This callback is called after the win animation completes (3 seconds)
                gameUIView?.ShowGamePanel(false);
                gameUIView?.ShowMenuPanel(true);
                
                // Ensure resume button is still hidden after returning to menu
                if (gameUIView?.ResumeButton != null)
                {
                    gameUIView.ResumeButton.gameObject.SetActive(false);
                }
            });
        }

        public void GiveUpGame()
        {
            if (!isGameActive) return;

            isGameActive = false;
            SaveGame();
            gameUIView?.ShowGamePanel(false);
            gameUIView?.ShowMenuPanel(true);
            
            // Update resume button visibility (should be shown after giving up)
            UpdateResumeButtonVisibility();
        }

        /// <summary>
        /// Update resume button visibility based on save data availability
        /// </summary>
        private void UpdateResumeButtonVisibility()
        {
            if (gameUIView?.ResumeButton != null)
            {
                bool hasSaveData = saveService.HasSaveData();
                bool canResume = false;
                if (hasSaveData)
                {
                    GameData data = saveService.LoadGameData();
                    canResume = data != null && !IsSaveCompleted(data);
                    if (data != null && IsSaveCompleted(data))
                    {
                        // Clean up completed saves so button stays hidden next time
                        saveService.DeleteSaveData();
                    }
                }

                gameUIView.ResumeButton.gameObject.SetActive(canResume);
                
                if (canResume && gameUIView.ResumeButton.onClick.GetPersistentEventCount() == 0)
                {
                    // Re-attach listener if it was removed
                    gameUIView.ResumeButton.onClick.AddListener(ResumeGame);
                }
            }
        }

        /// <summary>
        /// Checks if saved game is fully completed (all cards matched).
        /// </summary>
        private bool IsSaveCompleted(GameData data)
        {
            if (data == null) return false;
            if (data.cardMatchedStates == null || data.cardIds == null) return false;
            if (data.cardMatchedStates.Length == 0 || data.cardMatchedStates.Length != data.cardIds.Length) return false;

            for (int i = 0; i < data.cardMatchedStates.Length; i++)
            {
                if (!data.cardMatchedStates[i]) return false;
            }
            return true;
        }

        private void SaveGame()
        {
            GameData gameData = new GameData
            {
                gridSize = gridSize,
                gridShape = (int)currentShape, // Save current shape
                elapsedTime = elapsedTime,
                score = scoreManager.ScoreData.TotalScore,
                moves = scoreManager.ScoreData.Moves,
                combos = scoreManager.ScoreData.Combos
            };

            // Save card states
            gameData.cardSpriteIds = new int[cards.Count];
            gameData.cardIds = new int[cards.Count];
            gameData.cardMatchedStates = new bool[cards.Count];
            gameData.cardFlippedStates = new bool[cards.Count];

            for (int i = 0; i < cards.Count; i++)
            {
                gameData.cardSpriteIds[i] = cards[i].CardData.SpriteId;
                gameData.cardIds[i] = cards[i].CardData.CardId;
                gameData.cardMatchedStates[i] = cards[i].CardData.IsMatched;
                gameData.cardFlippedStates[i] = cards[i].CardData.IsFlipped;
            }

            saveService.SaveGameData(gameData);
        }

        private void RestoreGameState(GameData savedData)
        {
            if (savedData == null || cards.Count != savedData.cardIds.Length)
            {
                Debug.LogError($"Cannot restore game state - data mismatch! Cards: {cards.Count}, Saved: {savedData?.cardIds?.Length ?? 0}");
                return;
            }

            Debug.Log($"[GameManager] Restoring game state: {savedData.cardIds.Length} cards, {savedData.moves} moves, {savedData.score} score");

            // Create a dictionary for faster lookup
            var cardDict = new System.Collections.Generic.Dictionary<int, CardView>();
            foreach (var cardView in cards)
            {
                if (cardView != null && cardView.CardData != null)
                {
                    cardDict[cardView.CardData.CardId] = cardView;
                }
            }

            int restoredCount = 0;
            int matchedCount = 0;
            int flippedCount = 0;

            for (int i = 0; i < savedData.cardIds.Length; i++)
            {
                int cardId = savedData.cardIds[i];
                
                if (cardDict.TryGetValue(cardId, out CardView cardView))
                {
                    bool isMatched = savedData.cardMatchedStates[i];
                    // Reset flipped cards on resume - only matched cards should remain matched
                    // All non-matched cards should be face-down (reset)
                    bool shouldBeFlipped = false; // Always reset flipped cards on resume

                    // Use RestoreState to set state without animation
                    cardView.RestoreState(shouldBeFlipped, isMatched);

                    restoredCount++;
                    if (isMatched) matchedCount++;
                    // Note: flippedCount is now always 0 since we reset all flipped cards
                }
                else
                {
                    Debug.LogWarning($"[GameManager] Card with ID {cardId} not found during restoration!");
                }
            }

            Debug.Log($"[GameManager] Game state restored: {restoredCount} cards ({matchedCount} matched, {flippedCount} flipped)");
        }

        private IEnumerator RevealCardsBriefly()
        {
            // Show all cards briefly
            foreach (var cardView in cards)
            {
                cardView.FlipCard();
            }

            yield return new WaitForSeconds(GameConstants.CARD_REVEAL_DURATION);

            // Flip all back
            foreach (var cardView in cards)
            {
                cardView.FlipBack();
            }

            yield return new WaitForSeconds(GameConstants.CARD_FLIP_DURATION);
        }

        private void OnShapeChanged(GridShape newShape)
        {
            currentShape = newShape;
            Debug.Log($"[GameManager] Shape changed to: {newShape}");
            // Grid size remains the same, only shape changes
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && isGameActive)
            {
                SaveGame();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && isGameActive)
            {
                SaveGame();
            }
        }
    }
}

