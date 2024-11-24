public abstract class BuffEffect {
    public abstract string EffectType { get; }
    public abstract void ApplyEffect(LuxController target, float strength);
    public abstract void RemoveEffect(LuxController target, float strength);
}