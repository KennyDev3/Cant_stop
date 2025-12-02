using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private TextMeshProUGUI itemDisplayName;

    public void Initialize(ItemSO item, int count)
    {
        if (iconImage != null)
        {
            iconImage.sprite = item.icon;
        }

        if (itemDisplayName != null)
        {
            itemDisplayName.text = item.displayName;

        }

        UpdateCount(count);

    }

    public void UpdateCount(int count)
    {
        if (countText != null && count > 0)
        {
            countText.text = $"x{count}";
        }
    }
}