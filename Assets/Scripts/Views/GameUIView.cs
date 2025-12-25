using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CardMatch.Utils;
#if DOTWEEN_AVAILABLE
using DG.Tweening;
#endif

namespace CardMatch.Views
{
    /// <summary>
    /// UI View for game information display
    /// </summary>
    public class GameUIView : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_Text timeLabel;
        [SerializeField] private TMP_Text scoreLabel;
        [SerializeField] private TMP_Text movesLabel;
        [SerializeField] private TMP_Text comboLabel;
        [SerializeField] private TMP_Text gridSizeLabel;
        [SerializeField] private TMP_Text winLabel; // "You Win" text
        [SerializeField] private Button[] gridSizeButtons = new Button[6]; // 6 buttons for grid size selection
        [SerializeField] private GameObject gamePanel;
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private Button startButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button newGameButton;

        [Header("Animation Settings")]
        [SerializeField] private float comboAnimationDuration = 0.5f;
        [SerializeField] private float scoreAnimationDuration = 0.3f;
        [SerializeField] private float comboScaleAmount = 1.3f;
        [SerializeField] private float scoreScaleAmount = 1.2f;

        private Vector3 originalComboScale;
        private Vector3 originalScoreScale;
        private int previousScore = 0;
        private int previousCombo = 0;
        private float lastComboUpdateTime = 0f;
        private const float COMBO_HIDE_DELAY = 3f; // Hide combo text after 3 seconds of inactivity
#if DOTWEEN_AVAILABLE
        private Sequence continuousPulseSequence; // Store continuous pulse animation
        private Sequence[] gridButtonAnimations; // Store animations for each grid size button
        private Sequence winAnimationSequence; // Store win text animation
        private Sequence winIdleSequence; // Store win text idle animation
#endif
        private Vector3 originalWinScale; // Store original scale for win text
        private Vector3[] originalButtonScales; // Store original scales for grid size buttons
        private Vector3[] originalButtonPositions; // Store original positions for grid size buttons

        public Button[] GridSizeButtons => gridSizeButtons;
        public Button StartButton => startButton;
        public Button BackButton => backButton;
        public Button ResumeButton => resumeButton;
        public Button NewGameButton => newGameButton;
        
        private int selectedGridSize = GameConstants.DEFAULT_GRID_SIZE;
        public int SelectedGridSize => selectedGridSize;
        
        private GridShape selectedShape = GridShape.Square;
        public GridShape SelectedShape => selectedShape;

        private void Awake()
        {
            // Store original scales for animations
            if (comboLabel != null)
            {
                originalComboScale = comboLabel.transform.localScale;
            }
            if (scoreLabel != null)
            {
                originalScoreScale = scoreLabel.transform.localScale;
            }
            if (winLabel != null)
            {
                originalWinScale = winLabel.transform.localScale;
                winLabel.gameObject.SetActive(false); // Hide win label initially
            }
            
            // Initialize grid button animation arrays
#if DOTWEEN_AVAILABLE
            if (gridSizeButtons != null && gridSizeButtons.Length > 0)
            {
                gridButtonAnimations = new Sequence[gridSizeButtons.Length];
            }
#endif
            if (gridSizeButtons != null && gridSizeButtons.Length > 0)
            {
                originalButtonScales = new Vector3[gridSizeButtons.Length];
                originalButtonPositions = new Vector3[gridSizeButtons.Length];
                
                // Store original scales and positions
                for (int i = 0; i < gridSizeButtons.Length; i++)
                {
                    if (gridSizeButtons[i] != null)
                    {
                        originalButtonScales[i] = gridSizeButtons[i].transform.localScale;
                        originalButtonPositions[i] = gridSizeButtons[i].transform.localPosition;
                    }
                }
            }
        }

