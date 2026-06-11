using System;
using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ListOfScaryMonsters", story: "Check with [Vision] lits of [ScaryFings]", category: "Action", id: "ca714d964b40aec94ac0bcdbfdabe0c0")]
public partial class ListOfScaryMonstersAction : Action
{
    [SerializeReference] public BlackboardVariable<AdaptiveVisionSystem> Vision;
    [SerializeReference] public BlackboardVariable<List<GameObject>> ScaryFings;

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Vision.Value == null || ScaryFings.Value == null)
        {
            return Status.Failure;
        }

        ScaryFings.Value.Clear();

        // There is no 'visibleTargets' property in AdaptiveVisionSystem.
        // You need to decide what "scary" objects you want to collect.
        // Example: collect detectedPlayer, detectedFood, detectedDanger if visible.

        var visionSystem = Vision.Value;

        // Add detected player if visible
        if (visionSystem.isPlayerVisible && visionSystem.detectedPlayer != null)
        {
            ScaryFings.Value.Add(visionSystem.detectedPlayer.gameObject);
        }

        // Add detected danger if visible
        if (visionSystem.isDangerVisible && visionSystem.detectedDanger != null)
        {
            ScaryFings.Value.Add(visionSystem.detectedDanger.gameObject);
        }

        // Add detected food if visible (optional, if food is considered "scary")
        // if (visionSystem.isFoodVisible && visionSystem.detectedFood != null)
        // {
        //     ScaryFings.Value.Add(visionSystem.detectedFood.gameObject);
        // }

        return ScaryFings.Value.Count > 0 ? Status.Success : Status.Failure;
    }

    protected override void OnEnd()
    {
    }
}

