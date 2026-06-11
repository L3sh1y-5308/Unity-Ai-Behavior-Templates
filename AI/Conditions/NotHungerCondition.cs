using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "NotHunger", story: "[HungerSystem] is [Hunger] full", category: "Conditions", id: "e009a950c50dc6c17b11663b45f88e09")]
public partial class NotHungerCondition : Condition
{
    [SerializeReference] public BlackboardVariable<HungerSystem> HungerSystem;
    [SerializeReference] public BlackboardVariable<int> Hunger;

    public override bool IsTrue()
    {
        if (HungerSystem == null || HungerSystem.Value == null)
        {
            return false;
        }

        // Use the correct property or method to get current satiety from HungerSystem
        return HungerSystem.Value.GetCurrentSatiety() >= Hunger.Value;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
