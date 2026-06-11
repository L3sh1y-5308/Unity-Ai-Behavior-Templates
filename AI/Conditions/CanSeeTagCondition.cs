using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "CanSeeTag", story: "Check if [Vision] sees object with [Tag] [FoundTarget]", category: "Conditions", id: "897f538e21cc09a2d64680c382068d20")]
public partial class CanSeeTagCondition : Condition
{
    [SerializeReference] public BlackboardVariable<AdaptiveVisionSystem> Vision;
    [SerializeReference] public BlackboardVariable<string> Tag;
    [SerializeReference] public BlackboardVariable<GameObject> FoundTarget;

    public override bool IsTrue()
    {
        if (Vision.Value == null || string.IsNullOrEmpty(Tag.Value))
        {
            return false;
        }

        Transform found = Vision.Value.FindVisibleByTag(Tag.Value);
        if (found != null)
        {
            if (FoundTarget != null) FoundTarget.Value = found.gameObject;
            return true;
        }

        return false;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
