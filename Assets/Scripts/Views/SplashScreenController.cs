using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if DOTWEEN_AVAILABLE
using DG.Tweening;
#endif

namespace CardMatch.Views
{
    /// <summary>
    /// Builds and plays a simple branded splash screen at runtime.
    /// Shows a mango image with the game name, then reveals the main menu.
    /// </summary>
    public class SplashScreenController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameUIView gameUIView;
        [SerializeField] private Sprite mangoSprite; // Loaded from Resources if not assigned

        [Header("Text")]
        [SerializeField] private string titleText = "Nano Mango";
        [SerializeField] private Color titleColor = Color.white;
        [SerializeField] private int titleFontSize = 88;

        [Header("Timing")]
        [SerializeField] private float fadeInDuration = 0.55f;
        [SerializeField] private float holdDuration = 1.8f;
        [SerializeField] private float fadeOutDuration = 0.55f;

        [Header("Layout")]
        [SerializeField] private Color backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
        [SerializeField] private Color backgroundAltColor = new Color(0.08f, 0.12f, 0.18f, 1f);
        [SerializeField] private Color backgroundGlowColor = new Color(0.15f, 0.12f, 0.2f, 0.5f);
        [SerializeField] private float backgroundPulseDuration = 2.2f;
        [SerializeField] private Vector2 mangoSize = new Vector2(460f, 460f);
        [SerializeField] private float mangoStartScale = 0.4f;
        [Header("Bubbles / Sparks")]
        [SerializeField] private int bubbleCount = 8;
        [SerializeField] private Color bubbleColor = new Color(1f, 1f, 1f, 0.12f);
        [SerializeField] private Vector2 bubbleSizeRange = new Vector2(60f, 120f);
        [SerializeField] private Vector2 bubbleDriftRange = new Vector2(80f, 140f);
        [SerializeField] private float bubbleFloatDuration = 2.8f;

        private Canvas splashCanvas;
        private CanvasGroup canvasGroup;
        private Image backgroundImage;
        private Image mangoImage;
        private Image glowImage;
        private Image highlightImage;
        private Image flashOverlay;
        private TMP_Text titleLabel;
        private bool hasPlayed;
        private readonly List<Image> bubbleImages = new List<Image>();

        /// <summary>
        /// Creates (if needed) and starts the splash screen.
        /// </summary>
        public static SplashScreenController Create(GameUIView ui)
        {
            var existing = FindObjectOfType<SplashScreenController>();
            if (existing != null)
            {
                return existing;
            }

            GameObject go = new GameObject("SplashScreen");
            var controller = go.AddComponent<SplashScreenController>();
            controller.gameUIView = ui;
            return controller;
        }

        private void Awake()
        {
            if (gameUIView == null)
            {
                gameUIView = FindObjectOfType<GameUIView>();
            }

            if (mangoSprite == null)
            {
                // Auto-load mango sprite from Resources (moved to Resources folder)
                mangoSprite = Resources.Load<Sprite>("Sprites/UI/Decorations/mango");
            }

            BuildUI();
        }

        private void Start()
        {
            if (hasPlayed) return;
            hasPlayed = true;
            // Hide menus until splash completes
            gameUIView?.ShowMenuPanel(false);
            gameUIView?.ShowGamePanel(false);

            StartCoroutine(PlaySequence());
        }

        private void BuildUI()
        {
            // Root canvas
            GameObject canvasGO = new GameObject("SplashCanvas");
            canvasGO.transform.SetParent(transform, false);

            splashCanvas = canvasGO.AddComponent<Canvas>();
            splashCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            splashCanvas.sortingOrder = 5000; // Ensure on top

            canvasGroup = canvasGO.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = false; // blocks clicks but doesn't need interaction

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // Flash overlay (for punchy exit)
            GameObject flashGO = new GameObject("FlashOverlay");
            flashGO.transform.SetParent(canvasGO.transform, false);
            flashOverlay = flashGO.AddComponent<Image>();
            flashOverlay.color = Color.white.WithAlpha(0f);
            var flashRect = flashOverlay.rectTransform;
            flashRect.anchorMin = Vector2.zero;
            flashRect.anchorMax = Vector2.one;
            flashRect.offsetMin = Vector2.zero;
            flashRect.offsetMax = Vector2.zero;

            // Background
            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform, false);
            backgroundImage = bgGO.AddComponent<Image>();
            backgroundImage.color = backgroundColor.WithAlpha(0f);
            var bgRect = backgroundImage.rectTransform;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Soft glow layer
            GameObject glowGO = new GameObject("Glow");
            glowGO.transform.SetParent(canvasGO.transform, false);
            glowImage = glowGO.AddComponent<Image>();
            glowImage.color = backgroundGlowColor.WithAlpha(0f);
            var glowRect = glowImage.rectTransform;
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = new Vector2(-120f, -120f);
            glowRect.offsetMax = new Vector2(120f, 120f);

