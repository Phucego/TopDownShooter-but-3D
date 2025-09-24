using UnityEngine;

public class PoolableObject : MonoBehaviour
{
    private ObjectPool pool;
    private string poolTag;

    public void SetPool(ObjectPool objectPool, string tag)
    {
        pool = objectPool;
        poolTag = tag;
    }

    public void ReturnToPool()
    {
        if (pool != null && !string.IsNullOrEmpty(poolTag))
        {
            pool.ReturnToPool(poolTag, gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}