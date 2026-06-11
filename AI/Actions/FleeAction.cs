using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "FleeAction", story: "[Self] flees from [EnemyReunOf] using [NavMeshAgent]", category: "Action", id: "d0827144ce67c3e0619f56e7fe15f93f")]
public partial class FleeAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> EnemyReunOf;
    [SerializeReference] public BlackboardVariable<NavMeshAgent> NavMeshAgent;
    [SerializeReference] public BlackboardVariable<float> FleeDistance = new BlackboardVariable<float>(5f);
    [SerializeReference] public BlackboardVariable<float> SafeDistance = new BlackboardVariable<float>(15f);

    protected override Status OnStart()
    {
        if (Self.Value == null || NavMeshAgent.Value == null || EnemyReunOf.Value == null) 
            return Status.Failure;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Self.Value == null || NavMeshAgent.Value == null || EnemyReunOf.Value == null) 
            return Status.Failure;

        GameObject enemy = EnemyReunOf.Value;

        float distanceToEnemy = Vector3.Distance(Self.Value.transform.position, enemy.transform.position);

        if (distanceToEnemy >= SafeDistance.Value)
            return Status.Success;

        // Search for safe points
        GameObject[] safePoints = GameObject.FindGameObjectsWithTag("SafePoint");
        GameObject bestPoint = null;
        float closestDistance = Mathf.Infinity;

        foreach (GameObject point in safePoints)
        {
            float distToEnemy = Vector3.Distance(point.transform.position, enemy.transform.position);
            float distToMe = Vector3.Distance(Self.Value.transform.position, point.transform.position);

            if (distToEnemy > distanceToEnemy)
            {
                if (distToMe < closestDistance)
                {
                    closestDistance = distToMe;
                    bestPoint = point;
                }
            }
        }

        // Navigate to best safe point or use flee direction
        Vector3 targetPoint;
        if (bestPoint != null)
        {
            targetPoint = bestPoint.transform.position;
        }
        else
        {
            Vector3 fleeDirection = Self.Value.transform.position - enemy.transform.position;
            targetPoint = Self.Value.transform.position + fleeDirection.normalized * FleeDistance.Value;
        }

        if (NavMeshAgent.Value.isOnNavMesh)
            NavMeshAgent.Value.SetDestination(targetPoint);

        return Status.Running;
    }
}

