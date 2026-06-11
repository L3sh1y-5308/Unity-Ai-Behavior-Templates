using System;
using Unity.Behavior;
using UnityEngine; // ← это было пропущено
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "SetState",
    story: "Set [State] to [NewState]",
    category: "Action",
    id: "a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6")]
public partial class SetStateAction : Action
{
    [SerializeReference] public BlackboardVariable<StateMachine> State;
    [SerializeReference] public BlackboardVariable<StateMachine> NewState;

    protected override Status OnStart()
    {
        State.Value = NewState.Value;
        return Status.Success;
    }
}