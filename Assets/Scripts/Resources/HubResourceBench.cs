using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Hub-world bench that shows the player's resource bank when they approach.
/// Assign a trigger collider (e.g. BoxCollider isTrigger) and the same layer/collision so the player triggers it.
/// Panel is shown on enter, hidden on exit; bank counts are read from GameManager.
/// </summary>
public class HubResourceBench : MonoBehaviour
{
    [Header("Display")]
    [Tooltip("Panel to show when player is in range. Hidden when they leave.")]
    [SerializeField] private GameObject panel;

    [Tooltip("One entry per resource type. Each text shows the bank count for that resource.")]
    [SerializeField] private List<ResourceBankSlot> bankSlots = new List<ResourceBankSlot>();

    [System.Serializable]
    public struct ResourceBankSlot
    {
        public ResourceSO resourceType;
        public TextMeshProUGUI countText;
    }

    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (panel != null)
            panel.SetActive(true);
        RefreshDisplay();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (panel != null)
            panel.SetActive(false);
    }

    private void RefreshDisplay()
    {
        if (GameManager.Instance == null || bankSlots == null) return;

        foreach (var slot in bankSlots)
        {
            if (slot.resourceType == null || slot.countText == null) continue;
            int count = GameManager.Instance.GetHubBankCount(slot.resourceType);
            string label = string.IsNullOrEmpty(slot.resourceType.displayName) ? slot.resourceType.name : slot.resourceType.displayName;
            slot.countText.text = $"{label}: {count}";
        }
    }
}
