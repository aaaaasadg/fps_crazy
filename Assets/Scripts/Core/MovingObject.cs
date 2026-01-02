using UnityEngine;

/// <summary>
/// Simple script that moves an object on a specified axis at a set speed,
/// then teleports it back to its initial position after a set time.
/// </summary>
public class MovingObject : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Axis moveAxis = Axis.X;
    [SerializeField] private float maxDistance = 100f;
    [SerializeField] private float teleportTime = 10f;

    private Vector3 initialPosition;
    private float timer;
    private bool hasReachedMaxDistance;
    private Transform cachedTransform;

    private enum Axis
    {
        X,
        Y,
        Z
    }

    private void Awake()
    {
        cachedTransform = transform;
        initialPosition = cachedTransform.position;
        timer = 0f;
        hasReachedMaxDistance = false;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        // Calculate distance traveled on the movement axis
        float currentDistance = GetDistanceOnAxis();

        // Move only if distance limit not reached
        if (currentDistance < maxDistance && !hasReachedMaxDistance)
        {
            Vector3 position = cachedTransform.position;
            float movement = moveSpeed * Time.deltaTime;
            
            switch (moveAxis)
            {
                case Axis.X:
                    position.x += movement;
                    break;
                case Axis.Y:
                    position.y += movement;
                    break;
                case Axis.Z:
                    position.z += movement;
                    break;
            }
            
            cachedTransform.position = position;
            
            // Check if we've reached max distance after movement
            float newDistance = GetDistanceOnAxis();
            if (newDistance >= maxDistance)
            {
                hasReachedMaxDistance = true;
            }
        }

        // Teleport back after time elapsed
        if (timer >= teleportTime)
        {
            cachedTransform.position = initialPosition;
            timer = 0f;
            hasReachedMaxDistance = false;
        }
    }

    private float GetDistanceOnAxis()
    {
        switch (moveAxis)
        {
            case Axis.X:
                return Mathf.Abs(cachedTransform.position.x - initialPosition.x);
            case Axis.Y:
                return Mathf.Abs(cachedTransform.position.y - initialPosition.y);
            case Axis.Z:
                return Mathf.Abs(cachedTransform.position.z - initialPosition.z);
            default:
                return 0f;
        }
    }
}

