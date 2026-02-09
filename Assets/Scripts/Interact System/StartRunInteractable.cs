using UnityEngine;
using TMPro;

/// <summary>
/// Hub interactable: press E to start a run (loads a level scene, e.g. World_1).
/// Place on a chair (or any object) with an Outline and optional prompt text.
/// Ensure this object (or a child with a collider) is on the Interactable layer so PlayerInteractor can detect it.
/// </summary>
public class StartRunInteractable : MonoBehaviour, IInteractable
{
    [Header("Run")]
    [Tooltip("Scene to load when the player interacts (e.g. World_1). Must be in Build Settings.")]
    [SerializeField] private string sceneName = "World_1";

    [Header("Visual")]
    [Tooltip("Outline to enable/color when the player is in range. Leave empty if no outline.")]
    [SerializeField] private Outline outline;
    [Tooltip("Color when highlighted (in range).")]
    [SerializeField] private Color highlightColor = Color.yellow;
    [Tooltip("Color when not highlighted (optional; if outline stays on).")]
    [SerializeField] private Color normalColor = Color.white;

    [Header("Prompt")]
    [Tooltip("Text shown when in range (e.g. on a small label).")]
    [SerializeField] private string promptText = "Press E to start Run";
    [Tooltip("Optional: world-space or UI TextMeshPro to show/hide and set text when highlighted.")]
    [SerializeField] private TMP_Text promptLabel;

    private void Awake()
    {
        if (outline != null)
        {
            outline.enabled = true;
            outline.OutlineColor = normalColor;
        }

        if (promptLabel != null)
        {
            promptLabel.text = promptText;
            promptLabel.gameObject.SetActive(false);
        }
    }

    public string GetInteractionPrompt() => promptText;

    public void Interact(PlayerInteractor interactor)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[StartRunInteractable] No GameManager. Cannot start run.");
            return;
        }

        GameManager.Instance.StartRun();
        GameManager.Instance.RequestScene(SceneRequest.ToScene(sceneName, false));
    }

    public void Highlight()
    {
        if (outline != null)
        {
           
            outline.OutlineColor = highlightColor;
        }

        if (promptLabel != null)
        {
            promptLabel.text = promptText;
            promptLabel.gameObject.SetActive(true);
        }
    }

    public void Unhighlight()
    {
        if (outline != null)
        {
            outline.OutlineColor = normalColor;
        }

        if (promptLabel != null)
            promptLabel.gameObject.SetActive(false);
    }
}
