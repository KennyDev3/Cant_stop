using UnityEngine;

/// <summary>
/// Hub house: press E to open the upgrade panel (both Parry and Dash trees).
/// Add to the house object with a collider on the interactable layer and optional Outline.
/// </summary>
public class HubUpgradeHouseInteractable : MonoBehaviour, IInteractable
{
    [Header("UI")]
    [Tooltip("The upgrade panel GameObject to show when the player interacts. The panel controller on it will refresh when enabled (Step 7).")]
    [SerializeField] private GameObject upgradePanel;

    [Header("Prompt")]
    [SerializeField] private string promptText = "Press E to open Upgrades";

    private Outline _outline;

    private void Awake()
    {
        _outline = GetComponent<Outline>();
        if (_outline != null)
        {
            _outline.OutlineColor = Color.white;
            _outline.enabled = true;
        }
    }

    public string GetInteractionPrompt() => promptText;

    public void Interact(PlayerInteractor interactor)
    {
        if (upgradePanel == null)
        {
            Debug.LogWarning("[HubUpgradeHouse] No upgrade panel assigned.");
            return;
        }

        upgradePanel.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Highlight()
    {
        if (_outline != null)
            _outline.OutlineColor = Color.green;
    }

    public void Unhighlight()
    {
        if (_outline != null)
            _outline.OutlineColor = Color.white;
    }
}
