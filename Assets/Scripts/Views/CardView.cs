using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using CardMatch.Models;
using CardMatch.Utils;
#if DOTWEEN_AVAILABLE
using DG.Tweening;
#endif

namespace CardMatch.Views
{
    /// <summary>
    /// View component for card display and animations
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class CardView : MonoBehaviour
    {
        [SerializeField] private Image cardImage;
        [SerializeField] private Button cardButton;

        private CardData cardData;
        private Sprite cardFrontSprite;
        private Sprite cardBackSprite;
        private bool isFlipping = false;
        private Vector3 originalScale;
#if DOTWEEN_AVAILABLE
        private Tween flipTween;
        private Tween matchTween;
#endif

        public CardData CardData => cardData;
        public bool IsFlipping => isFlipping;
        public bool IsFlipped => cardData?.IsFlipped ?? false;

        private void Awake()
        {
            if (cardButton == null)
            {
                cardButton = GetComponent<Button>();
            }

            if (cardImage == null)
            {
                cardImage = GetComponent<Image>();
            }

            if (cardButton == null)
            {
                Debug.LogError($"[CardView] Button component not found on {gameObject.name}! Card clicks will not work.");
                return;
            }

            if (!cardButton.interactable)
            {
                Debug.LogWarning($"[CardView] Button is not interactable on {gameObject.name}. Enable it for clicks to work.");
            }

            cardButton.onClick.AddListener(OnCardButtonClicked);
            Debug.Log($"[CardView] Button listener added to {gameObject.name}");
        }

        public void Initialize(CardData data, Sprite frontSprite, Sprite backSprite)
        {
            cardData = data;
            cardFrontSprite = frontSprite;
            cardBackSprite = backSprite;

            // Store original scale (set by CardManager for grid layout)
            originalScale = transform.localScale;

            // Start with card back
            cardData.IsFlipped = false;
            
            // Ensure button is interactable
            if (cardButton != null)
            {
                cardButton.interactable = true;
            }
            
            // Ensure image can receive raycasts
            if (cardImage != null)
            {
                cardImage.raycastTarget = true;
            }
            
            UpdateCardSprite();
            ResetRotation();
            
            Debug.Log($"[CardView] Card initialized: {gameObject.name}, IsFlipped: {cardData.IsFlipped}, Original scale: {originalScale}, Button interactable: {cardButton?.interactable}");
        }

        public void FlipCard()
        {
            if (isFlipping) return;

            isFlipping = true;
            cardData.IsFlipping = true;
#if DOTWEEN_AVAILABLE
            PlayFlipTween();
#else
            StartCoroutine(FlipAnimation());
#endif
        }

        public void FlipBack()
        {
            if (isFlipping) return;

            isFlipping = true;
            cardData.IsFlipping = true;
#if DOTWEEN_AVAILABLE
            PlayFlipTween();
#else
            StartCoroutine(FlipAnimation());
#endif
        }

        public void SetMatched()
        {
            cardData.IsMatched = true;
            cardButton.interactable = false;
#if DOTWEEN_AVAILABLE
            PlayMatchTween();
#else
            StartCoroutine(MatchEffectCoroutine());
#endif
        }

        public void SetInteractable(bool interactable)
        {
            cardButton.interactable = interactable && !cardData.IsMatched;
        }

        public void ResetCard()
        {
            cardData.IsFlipped = false;
            cardData.IsMatched = false;
            cardData.IsFlipping = false;
            isFlipping = false;
            cardButton.interactable = true;
            cardImage.color = Color.white;
            UpdateCardSprite();
            ResetRotation();
            // Restore original scale
            if (originalScale != Vector3.zero)
            {
                transform.localScale = originalScale;
            }
#if DOTWEEN_AVAILABLE
            flipTween?.Kill();
            matchTween?.Kill();
            cardImage.color = Color.white;
#endif
        }

        /// <summary>
        /// Restore card state without animation (for game resume)
        /// </summary>
        public void RestoreState(bool isFlipped, bool isMatched)
        {
            if (cardData == null) return;

            cardData.IsFlipped = isFlipped;
            cardData.IsMatched = isMatched;
            cardData.IsFlipping = false;
            isFlipping = false;

            if (isMatched)
            {
                cardButton.interactable = false;
                cardImage.color = new Color(1f, 1f, 1f, 0.5f); // Semi-transparent for matched cards
            }
            else
            {
                cardButton.interactable = true;
                cardImage.color = Color.white;
            }

            // Restore original scale (important for resume - ensures card is properly scaled)
            if (originalScale != Vector3.zero)
            {
                transform.localScale = originalScale;
            }

            UpdateCardSprite();
            ResetRotation();
        }

        public event Action<CardData> OnCardClicked;

        private void OnCardButtonClicked()
        {
            // Debug logging to help identify issues
            if (cardData == null)
            {
                Debug.LogWarning($"[CardView] Card clicked but cardData is null on {gameObject.name}. Card not initialized.");
                return;
            }

            if (isFlipping)
            {
                Debug.Log($"[CardView] Card click ignored - card is flipping on {gameObject.name}");
                return;
            }

            if (cardData.IsFlipped)
            {
                Debug.Log($"[CardView] Card click ignored - card already flipped on {gameObject.name}");
                return;
            }

            if (cardData.IsMatched)
            {
                Debug.Log($"[CardView] Card click ignored - card already matched on {gameObject.name}");
                return;
            }

            Debug.Log($"[CardView] Card clicked successfully on {gameObject.name}. Firing event.");
            OnCardClicked?.Invoke(cardData);
        }

        private IEnumerator FlipAnimation()
        {
            // Use scale-based flip to avoid visible rotation snap
            // Scale to 0 (disappear), change sprite, scale back to 1 (appear)
            // This preserves original scale and doesn't interfere with UI raycast
            
            Vector3 currentScale = transform.localScale;
            
            // First half: scale to 0 (disappear)
            yield return StartCoroutine(ScaleCard(0f, GameConstants.CARD_FLIP_DURATION / 2f, currentScale));

            // Change sprite when card is "invisible"
            cardData.IsFlipped = !cardData.IsFlipped;
            UpdateCardSprite();

            // Second half: scale back to original (appear)
            yield return StartCoroutine(ScaleCard(1f, GameConstants.CARD_FLIP_DURATION / 2f, currentScale));

            isFlipping = false;
            cardData.IsFlipping = false;
        }

        private IEnumerator ScaleCard(float targetScaleFactor, float duration, Vector3 baseScale)
        {
            // Scale factor: 0 = invisible, 1 = full size
            Vector3 startScale = transform.localScale;
            Vector3 endScale = new Vector3(
                baseScale.x * targetScaleFactor, 
                baseScale.y, 
                baseScale.z
            );
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float currentScaleX = Mathf.Lerp(startScale.x, endScale.x, t);
                transform.localScale = new Vector3(currentScaleX, baseScale.y, baseScale.z);
                yield return null;
            }

            transform.localScale = endScale;
        }

