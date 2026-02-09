using UnityEngine;
using TMPro; // Standard TMP namespace

public class DisposalUI : MonoBehaviour
{
    // Note: Serialized as TextMeshPro (for 3D objects), not TextMeshProUGUI
    [SerializeField] private TextMeshPro infoText;

    public void UpdateDisplay(int currentCost, int maxCost, int attemptsLeft, bool success)
    {
        if (infoText == null) return;

        // Line 1: Capacity progress toward unlock
        string line1 = $"Capacity: {currentCost}/{maxCost}";

        // Line 2: Bullseye status
        string line2;
        if (success)
            line2 = "Bullseye: <color=green>SUCCESS</color>";
        else if (attemptsLeft <= 0)
            line2 = "Bullseye: <color=red>FAILED</color>";
        else
            line2 = $"Bullseye Attempts: {attemptsLeft}";

        // Standard line break works for 3D TextMeshPro
        infoText.text = line1 + "\n" + line2;
    }

    public void Hide() => gameObject.SetActive(false);
}