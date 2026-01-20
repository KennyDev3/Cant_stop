using UnityEngine;

// This script sits on the HIPS and talks to the main EnemyHealth
public class CorpseLootLogic : MonoBehaviour, IInteractable
{
    private EnemyHealth _parentHealth;
    private Outline _outline;

    private Color _white = new Color(1f, 1f, 1f);
    private Color _yellow = Color.yellow;

    public void Setup(EnemyHealth parent, Outline outline)
    {
        _parentHealth = parent;
        _outline = outline;

        if (_outline != null)
        {
            _outline.enabled = true;
            _outline.OutlineColor = _white;
            _outline.OutlineWidth = 1.5f;
        }
    }


    public void Highlight()
    {
        if (_outline != null) _outline.OutlineColor = _yellow;
    }

    public void Unhighlight()
    {
        if (_outline != null) _outline.OutlineColor = _white;
    }

    public void Interact(PlayerInteractor interactor)
    {
        // _parentHealth.HandleExternalTrigger(interactor.GetComponent<Collider>());
    }

    public string GetInteractionPrompt() => "Collect Scrap";


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (_parentHealth != null) _parentHealth.HandleExternalTrigger(other);
        }
    }
}