        private IEnumerator MatchEffectCoroutine()
        {
            // Simple scale and tint to indicate match (no fade-out)
            float upDuration = 0.15f;
            float downDuration = 0.25f;
            Vector3 baseScale = originalScale != Vector3.zero ? originalScale : transform.localScale;

            // Punch up
            float elapsed = 0f;
            while (elapsed < upDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / upDuration;
                float scale = Mathf.Lerp(1f, 1.12f, t);
                transform.localScale = baseScale * scale;
                yield return null;
            }

            // Settle down and tint
            elapsed = 0f;
            Color startColor = cardImage.color;
            Color endColor = new Color(startColor.r, startColor.g * 0.95f, startColor.b, 0.65f);
            while (elapsed < downDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / downDuration;
                float scale = Mathf.Lerp(1.12f, 0.9f, t);
                transform.localScale = baseScale * scale;
                cardImage.color = Color.Lerp(startColor, endColor, t);
                yield return null;
            }

            transform.localScale = baseScale * 0.9f;
            cardImage.color = endColor;
        }

#if DOTWEEN_AVAILABLE
        private void PlayFlipTween()
        {
            flipTween?.Kill();

            // ensure base scale cached
            Vector3 baseScale = originalScale != Vector3.zero ? originalScale : transform.localScale;

            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DOScaleX(0f, GameConstants.CARD_FLIP_DURATION * 0.5f).SetEase(Ease.InQuad));
            seq.AppendCallback(() =>
            {
                cardData.IsFlipped = !cardData.IsFlipped;
                UpdateCardSprite();
            });
            seq.Append(transform.DOScaleX(baseScale.x, GameConstants.CARD_FLIP_DURATION * 0.5f).SetEase(Ease.OutQuad));
            seq.OnComplete(() =>
            {
                isFlipping = false;
                cardData.IsFlipping = false;
                transform.localScale = baseScale;
            });

            flipTween = seq;
            seq.Play();
        }

        private void PlayMatchTween()
        {
            matchTween?.Kill();

            Vector3 baseScale = originalScale != Vector3.zero ? originalScale : transform.localScale;
            Color startColor = cardImage.color;
            Color endColor = new Color(startColor.r, startColor.g * 0.95f, startColor.b, 0.65f);

            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DOScale(baseScale * 1.12f, 0.16f).SetEase(Ease.OutBack));
            seq.Append(transform.DOScale(baseScale * 0.9f, 0.28f).SetEase(Ease.InOutQuad));
            seq.Join(cardImage.DOColor(endColor, 0.28f));
            // subtle settle
            seq.Append(transform.DOScale(baseScale * 0.92f, 0.12f).SetEase(Ease.OutQuad));
            seq.OnComplete(() =>
            {
                cardImage.color = endColor;
                transform.localScale = baseScale * 0.92f;
            });

            matchTween = seq;
            seq.Play();
        }
#endif

        private void UpdateCardSprite()
        {
            if (cardImage == null) return;

            cardImage.sprite = cardData.IsFlipped ? cardFrontSprite : cardBackSprite;
            
            // Ensure Image has raycast target enabled for button clicks
            if (cardImage.raycastTarget == false)
            {
                cardImage.raycastTarget = true;
            }
        }

        private void ResetRotation()
        {
            // Keep rotation at identity for proper UI raycasting
            // Restore original scale (preserves grid layout scaling)
            transform.rotation = Quaternion.identity;
            if (originalScale != Vector3.zero)
            {
                transform.localScale = originalScale;
            }
        }

        public void OnCardClick()
        {
            OnCardButtonClicked();
        }

        private void OnDestroy()
        {
            if (cardButton != null)
            {
                cardButton.onClick.RemoveListener(OnCardButtonClicked);
            }
        }
    }
}

