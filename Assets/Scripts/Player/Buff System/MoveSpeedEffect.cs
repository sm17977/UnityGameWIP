using UnityEngine;

public class MoveSpeedEffect : BuffEffect {
    
    public override string EffectType => "MoveSpeedEffect";
    public override void ApplyEffect(LuxController target, float strength) {
        Debug.Log("Setting can move to false");
        target.canMove.Value = false;
    }
    public override void RemoveEffect(LuxController target, float strength) {
        Debug.Log("Setting can move to true");
        target.canMove.Value = true;
    }
}