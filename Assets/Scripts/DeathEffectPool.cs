using UnityEngine;
using System.Collections.Generic;

public class DeathEffectPool : MonoBehaviour
{
    public static DeathEffectPool Instance;
    
    [Header("Pool Settings")]
    public GameObject deathEffectPrefab;
    public int poolSize = 10;
    
    private Queue<GameObject> objectPool;

    void Awake()
    {
        Instance = this;
        InitializePool();
    }

    void InitializePool()
    {
        objectPool = new Queue<GameObject>();
        
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(deathEffectPrefab);
            obj.SetActive(false);
            objectPool.Enqueue(obj);
        }
    }

    public GameObject GetDeathEffect()
    {
        if (objectPool.Count > 0)
        {
            GameObject obj = objectPool.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        else
        {
            // Expand pool if needed
            GameObject obj = Instantiate(deathEffectPrefab);
            return obj;
        }
    }

    public void ReturnDeathEffect(GameObject obj)
    {
        obj.SetActive(false);
        objectPool.Enqueue(obj);
    }
}