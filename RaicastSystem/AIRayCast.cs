using UnityEngine;
using UnityEngine.AI;

public class AIRayCast : RaycastDetector
{
    [Header("References")]
    [SerializeField] private NavMeshAgent navAgent;

    [Header("Target Settings")]
    [SerializeField] private string[] targetTags = new[] { "apple" };

    private Transform currentTarget;

    private void Awake()
    {
        if (navAgent == null)
        {
            navAgent = GetComponent<NavMeshAgent>();
        }
    }

    private void Update()
    {
        if (TryDetect(out RaycastHit hit))
        {
            currentTarget = hit.transform;
        }

        if (currentTarget != null && navAgent != null)
        {
            navAgent.SetDestination(currentTarget.position);
        }
    }

    protected override bool IsTarget(RaycastHit hitInfo)
    {
        if (hitInfo.collider == null || targetTags == null || targetTags.Length == 0)
            return false;

        foreach (string tag in targetTags)
        {
            if (!string.IsNullOrEmpty(tag) && hitInfo.collider.CompareTag(tag))
                return true;
        }

        return false;
    }
}

