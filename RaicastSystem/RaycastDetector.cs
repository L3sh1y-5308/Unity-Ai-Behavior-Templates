using UnityEngine;

public abstract class RaycastDetector : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField, Min(0.1f)] protected float rayLength = 10f;
    [SerializeField, Range(1, 360)] protected int raysCount = 36;
    [SerializeField] protected LayerMask detectionMask = ~0;

    [Header("Debug")]
    [SerializeField] protected bool drawRays = true;

    protected bool TryDetect(out RaycastHit hitInfo)
    {
        float step = 360f / Mathf.Max(1, raysCount);

        for (int i = 0; i < raysCount; i++)
        {
            float angle = step * i;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * transform.forward;

            if (Physics.Raycast(transform.position, direction, out hitInfo, rayLength, detectionMask))
            {
                if (IsTarget(hitInfo))
                    return true;
            }
        }

        hitInfo = default;
        return false;
    }

    protected abstract bool IsTarget(RaycastHit hitInfo);

    protected virtual void OnDrawGizmosSelected()
    {
        if (!drawRays)
            return;

        Gizmos.color = Color.cyan;

        float step = 360f / Mathf.Max(1, raysCount);
        for (int i = 0; i < raysCount; i++)
        {
            float angle = step * i;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * transform.forward;
            Gizmos.DrawLine(transform.position, transform.position + direction * rayLength);
        }
    }
}