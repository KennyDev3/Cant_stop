using UnityEngine;
using TMPro;

public class FloatingMoneyText : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("How fast it moves upward.")]
    [SerializeField] private float moveSpeed = 3f;
    [Tooltip("Total time before destruction.")]
    [SerializeField] private float lifeTime = 1.2f;

    [Header("Juice / Tweening")]
    [Tooltip("Controls scale over time. Rec: Start at 0, overshoot to 1.2, settle at 1.")]
    [SerializeField]
    private AnimationCurve scaleCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.2f, 1.3f), 
        new Keyframe(0.4f, 1.0f),
        new Keyframe(1f, 0f)      
    );

    [Tooltip("Controls opacity over time.")]
    [SerializeField]
    private AnimationCurve alphaCurve = new AnimationCurve(
        new Keyframe(0f, 1f),
        new Keyframe(0.7f, 1f),
        new Keyframe(1f, 0f) 
    );

    [Header("Orientation (2.5D)")]
    [SerializeField] private Vector3 fixedRotation = new Vector3(68f, 0f, 0f);

    private TextMeshPro _textMesh;
    private Color _startColor;
    private float _timer;
    private Vector3 _randomDir;

    private void Awake()
    {
        _textMesh = GetComponentInChildren<TextMeshPro>();
        if (_textMesh == null) _textMesh = GetComponent<TextMeshPro>();
    }

    public void Initialize(string text)
    {
        if (_textMesh != null)
        {
            
            _textMesh.text = "$" + text;
            _startColor = _textMesh.color;
        }


        float randomX = Random.Range(-0.5f, 0.5f);
        float randomZ = Random.Range(-0.2f, 0.2f); 
        _randomDir = new Vector3(randomX, 1f, randomZ).normalized; 

        transform.rotation = Quaternion.Euler(fixedRotation);

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        float percent = _timer / lifeTime;


        float currentScale = scaleCurve.Evaluate(percent);
        transform.localScale = Vector3.one * currentScale;

        if (_textMesh != null)
        {
            float currentAlpha = alphaCurve.Evaluate(percent);
            _textMesh.color = new Color(_startColor.r, _startColor.g, _startColor.b, currentAlpha);
        }

        transform.position += _randomDir * moveSpeed * Time.deltaTime;
    }
}