            // Highlight sweep overlay
            GameObject highlightGO = new GameObject("Highlight");
            highlightGO.transform.SetParent(canvasGO.transform, false);
            highlightImage = highlightGO.AddComponent<Image>();
            highlightImage.color = Color.white.WithAlpha(0f);
            var hlRect = highlightImage.rectTransform;
            hlRect.anchorMin = new Vector2(0.2f, -0.1f);
            hlRect.anchorMax = new Vector2(0.8f, 0.6f);
            hlRect.offsetMin = Vector2.zero;
            hlRect.offsetMax = Vector2.zero;
            hlRect.rotation = Quaternion.Euler(0f, 0f, -20f);

            // Mango image
            GameObject mangoGO = new GameObject("Mango");
            mangoGO.transform.SetParent(canvasGO.transform, false);
            mangoImage = mangoGO.AddComponent<Image>();
            mangoImage.color = Color.white.WithAlpha(0f);
            mangoImage.sprite = mangoSprite;
            var mangoRect = mangoImage.rectTransform;
            mangoRect.sizeDelta = mangoSize;
            mangoRect.anchorMin = new Vector2(0.5f, 0.55f);
            mangoRect.anchorMax = new Vector2(0.5f, 0.55f);
            mangoRect.anchoredPosition = Vector2.zero;
            mangoRect.localScale = Vector3.one * mangoStartScale;

            // Title text
            GameObject titleGO = new GameObject("Title");
            titleGO.transform.SetParent(canvasGO.transform, false);
            titleLabel = titleGO.AddComponent<TextMeshProUGUI>();
            titleLabel.text = titleText;
            titleLabel.fontSize = titleFontSize;
            titleLabel.alignment = TextAlignmentOptions.Center;
            titleLabel.color = titleColor.WithAlpha(0f);
            titleLabel.enableWordWrapping = false;
            titleLabel.font = TMP_Settings.defaultFontAsset ?? Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

