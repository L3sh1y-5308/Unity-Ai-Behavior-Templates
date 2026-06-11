using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "CanSeeLayerCondition", story: "Check if [Vision] sees object on Layer", category: "Conditions", id: "cf0d19d5e8893040dbd6765fb9a901ca")]
public partial class CanSeeLayerCondition : Condition
{
    [SerializeReference] public BlackboardVariable<AdaptiveVisionSystem> Vision;
    [SerializeReference] public BlackboardVariable<LayerMask> Layer;
    [SerializeReference] public BlackboardVariable<GameObject> FoundTarget;

    public override bool IsTrue()
    {
        if (Vision.Value == null)
        {
            return false;
        }

        Transform found = Vision.Value.FindVisibleByLayer(Layer.Value);
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
