using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; //target is the player

    public float smoothTime;

    public Vector3 offset;

    public Vector3 velocity = Vector3.zero;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (target != null)
        {
            Vector3 targetPos = target.position + offset;

            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
        }
    }
}