        private void Update()
        {
            // Hide combo text after inactivity delay, but only if combo is still >= 2
            // (If combo drops below 2, it will be hidden immediately in UpdateCombo)
            if (comboLabel != null && previousCombo >= 2 && comboLabel.text != "" && lastComboUpdateTime > 0f)
            {
                if (Time.time - lastComboUpdateTime >= COMBO_HIDE_DELAY)
                {
                    Debug.Log($"[GameUIView] Hiding combo text after {COMBO_HIDE_DELAY}s of inactivity");
                    AnimateComboHide();
                    lastComboUpdateTime = 0f; // Reset timer (0 means "hidden due to inactivity")
                }
            }
        }

        private void OnDestroy()
        {
            // Kill any running tweens when object is destroyed
#if DOTWEEN_AVAILABLE
            if (comboLabel != null)
            {
                comboLabel.transform.DOKill();
                comboLabel.DOKill();
            }
            if (scoreLabel != null)
            {
                scoreLabel.transform.DOKill();
                scoreLabel.DOKill();
            }
            if (continuousPulseSequence != null && continuousPulseSequence.IsActive())
            {
                continuousPulseSequence.Kill();
                continuousPulseSequence = null;
            }
            
            // Kill grid button animations
            if (gridButtonAnimations != null)
            {
                for (int i = 0; i < gridButtonAnimations.Length; i++)
                {
                    if (gridButtonAnimations[i] != null && gridButtonAnimations[i].IsActive())
                    {
                        gridButtonAnimations[i].Kill();
                        gridButtonAnimations[i] = null;
                    }
                }
            }
            
            // Kill all button animations
            if (gridSizeButtons != null)
            {
                for (int i = 0; i < gridSizeButtons.Length; i++)
                {
                    if (gridSizeButtons[i] != null)
                    {
                        gridSizeButtons[i].transform.DOKill();
                    }
                }
            }
#endif
        }


        public void UpdateScore(int score)
        {
            if (scoreLabel != null)
            {
                scoreLabel.text = $"Score: {score}";
                
                // Animate score text when it changes
                if (score != previousScore && score > previousScore)
                {
                    AnimateScoreText();
                }
                previousScore = score;
            }
        }

        private void AnimateScoreText()
        {
            if (scoreLabel == null) return;

#if DOTWEEN_AVAILABLE
            // Kill any existing animation
            scoreLabel.transform.DOKill();
            
            // Reset to original scale
            scoreLabel.transform.localScale = originalScoreScale;
            
            // Bounce animation: scale up then back down
            Sequence scoreSequence = DOTween.Sequence();
            scoreSequence.Append(scoreLabel.transform.DOScale(originalScoreScale * scoreScaleAmount, scoreAnimationDuration * 0.5f).SetEase(Ease.OutBack));
            scoreSequence.Append(scoreLabel.transform.DOScale(originalScoreScale, scoreAnimationDuration * 0.5f).SetEase(Ease.InBack));
            
            // Optional: Add a subtle color flash
            Color originalColor = scoreLabel.color;
            scoreSequence.Join(scoreLabel.DOColor(new Color(1f, 1f, 0.5f, 1f), scoreAnimationDuration * 0.3f)
                .OnComplete(() => scoreLabel.DOColor(originalColor, scoreAnimationDuration * 0.2f)));
#else
            // Fallback: Simple scale animation without DOTween
            StartCoroutine(AnimateScoreTextCoroutine());
#endif
        }

#if !DOTWEEN_AVAILABLE
        private System.Collections.IEnumerator AnimateScoreTextCoroutine()
        {
            if (scoreLabel == null) yield break;
            
            Vector3 startScale = originalScoreScale;
            Vector3 targetScale = originalScoreScale * scoreScaleAmount;
            float elapsed = 0f;
            float halfDuration = scoreAnimationDuration * 0.5f;
            
            // Scale up
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                scoreLabel.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }
            
            // Scale down
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                scoreLabel.transform.localScale = Vector3.Lerp(targetScale, startScale, t);
                yield return null;
            }
            
            scoreLabel.transform.localScale = startScale;
        }