            var titleRect = titleLabel.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 0.28f);
            titleRect.anchorMax = new Vector2(0.5f, 0.28f);
            titleRect.anchoredPosition = new Vector2(0f, -40f);
            titleRect.sizeDelta = new Vector2(900f, 200f);

            // Floating bubbles / sparkles
            GameObject bubblesRoot = new GameObject("Bubbles");
            bubblesRoot.transform.SetParent(canvasGO.transform, false);
            var bubblesRect = bubblesRoot.AddComponent<RectTransform>();
            bubblesRect.anchorMin = Vector2.zero;
            bubblesRect.anchorMax = Vector2.one;
            bubblesRect.offsetMin = Vector2.zero;
            bubblesRect.offsetMax = Vector2.zero;

            for (int i = 0; i < bubbleCount; i++)
            {
                GameObject b = new GameObject($"Bubble_{i}");
                b.transform.SetParent(bubblesRoot.transform, false);
                var img = b.AddComponent<Image>();
                img.color = bubbleColor.WithAlpha(0f);
                float size = Random.Range(bubbleSizeRange.x, bubbleSizeRange.y);
                var r = img.rectTransform;
                r.sizeDelta = new Vector2(size, size);
                r.anchorMin = r.anchorMax = new Vector2(Random.Range(0.2f, 0.8f), Random.Range(0.3f, 0.8f));
                r.anchoredPosition = Vector2.zero;
                bubbleImages.Add(img);
            }
        }

        private IEnumerator PlaySequence()
        {
            // Small delay to ensure UI is built
            yield return null;

#if DOTWEEN_AVAILABLE
            Sequence seq = DOTween.Sequence();

            // Prep starting positions for text (so we don't rely on .From for this tween)
            var titleRect = titleLabel.rectTransform;
            float titleStartY = -110f;
            float titleEndY = 24f;
            titleRect.anchoredPosition = new Vector2(titleRect.anchoredPosition.x, titleStartY);

            // Start background gentle pulse
            backgroundImage.DOColor(backgroundAltColor, backgroundPulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
            glowImage.DOColor(backgroundGlowColor * 1.15f, backgroundPulseDuration * 1.1f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            // Start bubble float animations
            foreach (var bubble in bubbleImages)
            {
                float startDelay = Random.Range(0f, 0.6f);
                float driftX = Random.Range(-bubbleDriftRange.x, bubbleDriftRange.x);
                float driftY = Random.Range(bubbleDriftRange.x * 0.5f, bubbleDriftRange.y);
                float duration = bubbleFloatDuration + Random.Range(-0.4f, 0.4f);
                bubble.rectTransform.anchoredPosition = new Vector2(0f, -30f);
                bubble.color = bubbleColor.WithAlpha(0f);

                Sequence bSeq = DOTween.Sequence();
                bSeq.AppendInterval(startDelay);
                bSeq.Append(bubble.DOFade(bubbleColor.a, 0.35f));
                bSeq.Join(bubble.rectTransform.DOAnchorPos(new Vector2(driftX, driftY), duration).SetEase(Ease.OutQuad));
                bSeq.Join(bubble.rectTransform.DOScale(1.1f, duration * 0.6f).From(0.7f));
                bSeq.Append(bubble.DOFade(0f, 0.35f));
                bSeq.SetLoops(-1, LoopType.Restart);
            }

            // Fade in background and bring in mango/text
            seq.Append(backgroundImage.DOFade(backgroundColor.a, fadeInDuration));
            seq.Join(glowImage.DOFade(backgroundGlowColor.a, fadeInDuration * 0.9f));
            seq.Join(mangoImage.DOFade(1f, fadeInDuration * 0.85f));
            seq.Join(mangoImage.rectTransform.DOScale(1.05f, fadeInDuration + 0.2f).From(mangoStartScale).SetEase(Ease.OutBack));
            seq.Join(mangoImage.rectTransform.DOLocalRotate(new Vector3(0f, 0f, 6f), fadeInDuration + 0.25f, RotateMode.FastBeyond360).From(new Vector3(0f, 0f, -25f)).SetEase(Ease.OutBack));
            seq.Join(titleLabel.DOFade(1f, fadeInDuration * 0.9f));
            seq.Join(titleRect
                .DOAnchorPosY(titleEndY, fadeInDuration + 0.2f)
                .SetEase(Ease.OutCubic));
            seq.Join(titleRect.DOScale(1.05f, fadeInDuration * 0.9f).From(0.85f).SetEase(Ease.OutBack));
            seq.Join(titleLabel.DOColor(titleColor, fadeInDuration));

            // Highlight sweep
            seq.Insert(fadeInDuration * 0.3f, highlightImage.DOFade(0.35f, 0.18f));
            seq.Insert(fadeInDuration * 0.3f, highlightImage.rectTransform.DOAnchorPos(new Vector2(500f, 500f), 0.5f).From(new Vector2(-500f, -300f)).SetEase(Ease.OutCubic));
            seq.Insert(fadeInDuration * 0.55f, highlightImage.DOFade(0f, 0.25f));

            // Gentle hover during hold
            seq.AppendInterval(holdDuration * 0.35f);
            seq.Join(mangoImage.rectTransform.DOLocalRotate(new Vector3(0f, 0f, -8f), 0.5f).SetEase(Ease.InOutSine).SetLoops(2, LoopType.Yoyo));
            seq.Join(mangoImage.rectTransform.DOPunchScale(new Vector3(0.06f, 0.06f, 0f), 0.6f, 1, 0.2f));
            seq.AppendInterval(holdDuration * 0.65f);

            // Fade out
            seq.Append(backgroundImage.DOFade(0f, fadeOutDuration));
            seq.Join(glowImage.DOFade(0f, fadeOutDuration * 0.8f));
            seq.Join(mangoImage.DOFade(0f, fadeOutDuration));
            seq.Join(mangoImage.rectTransform.DOScale(1.2f, fadeOutDuration));
            seq.Join(mangoImage.rectTransform.DOLocalRotate(new Vector3(0f, 0f, 32f), fadeOutDuration));
            seq.Join(titleLabel.DOFade(0f, fadeOutDuration));
            seq.Join(titleRect.DOAnchorPosY(titleEndY + 40f, fadeOutDuration).SetEase(Ease.InCubic));
            // Flash + highlight sweep on exit
            seq.Insert(fadeOutDuration * 0.15f, flashOverlay.DOFade(0.35f, 0.12f).From(0f));
            seq.Insert(fadeOutDuration * 0.35f, flashOverlay.DOFade(0f, 0.22f));
            seq.Insert(fadeOutDuration * 0.2f, highlightImage.DOFade(0.25f, 0.18f));
            seq.Insert(fadeOutDuration * 0.2f, highlightImage.rectTransform.DOAnchorPos(new Vector2(400f, 420f), 0.35f).From(new Vector2(-400f, -260f)).SetEase(Ease.OutCubic));
            seq.Insert(fadeOutDuration * 0.45f, highlightImage.DOFade(0f, 0.2f));
            seq.OnComplete(OnSplashFinished);
            seq.Play();
#else
            // Fallback: simple wait then finish
            yield return new WaitForSeconds(fadeInDuration + holdDuration + fadeOutDuration);
            OnSplashFinished();
#endif
        }

        private void OnSplashFinished()
        {
            gameUIView?.ShowMenuPanel(true);
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
            }
            Destroy(gameObject);
        }
    }

    internal static class ColorExtensions
    {
        public static Color WithAlpha(this Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}

