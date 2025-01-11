using UnityEngine;

public class SlowEffect : BuffEffect {
    
    public override string EffectType => "SlowEffect";
    public override void ApplyEffect(LuxController target, float strength) {
        target.movementSpeed.Value -= strength;
    }
    public override void RemoveEffect(LuxController target, float strength) {
        target.movementSpeed.Value += strength;
    }
}