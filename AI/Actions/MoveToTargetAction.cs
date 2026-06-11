using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

/// <summary>
/// Движет агента к Target по NavMesh, обновляя каждый кадр (цель может двигаться).
/// Возвращает Success когда дистанция <= StopDistance.
/// Возвращает Failure если агент или таргет null / путь недостижим.
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "MoveToTarget",
    story: "[Agent] moves to [Target] stop at [StopDistance]",
    category: "Action",
    id: "b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7")]
public partial class MoveToTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<NavMeshAgent> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    /// <summary>Дистанция при которой считаем, что "достигли" цели</summary>
    [SerializeReference] public BlackboardVariable<float> StopDistance;

    protected override Status OnStart()
    {
        if (Agent.Value == null || Target.Value == null)
            return Status.Failure;

        Agent.Value.isStopped = false;
        Agent.Value.stoppingDistance = StopDistance.Value;
        Agent.Value.SetDestination(Target.Value.transform.position);

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Agent.Value == null || Target.Value == null)
            return Status.Failure;

        // Обновляем каждый кадр — цель (игрок, добыча) может двигаться
        Agent.Value.SetDestination(Target.Value.transform.position);

        // Путь полностью недостижим — сообщаем об этом BT
        if (!Agent.Value.pathPending &&
            Agent.Value.pathStatus == NavMeshPathStatus.PathInvalid)
            return Status.Failure;

        float dist = Vector3.Distance(
            Agent.Value.transform.position,
            Target.Value.transform.position);

        return dist <= StopDistance.Value ? Status.Success : Status.Running;
    }

    protected override void OnEnd()
    {
        // Сбрасываем путь при выходе из ноды (прерывание / смена стейта)
        if (Agent.Value != null)
        {
            Agent.Value.ResetPath();
            Agent.Value.isStopped = false; // не блокируем агента для следующих нодов
        }
    }
}
