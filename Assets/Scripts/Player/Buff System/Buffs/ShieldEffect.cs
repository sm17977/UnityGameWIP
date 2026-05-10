public class ShieldEffect : BuffEffect {
    public override string EffectType => "ShieldEffect";
    public override void ApplyEffect(LuxController target, float strength) {
        target.health.Heal(strength);
    }

    public override void RemoveEffect(LuxController target, float strength) {
        target.health.TakeDamage(strength);
    }
}
