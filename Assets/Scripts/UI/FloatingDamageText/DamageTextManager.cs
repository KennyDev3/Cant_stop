using System.Collections.Generic;
using UnityEngine;

public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance { get; private set; }

    [Header("Setup")]
    [SerializeField] private DamageText textPrefab;
    [SerializeField] private int poolSize = 30;
    private Transform _poolContainer;

    private Queue<DamageText> _pool = new Queue<DamageText>();

    private void Awake()
    {
        // Singleton Setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        GameObject container = new GameObject("DamageText_Pool_Container");
        _poolContainer = container.transform;

        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            CreateNewText();
        }
    }

    private DamageText CreateNewText()
    {
        DamageText txt = Instantiate(textPrefab, _poolContainer);
        txt.gameObject.SetActive(false);
        _pool.Enqueue(txt);
        return txt;
    }

    public void ReturnTextToPool(DamageText text)
    {
        text.gameObject.SetActive(false);
        _pool.Enqueue(text);
    }

    public void ShowDamage(float amount, Vector3 position, bool isCrit = false)
    {
        DamageText txt;

        if (_pool.Count > 0)
        {
            txt = _pool.Dequeue();
        }
        else
        {
            
            txt = CreateNewText();
            _pool.Dequeue(); 
        }

        txt.gameObject.SetActive(true);
        txt.Initialize(amount, position, isCrit);
    }
}