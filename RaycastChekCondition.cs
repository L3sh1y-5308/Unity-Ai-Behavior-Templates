using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Raycast Chek ", story: "Check [Target] with Raycast [detect]", category: "Conditions", id: "715f46f8f7959105de9146fd932fd286")]
public partial class RaycastChekCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<AdaptiveVisionSystem> Detect;

    public override bool IsTrue()
    {
        return Detect.Value.GetDetectedPlayer() != false && Detect.Value.CanSeePlayer() != false;
    }
}

