using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "IsHungry", story: "Check if [Hunger] is below [Threshold]", category: "Conditions", id: "9797b4c4799ffff33f3464cf6e412f38")]
public partial class IsHungryCondition : Condition
{
    [SerializeReference] public BlackboardVariable<HungerSystem> Hunger;
    [SerializeReference] public BlackboardVariable<int> Threshold;

    public override bool IsTrue()
    {
        if (Hunger.Value == null) return false;
        return Hunger.Value.GetCurrentSatiety() < Threshold.Value;
    }

    public override void OnStart() { }
    public override void OnEnd() { }
}
