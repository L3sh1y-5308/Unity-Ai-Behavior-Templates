using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "RangeDetector", story: "Check [VisionSystem] and detect [Target] in vision zone", category: "Action", id: "4bfd8977efcf191e2f2d07ffddac8eae")]
public partial class RangeDetectorAction : Action
{
    [SerializeReference] public BlackboardVariable<AdaptiveVisionSystem> VisionSystem;
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    protected override Status OnUpdate()
    {
        bool playerDetected = VisionSystem.Value.CanSeePlayer();
        return playerDetected == false ? Status.Failure : Status.Success;
    }
}

