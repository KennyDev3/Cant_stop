using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Always-on hub HUD panel that shows the player's resource bank.
/// Attach this to a UI GameObject under the Hub canvas and wire up the slots in the Inspector.
/// </summary>
public class HubResourceHud : MonoBehaviour
{
    [Tooltip("One entry per resource type. Each text shows the bank count for that resource.")]
    [SerializeField] private List<ResourceSlot> slots = new List<ResourceSlot>();

    [System.Serializable]
    public struct ResourceSlot
    {
        public ResourceSO resourceType;
        public TextMeshProUGUI countText;
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnHubBankChanged += HandleBankChanged;
            GameManager.Instance.OnSceneReady += HandleSceneReady;
        }

        RefreshDisplay();
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnHubBankChanged -= HandleBankChanged;
            GameManager.Instance.OnSceneReady -= HandleSceneReady;
        }
    }

    private void HandleSceneReady()
    {
        RefreshDisplay();
    }

    private void HandleBankChanged()
    {
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (GameManager.Instance == null || slots == null) return;

        foreach (var slot in slots)
        {
            if (slot.resourceType == null || slot.countText == null) continue;

            int count = GameManager.Instance.GetHubBankCount(slot.resourceType);
            string label = string.IsNullOrEmpty(slot.resourceType.displayName)
                ? slot.resourceType.name
                : slot.resourceType.displayName;

            slot.countText.text = $"{label}: {count}";
        }
    }
}

