using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class ItemPopupUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;  
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;

    [Header("Settings")]
    [SerializeField] private float visibleDuration = 5f;
    [SerializeField] private float fadeDuration = 1f;

    public void Initialize(string itemName, string itemDescription)
    {
        itemNameText.text = itemName;
        itemDescriptionText.text = itemDescription;

        StartCoroutine(PopupRoutine());
    }

    private System.Collections.IEnumerator PopupRoutine()
    {
        // Fully visible
        canvasGroup.alpha = 1f;

        // Wait while fully visible
        yield return new WaitForSeconds(visibleDuration);

        // Fade Out
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }

        Destroy(gameObject);
    }
}
