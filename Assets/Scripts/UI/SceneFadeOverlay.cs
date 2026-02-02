using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen black overlay for scene transitions. Create as a child of GameManager (DontDestroyOnLoad)
/// so it persists across scene loads. Builds its Canvas and Image at runtime if not assigned.
/// </summary>
public class SceneFadeOverlay : MonoBehaviour
{
    [Header("Optional: assign if using a prefab. Otherwise built at runtime.")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image overlayImage;

    /// <summary>Use max sort order so overlay draws on top of all other Canvases (e.g. your game UI).</summary>
    private const int CanvasSortOrder = 32767;

    private void Awake()
    {
        if (canvasGroup != null && overlayImage != null)
            return;

        CreateOverlay();
    }

    private void CreateOverlay()
    {

        var canvasGo = new GameObject("SceneFadeCanvas");
        canvasGo.transform.SetParent(transform);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CanvasSortOrder;
        canvas.pixelPerfect = false;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var imageGo = new GameObject("OverlayImage");
        imageGo.transform.SetParent(canvasGo.transform, false);

        overlayImage = imageGo.AddComponent<Image>();
        // Use opaque black; visibility is controlled by CanvasGroup.alpha. (Image alpha=0 would make fade invisible.)
        overlayImage.color = new Color(0f, 0f, 0f, 1f);
        overlayImage.raycastTarget = false;

        var rect = overlayImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        canvasGroup = imageGo.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    /// <summary>Fade to black. Use unscaled time so it runs during pause.</summary>
    public IEnumerator FadeOut(float duration)
    {
        if (canvasGroup == null) yield break;

        canvasGroup.blocksRaycasts = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    /// <summary>Fade from black to clear. Use unscaled time so it runs during pause.</summary>
    public IEnumerator FadeIn(float duration)
    {
        if (canvasGroup == null) yield break;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }
}
