using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshPro textMesh;

    [Header("Animation Settings")]
    [SerializeField] private AnimationCurve scaleCurve;
    [SerializeField] private AnimationCurve alphaCurve;
    [SerializeField] private float lifetime = 1.0f;
    [SerializeField] private float floatSpeed = 3.0f;
    [SerializeField] private Vector3 randomOffset = new Vector3(0.5f, 0, 0.5f);

    [Header("Visuals")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color criticalColor = new Color(1f, 0.6f, 0f); // Orange
    [SerializeField] private float critScaleMultiplier = 1.5f;

    private float _timeElapsed = 0f;
    private Vector3 _startPos;
    private Vector3 _floatDirection;
    private bool _isActive = false;
    private Camera _mainCamera;

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    public void Initialize(float damageAmount, Vector3 position, bool isCrit)
    {
        _isActive = true;
        _timeElapsed = 0f;

        // Visual setup
        textMesh.text = Mathf.RoundToInt(damageAmount).ToString();

        if (isCrit)
        {
            textMesh.color = criticalColor;
            textMesh.fontSize = 5; 
            // Add something for Crits
            textMesh.text += "!";
        }
        else
        {
            textMesh.color = normalColor;
            textMesh.fontSize = 3; 
        }

        Vector3 offset = new Vector3(
            Random.Range(-randomOffset.x, randomOffset.x),
            Random.Range(-randomOffset.y, randomOffset.y),
            Random.Range(-randomOffset.z, randomOffset.z)
        );

        transform.position = position + offset;
        _startPos = transform.position;

        _floatDirection = Vector3.up + (Random.insideUnitSphere * 0.2f);
    }

    private void Update()
    {
        if (!_isActive) return;

        transform.rotation = _mainCamera.transform.rotation;

        _timeElapsed += Time.deltaTime;

        if (_timeElapsed < lifetime)
        {
            float percentage = _timeElapsed / lifetime;

            float scale = scaleCurve.Evaluate(percentage);
            transform.localScale = Vector3.one * scale;

            float alpha = alphaCurve.Evaluate(percentage);
            textMesh.alpha = alpha;

            transform.position += _floatDirection * floatSpeed * Time.deltaTime;
        }
        else
        {
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        _isActive = false;
        DamageTextManager.Instance.ReturnTextToPool(this);
    }
}