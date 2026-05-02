using UnityEngine;
using UnityEngine.AI;

public class AITargetDetector : RayCastCont
{
    [Header("AI References")]
    [SerializeField] private NavMeshAgent navAgent;

    [Header("Target Settings")]
    [SerializeField] private string[] targetTags = new[] { "Player" };

    private Transform currentTarget;

    private void Awake()
    {
        if (navAgent == null)
            navAgent = GetComponent<NavMeshAgent>();
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

    public override bool IsTarget(RaycastHit hitInfo)
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