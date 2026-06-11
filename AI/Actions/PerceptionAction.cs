using System;
using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

/// <summary>
/// Центральный нод восприятия — тикается в Parallel-ветке каждый кадр
/// и выставляет StateMachine по приоритетам:
///
///   1. Опасность видна                → ScaredRun
///   2. Игрок виден (приоритет над едой) → ChasePlayer
///   3. Голоден И еда видна             → SearchFood_Eat
///   4. Голоден                         → Hunger (поиск еды вслепую)
///   5. Иначе                           → Idle_Patrol
///
/// Всегда возвращает Running — не прерывает Parallel.
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "PerceptionTick",
    story: "[Vision] updates [State] hunger [Hunger] thresh [HungerThreshold] scary [ScaryList]",
    category: "Action",
    id: "c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8")]
public partial class PerceptionAction : Action
{
    [SerializeReference] public BlackboardVariable<AdaptiveVisionSystem> Vision;
    [SerializeReference] public BlackboardVariable<StateMachine> State;
    [SerializeReference] public BlackboardVariable<HungerSystem> Hunger;
    [SerializeReference] public BlackboardVariable<int> HungerThreshold;

    /// <summary>Список GameObject-тегов или объектов, которых NPC боится</summary>
    [SerializeReference] public BlackboardVariable<List<GameObject>> ScaryList;

    /// <summary>Выходные таргеты для других нодов</summary>
    [SerializeReference] public BlackboardVariable<GameObject> PlayerTarget;
    [SerializeReference] public BlackboardVariable<GameObject> FoodTarget;
    [SerializeReference] public BlackboardVariable<GameObject> ThreatTarget;

    protected override Status OnStart() => Status.Running;

    protected override Status OnUpdate()
    {
        if (Vision.Value == null) return Status.Running;

        var vis = Vision.Value;

        // --- Приоритет 1: Опасность ---
        if (IsDangerVisible(vis))
        {
            if (ThreatTarget != null && vis.detectedDanger != null)
                ThreatTarget.Value = vis.detectedDanger.gameObject;

            SetState(StateMachine.ScaredRun);
            return Status.Running;
        }

        // --- Приоритет 2: Игрок виден ---
        if (vis.CanSeePlayer() && vis.detectedPlayer != null)
        {
            if (PlayerTarget != null)
                PlayerTarget.Value = vis.detectedPlayer.gameObject;

            // Игрок в приоритете — идём за ним независимо от голода
            SetState(StateMachine.ChasePlayer);
            return Status.Running;
        }

        bool isHungry = Hunger.Value != null &&
                        Hunger.Value.GetCurrentSatiety() < HungerThreshold.Value;

        // --- Приоритет 3: Голоден + еда видна ---
        if (isHungry && vis.CanSeeFood() && vis.detectedFood != null)
        {
            if (FoodTarget != null)
                FoodTarget.Value = vis.detectedFood.gameObject;

            SetState(StateMachine.Hunting);
            return Status.Running;
        }

        // --- Приоритет 4: Голоден, но еды не видит ---
        if (isHungry)
        {
            SetState(StateMachine.Hunger);
            return Status.Running;
        }

        // --- Приоритет 5: Всё спокойно ---
        SetState(StateMachine.Idle);
        return Status.Running;
    }

    // ─────────────────────────────────────────────

    private bool IsDangerVisible(AdaptiveVisionSystem vis)
    {
        // Через встроенный Danger-тег
        if (vis.CanSeeDanger()) return true;

        // Через список ScaryList (если подключён)
        if (ScaryList?.Value != null)
        {
            foreach (var obj in ScaryList.Value)
            {
                if (obj != null && vis.IsTargetVisible(obj.transform))
                    return true;
            }
        }

        return false;
    }

    private void SetState(StateMachine newState)
    {
        // Пишем только при изменении — избегаем лишних обновлений блэкборда
        if (State.Value != newState)
            State.Value = newState;
    }
}
