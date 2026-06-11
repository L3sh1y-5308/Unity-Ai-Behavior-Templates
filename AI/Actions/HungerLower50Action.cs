using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "HungerLower50", story: "[HungerSystem] lower then [Norm]", category: "Action", id: "3a7b92857d4dd018008db3c3bd99b6a9")]
public partial class HungerLower50Action : Action
{
    [SerializeReference] public BlackboardVariable<HungerSystem> HungerSystem;
    [SerializeReference] public BlackboardVariable<int> Norm;

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (HungerSystem.Value != null && HungerSystem.Value.GetCurrentSatiety() < Norm.Value)
        {
            return Status.Success;
        }

        return Status.Failure;
    }

    protected override void OnEnd()
    {
    }
}