#endif

        public void UpdateMoves(int moves)
        {
            if (movesLabel != null)
            {
                movesLabel.text = $"Moves: {moves}";
            }
        }

        public void UpdateCombo(int combo, int multiplier)
        {
            if (comboLabel == null)
            {
                Debug.LogWarning("[GameUIView] Combo label is null! Cannot update combo text.");
                return;
            }

            // Only show combo if user has 2 or more consecutive matches
            if (combo >= 2)
            {
                // Only show/update text when combo increases (not when it stays the same)
                // This prevents re-showing the text if it was hidden due to inactivity
                if (combo > previousCombo)
                {
                    // Display the actual combo count: 2x, 3x, 4x, etc.
                    comboLabel.text = $"Combo: {combo}x";
                    comboLabel.color = Color.yellow;
                    
                    // Ensure label is active and visible
                    if (!comboLabel.gameObject.activeSelf)
                    {
                        comboLabel.gameObject.SetActive(true);
                    }
                    
                    // Reset hide timer when combo increases
                    lastComboUpdateTime = Time.time;
                    Debug.Log($"[GameUIView] Combo increased to {combo}x - showing and resetting hide timer");
                    
                    // Animate combo text when it increases
                    AnimateComboText(combo, multiplier);
                }
                // If combo is the same and text was hidden (lastComboUpdateTime == 0), don't re-show it
                // If combo is the same and text is visible, keep it visible (Update() will handle hiding)
            }
            else
            {
                // Hide combo text immediately if less than 2 consecutive matches
                if (comboLabel.text != "")
                {
                    AnimateComboHide();
                }
                // Reset combo animation when combo is lost
                if (previousCombo >= 2)
                {
                    Debug.Log($"[GameUIView] Combo dropped below 2 - hiding immediately");
                }
                lastComboUpdateTime = 0f; // Reset timer
            }
            previousCombo = combo;
        }

        /// <summary>
        /// Animates the combo text fading out and scaling down
        /// </summary>
        private void AnimateComboHide()
        {
            if (comboLabel == null) return;

#if DOTWEEN_AVAILABLE
            // Kill all animations including continuous pulse
            comboLabel.transform.DOKill();
            comboLabel.DOKill();
            if (continuousPulseSequence != null && continuousPulseSequence.IsActive())
            {
                continuousPulseSequence.Kill();
                continuousPulseSequence = null;
            }
            
            // Dramatic fade-out animation
            Sequence hideSequence = DOTween.Sequence();
            
            // Phase 1: Scale down and rotate out (0.0s - 0.3s)
            hideSequence.Append(comboLabel.transform.DOScale(originalComboScale * 0.5f, 0.3f)
                .SetEase(Ease.InBack));
            hideSequence.Join(comboLabel.transform.DORotate(new Vector3(0, 0, -90f), 0.3f, RotateMode.FastBeyond360)
                .SetEase(Ease.InBack));
            
            // Phase 2: Fade out with color shift (simultaneous)
            hideSequence.Join(comboLabel.DOFade(0f, 0.3f)
                .SetEase(Ease.InQuad));
            hideSequence.Join(comboLabel.DOColor(new Color(1f, 0.5f, 0f, 0f), 0.3f)); // Fade to orange transparent
            
            // Phase 3: Clear text and reset state
            hideSequence.AppendCallback(() => {
                comboLabel.text = "";
                comboLabel.gameObject.SetActive(false);
                comboLabel.transform.localScale = originalComboScale;
                comboLabel.transform.localRotation = Quaternion.identity;
                comboLabel.color = Color.yellow;
                comboLabel.alpha = 1f; // Reset alpha for next time
            });
            
            hideSequence.Play();
#else
            // Fallback: instant hide
            comboLabel.text = "";
            comboLabel.gameObject.SetActive(false);
            comboLabel.transform.localScale = originalComboScale;
            comboLabel.transform.localRotation = Quaternion.identity;
            comboLabel.color = Color.yellow;
#endif
        }

        private void AnimateComboText(int combo, int multiplier)
        {
            if (comboLabel == null) return;

#if DOTWEEN_AVAILABLE
            // Kill any existing animation
            comboLabel.transform.DOKill();
            comboLabel.DOKill();
            
            // Reset to original state
            comboLabel.transform.localScale = originalComboScale;
            comboLabel.transform.localRotation = Quaternion.identity;
            
            // Dynamic color based on combo level
            Color comboColor = GetComboColor(combo);
            Color originalColor = Color.yellow;
            
            // Calculate animation intensity based on combo level
            float intensity = Mathf.Min(1f + (combo - 2) * 0.2f, 2f); // More intense for higher combos
            float scaleMultiplier = comboScaleAmount * intensity;
            
            // Create epic combo animation sequence
            Sequence comboSequence = DOTween.Sequence();
            
            // Phase 1: EXPLOSIVE ENTRANCE (0.0s - 0.3s)
            // Massive scale up with elastic bounce
            comboSequence.Append(comboLabel.transform.DOScale(originalComboScale * scaleMultiplier * 1.5f, 0.15f)
                .SetEase(Ease.OutBack));
            
            // Simultaneous: Color flash to combo color
            comboSequence.Join(comboLabel.DOColor(comboColor, 0.15f));
            
            // Simultaneous: Position shake for impact
            comboSequence.Join(comboLabel.transform.DOShakePosition(0.2f, strength: 15f * intensity, vibrato: 20, randomness: 90f, snapping: false, fadeOut: true));
            
            // Phase 2: PULSE EFFECT (0.3s - 0.6s)
            // Scale down slightly then pulse
            comboSequence.Append(comboLabel.transform.DOScale(originalComboScale * scaleMultiplier, 0.15f)
                .SetEase(Ease.InOutQuad));
            
            // Pulsing scale animation (grows and shrinks)
            comboSequence.Append(comboLabel.transform.DOScale(originalComboScale * scaleMultiplier * 1.1f, 0.1f)
                .SetEase(Ease.InOutSine));
            comboSequence.Append(comboLabel.transform.DOScale(originalComboScale * scaleMultiplier, 0.1f)
                .SetEase(Ease.InOutSine));
            
            // Phase 3: ROTATION & SHAKE (for high combos)
            if (combo >= 3)
            {
                // Rotation shake with increasing intensity
                float rotationStrength = 15f + (combo - 3) * 5f;
                comboSequence.Join(comboLabel.transform.DOShakeRotation(0.4f, strength: rotationStrength, vibrato: 15, randomness: 90f, fadeOut: true));
            }
            
            // Phase 4: SMOOTH RETURN (0.6s - 1.0s)
            // Scale back to normal with smooth ease
            comboSequence.Append(comboLabel.transform.DOScale(originalComboScale, 0.4f)
                .SetEase(Ease.OutElastic));
            
            // Color fade back to yellow
            comboSequence.Join(comboLabel.DOColor(originalColor, 0.4f)
                .SetEase(Ease.InOutQuad));
            
            // Phase 5: CONTINUOUS PULSE (for high combos)
            if (combo >= 4)
            {
                // Kill any existing continuous pulse
                if (continuousPulseSequence != null && continuousPulseSequence.IsActive())
                {
                    continuousPulseSequence.Kill();
                }
                
                // Add a subtle continuous pulsing effect
                continuousPulseSequence = DOTween.Sequence();
                continuousPulseSequence.Append(comboLabel.transform.DOScale(originalComboScale * 1.05f, 0.5f)
                    .SetEase(Ease.InOutSine));
                continuousPulseSequence.Append(comboLabel.transform.DOScale(originalComboScale, 0.5f)
                    .SetEase(Ease.InOutSine));
                continuousPulseSequence.SetLoops(-1); // Infinite loop
                continuousPulseSequence.Play();
            }
            else
            {
                // Stop continuous pulse if combo drops below 4
                if (continuousPulseSequence != null && continuousPulseSequence.IsActive())
                {
                    continuousPulseSequence.Kill();
                    continuousPulseSequence = null;
                }
            }
            
            // Add callback for when animation completes
            comboSequence.OnComplete(() => {
                // Ensure final state
                comboLabel.transform.localScale = originalComboScale;
                comboLabel.color = originalColor;
            });
            
            // Set sequence to play
            comboSequence.Play();
#else
            // Fallback: Simple scale animation without DOTween
            StartCoroutine(AnimateComboTextCoroutine(combo));
#endif
        }

        private Color GetComboColor(int combo)
        {
            // Color gradient based on combo level
            if (combo >= 5) return new Color(1f, 0f, 1f, 1f); // Magenta for 5+
            if (combo >= 4) return new Color(1f, 0.5f, 0f, 1f); // Orange for 4
            if (combo >= 3) return new Color(1f, 0.8f, 0f, 1f); // Gold for 3
            return new Color(1f, 1f, 0.5f, 1f); // Light yellow for 1-2
        }

        private void ResetComboText()
        {
            if (comboLabel == null) return;

#if DOTWEEN_AVAILABLE
            // Kill all animations including continuous pulse
            comboLabel.transform.DOKill();
            comboLabel.DOKill();
            if (continuousPulseSequence != null && continuousPulseSequence.IsActive())
            {
                continuousPulseSequence.Kill();
                continuousPulseSequence = null;
            }
            
            // Smooth fade out and reset
            Sequence resetSequence = DOTween.Sequence();
            resetSequence.Append(comboLabel.transform.DOScale(originalComboScale * 0.8f, 0.15f)
                .SetEase(Ease.InBack));
            resetSequence.Join(comboLabel.DOColor(new Color(1f, 1f, 0f, 0.5f), 0.15f)); // Fade to semi-transparent yellow
            resetSequence.AppendCallback(() => {
                comboLabel.transform.localScale = originalComboScale;
                comboLabel.color = Color.yellow;
            });
            resetSequence.Append(comboLabel.DOColor(Color.yellow, 0.1f)); // Fade back in
#else
            comboLabel.transform.localScale = originalComboScale;
            comboLabel.color = Color.yellow;
#endif
        }

