using UnityEngine;
using TMPro;
using System.Collections;

public class PopupText : MonoBehaviour
{
    [SerializeField] private TextMeshPro _textMesh;
    [SerializeField] private float moveSpeed = 0.8f;
    [SerializeField] private float fadeDuration = 0.8f;
    [SerializeField] private float stayDuration = 0.4f;

    private Vector3 _fixedRotation = new Vector3(70f, 0f, 0f);
    private Vector3 _targetScale = new Vector3(0.15f, 0.15f, 0.15f);
    private Vector3 _punchScale = new Vector3(0.2f, 0.2f, 0.2f); // Slightly larger for the pop

    public void Setup(int amount)
    {
        transform.rotation = Quaternion.Euler(_fixedRotation);
        transform.localScale = Vector3.zero;

        // Using a TMP Sprite tag instead of a raw emoji
        // This assumes you have a sprite asset set up. 
        // If you don't have one, this will just show a blank space or 'X'
        _textMesh.text = $"+{amount} <sprite=0>";

        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        float elapsed = 0;
        float popDuration = 0.15f;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(Vector3.zero, _punchScale, elapsed / popDuration);
            transform.rotation = Quaternion.Euler(_fixedRotation);
            yield return null;
        }
        transform.localScale = _targetScale;

        yield return new WaitForSeconds(stayDuration);

        float fadeElapsed = 0;
        Color startColor = _textMesh.color;

        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.deltaTime;
            float t = fadeElapsed / fadeDuration;

            transform.position += Vector3.up * moveSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Euler(_fixedRotation);
            transform.localScale = _targetScale;

            _textMesh.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
            yield return null;
        }

        Destroy(gameObject);
    }
}