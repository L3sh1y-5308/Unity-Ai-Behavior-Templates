using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ScaredOf_OP_Enemy", story: "Check with [Vision] Scared [EnemyType]", category: "Action", id: "7154e70295bfd886f622bada2637a793")]
public partial class ScaredOfOpEnemyAction : Action
{
    [SerializeReference] public BlackboardVariable<AdaptiveVisionSystem> Vision;
    [SerializeReference] public BlackboardVariable<string> EnemyType;
    
    protected override Status OnUpdate()
    {
        if (Vision.Value == null || Vision.Value.detectedDanger == null)
        {
            return Status.Failure;
        }

        // We check if the vision system sees a danger AND if that danger matches the tag
        bool isCorrectEnemy = Vision.Value.detectedDanger.CompareTag(EnemyType.Value);
        bool canSee = Vision.Value.CanSeeDanger();

        return (isCorrectEnemy && canSee) ? Status.Success : Status.Failure;
    }
}