#if !DOTWEEN_AVAILABLE
        private System.Collections.IEnumerator AnimateComboTextCoroutine(int combo)
        {
            if (comboLabel == null) yield break;
            
            Vector3 startScale = originalComboScale;
            Vector3 targetScale = originalComboScale * comboScaleAmount;
            float elapsed = 0f;
            float upDuration = comboAnimationDuration * 0.4f;
            float downDuration = comboAnimationDuration * 0.6f;
            
            Color originalColor = comboLabel.color;
            Color targetColor = GetComboColor(combo);
            
            // Scale up
            while (elapsed < upDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / upDuration;
                comboLabel.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                comboLabel.color = Color.Lerp(originalColor, targetColor, t);
                yield return null;
            }
            
            // Scale down
            elapsed = 0f;
            while (elapsed < downDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / downDuration;
                comboLabel.transform.localScale = Vector3.Lerp(targetScale, startScale, t);
                comboLabel.color = Color.Lerp(targetColor, originalColor, t);
                yield return null;
            }
            
            comboLabel.transform.localScale = startScale;
            comboLabel.color = originalColor;
        }
#endif

        public void UpdateGridSize(int size)
        {
            if (gridSizeLabel != null)
            {
                gridSizeLabel.text = $"{size} x {size}";
            }
        }

        public void ShowGamePanel(bool show)
        {
            if (gamePanel != null)
            {
                gamePanel.SetActive(show);
            }
        }

        public void ShowMenuPanel(bool show)
        {
            if (menuPanel != null)
            {
                menuPanel.SetActive(show);
            }
        }

        /// <summary>
        /// Sets up shape selection buttons with click listeners (using grid size buttons)
        /// </summary>
        public void SetupGridSizeButtons(System.Action<GridShape> onShapeSelected, GridShape defaultShape = GridShape.Square)
        {
            if (gridSizeButtons == null || gridSizeButtons.Length == 0)
            {
                Debug.LogWarning("[GameUIView] Grid size buttons array is null or empty!");
                return;
            }

            // Define shapes for each button
            GridShape[] shapes = new GridShape[]
            {
                GridShape.Square,
                GridShape.Diamond,  // Changed from VShape
                GridShape.OShape,
                GridShape.LShape,
                GridShape.Heart,    // Changed from Plus
                GridShape.Diamond  // Keep Diamond for 6th button (or could be removed)
            };

            // Clear existing listeners and set up new ones
            for (int i = 0; i < gridSizeButtons.Length && i < shapes.Length; i++)
            {
                if (gridSizeButtons[i] != null)
                {
                    // Remove all existing listeners
                    gridSizeButtons[i].onClick.RemoveAllListeners();
                    
                    GridShape shape = shapes[i];
                    
                    // Update button text to show shape name
                    var buttonText = gridSizeButtons[i].GetComponentInChildren<TMP_Text>();
                    if (buttonText != null)
                    {
                        buttonText.text = GetShapeDisplayName(shape);
                    }
                    
                    // Add listener with captured shape
                    GridShape capturedShape = shape; // Capture for closure
                    gridSizeButtons[i].onClick.AddListener(() => OnShapeButtonClicked(capturedShape, onShapeSelected));
                    
                    Debug.Log($"[GameUIView] Set up shape button {i} for shape: {capturedShape}");
                }
            }
            
            // Set selection (use provided default or Square)
            selectedShape = defaultShape;
            UpdateShapeButtonSelection(selectedShape);
        }

        /// <summary>
        /// Gets display name for a shape
        /// </summary>
        private string GetShapeDisplayName(GridShape shape)
        {
            switch (shape)
            {
                case GridShape.Square: return "Square";
                case GridShape.VShape: return "V";
                case GridShape.OShape: return "O";
                case GridShape.LShape: return "L";
                case GridShape.Plus: return "Plus";
                case GridShape.Diamond: return "Diamond";
                case GridShape.Heart: return "Heart";
                default: return shape.ToString();
            }
        }

        /// <summary>
        /// Called when a shape button is clicked
        /// </summary>
        private void OnShapeButtonClicked(GridShape shape, System.Action<GridShape> onShapeSelected)
        {
            selectedShape = shape;
            UpdateShapeButtonSelection(shape);
            
            Debug.Log($"[GameUIView] Shape button clicked: {shape}");
            
            // Notify the callback
            onShapeSelected?.Invoke(shape);
        }

        /// <summary>
        /// Updates button visual states to show which shape is selected
        /// </summary>
        private void UpdateShapeButtonSelection(GridShape selectedShape)
        {
            if (gridSizeButtons == null) return;

            GridShape[] shapes = new GridShape[]
            {
                GridShape.Square,
                GridShape.Diamond,  // Changed from VShape
                GridShape.OShape,
                GridShape.LShape,
                GridShape.Heart,    // Changed from Plus
                GridShape.Diamond  // Keep Diamond for 6th button
            };

            for (int i = 0; i < gridSizeButtons.Length && i < shapes.Length; i++)
            {
                if (gridSizeButtons[i] != null)
                {
                    GridShape buttonShape = shapes[i];
                    bool isSelected = (buttonShape == selectedShape);
                    
                    // Reset all buttons first, then animate the selected one
                    ResetGridButton(i);
                    
                    if (isSelected)
                    {
                        AnimateGridButton(i);
                    }
                }
            }
        }

        /// <summary>
        /// Resets a grid button to its original state
        /// </summary>
        private void ResetGridButton(int buttonIndex)
        {
            if (buttonIndex < 0 || buttonIndex >= gridSizeButtons.Length || gridSizeButtons[buttonIndex] == null)
                return;

#if DOTWEEN_AVAILABLE
            // Kill any existing animation for this button
            if (gridButtonAnimations != null && buttonIndex < gridButtonAnimations.Length)
            {
                if (gridButtonAnimations[buttonIndex] != null && gridButtonAnimations[buttonIndex].IsActive())
                {
                    gridButtonAnimations[buttonIndex].Kill();
                    gridButtonAnimations[buttonIndex] = null;
                }
            }
            
            // Kill all tweens on this transform
            gridSizeButtons[buttonIndex].transform.DOKill();
            
            // Only animate scale back to normal (no position changes needed)
            if (originalButtonScales != null && buttonIndex < originalButtonScales.Length)
            {
                gridSizeButtons[buttonIndex].transform.DOScale(originalButtonScales[buttonIndex], 0.2f);
            }
#else
            // Fallback: just reset scale instantly (no position changes)
            if (originalButtonScales != null && buttonIndex < originalButtonScales.Length)
            {
                gridSizeButtons[buttonIndex].transform.localScale = originalButtonScales[buttonIndex];
            }
#endif
        }

        /// <summary>
        /// Animates a grid button to show it's selected (scale up and float)
        /// </summary>
        private void AnimateGridButton(int buttonIndex)
        {
            if (buttonIndex < 0 || buttonIndex >= gridSizeButtons.Length || gridSizeButtons[buttonIndex] == null)
                return;

#if DOTWEEN_AVAILABLE
            // Kill any existing animation first
            if (gridButtonAnimations != null && buttonIndex < gridButtonAnimations.Length)
            {
                if (gridButtonAnimations[buttonIndex] != null && gridButtonAnimations[buttonIndex].IsActive())
                {
                    gridButtonAnimations[buttonIndex].Kill();
                }
            }
            
            Transform buttonTransform = gridSizeButtons[buttonIndex].transform;
            Vector3 originalScale = originalButtonScales != null && buttonIndex < originalButtonScales.Length 
                ? originalButtonScales[buttonIndex] 
                : Vector3.one;
            
            // Scale up slightly (1.15x) - simple and clean
            Vector3 scaledSize = originalScale * 1.15f;
            buttonTransform.DOScale(scaledSize, 0.3f).SetEase(Ease.OutBack);
#else
            if (originalButtonScales != null && buttonIndex < originalButtonScales.Length)
            {
                gridSizeButtons[buttonIndex].transform.localScale = originalButtonScales[buttonIndex] * 1.15f;
            }
#endif
        }

        /// <summary>
        /// Shows "You Win" text with DOTween animation, stays for 3 seconds, then returns to menu
        /// </summary>
        public void ShowWinAnimation(System.Action onComplete)
        {
            if (winLabel == null)
            {
                Debug.LogWarning("[GameUIView] Win label is not assigned!");
                onComplete?.Invoke();
                return;
            }

            // Hide combo text when win appears
            HideComboText();

#if DOTWEEN_AVAILABLE
            // Kill any existing win animations
            if (winAnimationSequence != null && winAnimationSequence.IsActive())
            {
                winAnimationSequence.Kill();
            }
            if (winIdleSequence != null && winIdleSequence.IsActive())
            {
                winIdleSequence.Kill();
            }
            winLabel.transform.DOKill();
            winLabel.DOKill();

            // Reset win label state
            winLabel.text = "YOU WIN!";
            winLabel.gameObject.SetActive(true);
            winLabel.transform.localScale = Vector3.zero;
            winLabel.transform.localRotation = Quaternion.identity;
            winLabel.color = Color.white;
            winLabel.alpha = 0f; // Start transparent

            // Store original position
            Vector3 originalPos = winLabel.transform.localPosition;

            // Create impressive entrance animation sequence
            winAnimationSequence = DOTween.Sequence();

            // Step 1: Dramatic entrance - scale up BIG with rotation and fade in
            winAnimationSequence.Append(winLabel.transform.DOScale(originalWinScale * 2.5f, 0.6f).SetEase(Ease.OutBack));
            winAnimationSequence.Join(winLabel.transform.DORotate(new Vector3(0, 0, 360f), 0.6f, RotateMode.FastBeyond360).SetEase(Ease.OutCubic));
            winAnimationSequence.Join(winLabel.DOFade(1f, 0.6f));
            winAnimationSequence.Join(winLabel.DOColor(Color.yellow, 0.6f));

            // Step 2: Bounce back with color change
            winAnimationSequence.Append(winLabel.transform.DOScale(originalWinScale * 0.8f, 0.2f).SetEase(Ease.InQuad));
            winAnimationSequence.Join(winLabel.DOColor(Color.green, 0.2f));

            // Step 3: Scale up to normal with final color
            winAnimationSequence.Append(winLabel.transform.DOScale(originalWinScale * 1.2f, 0.3f).SetEase(Ease.OutElastic));
            winAnimationSequence.Join(winLabel.DOColor(new Color(1f, 0.8f, 0f), 0.3f)); // Gold color

            // Step 4: Settle to normal size
            winAnimationSequence.Append(winLabel.transform.DOScale(originalWinScale, 0.2f).SetEase(Ease.InOutQuad));

            // Step 5: Start impressive idle animation (continuous floating/pulsing/rotating)
            winAnimationSequence.AppendCallback(() => {
                StartWinIdleAnimation();
            });

            // Step 6: Wait 3 seconds
            winAnimationSequence.AppendInterval(3f);

            // Step 7: Dramatic exit - rotate, fade out, and scale down
            winAnimationSequence.Append(winLabel.transform.DORotate(new Vector3(0, 0, -180f), 0.5f, RotateMode.FastBeyond360).SetEase(Ease.InBack));
            winAnimationSequence.Join(winLabel.DOFade(0f, 0.5f));
            winAnimationSequence.Join(winLabel.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack));

            // Step 8: Hide and call completion callback
            winAnimationSequence.AppendCallback(() => {
                winLabel.gameObject.SetActive(false);
                winLabel.transform.localRotation = Quaternion.identity;
                if (winIdleSequence != null && winIdleSequence.IsActive())
                {
                    winIdleSequence.Kill();
                    winIdleSequence = null;
                }
                onComplete?.Invoke();
            });

            winAnimationSequence.Play();
