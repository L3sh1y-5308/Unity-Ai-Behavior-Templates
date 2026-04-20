using UnityEngine;
using UnityEngine.AI; //important

public class RandomMovement : MonoBehaviour 
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform player;


    [Header("Patrol")]
    [SerializeField] private float patrolRad = 10f;
    [SerializeField] private float waitTimeAtPoint = 2f;
    private Vector3 currentPointPatrol;
    private bool isPatrolling;
    private float waitTimer;


    private void Start()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        
        SetNewPatrolPoint();
    }

    private void Update()
    {
        Patrol();
    }

    private void Patrol()
    {
        // Check if agent has reached destination
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!isPatrolling)
            {
                isPatrolling = true;
                waitTimer = 0f;
            }

            waitTimer += Time.deltaTime;

            // Wait at point, then move to new location
            if (waitTimer >= waitTimeAtPoint)
            {
                SetNewPatrolPoint();
                isPatrolling = false;
            }
        }
    }

    private void SetNewPatrolPoint()
    { 
        Vector3 randomPoint = GetRandomPointOnNavMesh();
        
        if (randomPoint != Vector3.zero)
        {
            currentPointPatrol = randomPoint;
            agent.SetDestination(currentPointPatrol);
        }
    }

    private Vector3 GetRandomPointOnNavMesh()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRad;
        randomDirection += transform.position;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRad, NavMesh.AllAreas))
        {
            return hit.position;
        }
        
        return Vector3.zero;
    }
}