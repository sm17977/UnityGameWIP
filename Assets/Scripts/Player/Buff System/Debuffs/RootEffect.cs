using UnityEngine;

public class RootEffect : BuffEffect {
    
    public override string EffectType => "RootEffect";
    public override void ApplyEffect(LuxController target, float strength) {
        target.canMove.Value = false;
    }
    public override void RemoveEffect(LuxController target, float strength) {
        target.canMove.Value = true;
    }
}