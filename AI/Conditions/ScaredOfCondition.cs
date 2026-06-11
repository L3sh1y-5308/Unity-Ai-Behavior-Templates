using System;
using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "ScaredOf", story: "Check [List] with [Vision]", category: "Conditions", id: "4dc5c418926a045c94c28649c1e71dd1")]
public partial class ScaredOfCondition : Condition
{
    [SerializeReference] public BlackboardVariable<List<GameObject>> List;
    [SerializeReference] public BlackboardVariable<AdaptiveVisionSystem> Vision;

    public override bool IsTrue()
    {
        if (List.Value == null || Vision.Value == null || List.Value.Count == 0)
        {
            return false;
        }

        foreach (var target in List.Value)
        {
            if (target != null && Vision.Value.IsTargetVisible(target.transform))
            {
                return true;
            }
        }

        return false;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
