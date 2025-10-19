// GameManager.cs
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int CurrentRotation { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void IncrementRotation()
    {
        CurrentRotation++;
        Debug.Log("Rotation completed: " + CurrentRotation);
        // This is where you will trigger events for stronger enemies, new garbage types, etc.
    }
}