#else
            // Fallback without DOTween
            winLabel.text = "YOU WIN!";
            winLabel.gameObject.SetActive(true);
            StartCoroutine(ShowWinFallback(onComplete));
#endif
        }

        /// <summary>
        /// Hides the combo text
        /// </summary>
        private void HideComboText()
        {
            if (comboLabel != null)
            {
#if DOTWEEN_AVAILABLE
                comboLabel.transform.DOKill();
                comboLabel.DOKill();
                comboLabel.DOFade(0f, 0.3f).OnComplete(() => {
                    comboLabel.text = "";
                    comboLabel.gameObject.SetActive(false);
                    ResetComboText();
                });
#else
                comboLabel.text = "";
                comboLabel.gameObject.SetActive(false);
                ResetComboText();
#endif
            }
        }

        /// <summary>
        /// Starts the idle animation for win text (continuous floating/pulsing/rotating/color changing)
        /// </summary>
        private void StartWinIdleAnimation()
        {
            if (winLabel == null) return;

#if DOTWEEN_AVAILABLE
            // Kill existing idle animation
            if (winIdleSequence != null && winIdleSequence.IsActive())
            {
                winIdleSequence.Kill();
            }

            // Store original position for floating
            Vector3 originalPos = winLabel.transform.localPosition;

            // Create impressive continuous animation
            winIdleSequence = DOTween.Sequence();

            // Floating animation (up and down - more dramatic)
            winIdleSequence.Append(winLabel.transform.DOLocalMoveY(originalPos.y + 15f, 1.2f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo));

            // Pulsing scale animation (more dramatic scale variation)
            winIdleSequence.Join(winLabel.transform.DOScale(originalWinScale * 1.15f, 1f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo));

            // Subtle rotation animation (gentle sway)
            winIdleSequence.Join(winLabel.transform.DORotate(new Vector3(0, 0, 5f), 1.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo));

            // Color pulsing animation (between gold and yellow)
            winIdleSequence.Join(winLabel.DOColor(new Color(1f, 0.9f, 0.3f), 1.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo));

            winIdleSequence.Play();
#endif
        }

#if !DOTWEEN_AVAILABLE
        /// <summary>
        /// Fallback coroutine for showing win text without DOTween
        /// </summary>
        private System.Collections.IEnumerator ShowWinFallback(System.Action onComplete)
        {
            yield return new WaitForSeconds(3f);
            winLabel.gameObject.SetActive(false);
            onComplete?.Invoke();
        }
#endif
    }
}

