using UnityEngine;
using UnityEngine.SceneManagement; // Needed to load scenes
using StarterAssets; // Needed for IInteractable and PlayerInteractor

public class TutorialFinishButton : MonoBehaviour, IInteractable
{
    [Header("Scene Settings")]
    [Tooltip("The name of the scene to load when pressed.")]
    [SerializeField] private string sceneToLoad = "GameScene";

    [Header("Animation Settings")]
    [Tooltip("How far below ground the button starts.")]
    [SerializeField] private float riseDistance = 2.0f;
    [Tooltip("How fast it rises.")]
    [SerializeField] private float riseSpeed = 0.7f;

    [Header("Visuals")]
    [SerializeField] private Outline targetOutline;
    [SerializeField] private Collider interactionCollider;

    private Vector3 _targetPosition;
    private bool _isRising = false;
    private bool _isInteractable = false;

    private void Awake()
    {
        gameObject.SetActive(false);

        _targetPosition = transform.position; 

        transform.position = new Vector3(transform.position.x, transform.position.y - riseDistance, transform.position.z);

        if (targetOutline) targetOutline.enabled = false;
        if (interactionCollider) interactionCollider.enabled = false;
    }

    private void Update()
    {
        if (_isRising)
        {
            transform.position = Vector3.MoveTowards(transform.position, _targetPosition, riseSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, _targetPosition) < 0.01f)
            {
                _isRising = false;
                transform.position = _targetPosition;
            }
        }
    }

    public void ActivateButton()
    {
        gameObject.SetActive(true);
        _isRising = true;
        _isInteractable = true;

        if (interactionCollider) interactionCollider.enabled = true;
    }


    public void Interact(PlayerInteractor interactor)
    {
        if (!_isInteractable) return;

        Debug.Log($"Loading Scene: {sceneToLoad}");

        SceneManager.LoadScene(sceneToLoad);
    }

    public void Highlight()
    {
        if (!_isInteractable) return;

        if (targetOutline != null)
        {
            targetOutline.enabled = true;
            targetOutline.OutlineColor = Color.green; 
        }
    }

    public void Unhighlight()
    {
        if (targetOutline != null) targetOutline.enabled = false;
    }

    public string GetInteractionPrompt()
    {
        return "Start Game";
    }
}