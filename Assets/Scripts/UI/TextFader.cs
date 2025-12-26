using UnityEngine;
using TMPro; 
using System.Collections;

public class TextFader : MonoBehaviour
{
    public float delayBeforeFade = 5f;
    public float fadeDuration = 2f;

    private TextMeshPro _textMesh;

    void Awake()
    {
        _textMesh = GetComponent<TextMeshPro>();
    }

    void Start()
    {
        // Start the timer as soon as the object appears
        StartCoroutine(FadeOutSequence());
    }

    IEnumerator FadeOutSequence()
    {
        // 1. Wait for the specified delay
        yield return new WaitForSeconds(delayBeforeFade);

        // 2. Gradually fade the alpha
        float currentTime = 0;
        Color startColor = _textMesh.color;

        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(1f, 0f, currentTime / fadeDuration);
            _textMesh.color = new Color(startColor.r, startColor.g, startColor.b, newAlpha);
            yield return null;
        }

        // 3. Optional: Disable the object once it's invisible
        gameObject.SetActive(false);
    }
}