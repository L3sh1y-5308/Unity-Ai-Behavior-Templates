using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "FoodChek", story: "Check [Food] with Raycast [VisionSystem]", category: "Conditions", id: "9526e244d47f28905134ac25f9026ce5")]
public partial class FoodChekCondition : Condition
{
    [SerializeReference] public BlackboardVariable<string> Food;
    [SerializeReference] public BlackboardVariable<AdaptiveVisionSystem> VisionSystem;

    public override bool IsTrue()
    {
        return VisionSystem.Value.detectedFood != null && VisionSystem.Value.CanSeeFood();
    }

}
