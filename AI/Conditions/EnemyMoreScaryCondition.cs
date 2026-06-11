using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "EnemyMoreScary", story: "Chek [ScaryMonster] with [Vision]", category: "Conditions", id: "eb6feafa31348057ea661d585ab24923")]
public partial class EnemyMoreScaryCondition : Condition
{
    [SerializeReference] public BlackboardVariable<string> ScaryMonster;
    [SerializeReference] public BlackboardVariable<AdaptiveVisionSystem> Vision;

    public override bool IsTrue()
    {
        return Vision.Value.detectedDanger != null && Vision.Value.CanSeeDanger();
    }
}
