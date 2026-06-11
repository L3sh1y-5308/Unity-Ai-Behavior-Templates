using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "FleeTick", story: "[Flee] runs from [Threat] result in [IsCornered]", category: "Action", id: "335e81021ed5b5556133f067c2f6c1ee")]
public partial class FleeTickAction : Action
{
    [SerializeReference] public BlackboardVariable<FleeBehavior> Flee;
    [SerializeReference] public BlackboardVariable<GameObject> Threat;
    [SerializeReference] public BlackboardVariable<bool> IsCornered;

    protected override Status OnStart() => Status.Running;

    protected override Status OnUpdate()
    {
        if (Flee.Value == null || Threat.Value == null) return Status.Failure;

        // Call the tick method and capture its phase result
        int phase = Flee.Value.Tick(Threat.Value.transform);

        // Update the blackboard with the current cornered state
        IsCornered.Value = Flee.Value.IsCornered();

        // Phase 2 indicates the agent is cornered; return Failure to allow tree to switch to attack
        if (phase == 2)
        {
            return Status.Failure;
        }

        // Otherwise continue running (keep fleeing / accumulating panic)
        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (Flee.Value != null) Flee.Value.ResetPanic();
    }
}

