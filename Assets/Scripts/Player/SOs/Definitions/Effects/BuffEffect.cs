using UnityEngine;

public abstract class BuffEffect : ScriptableObject {
    public abstract void ApplyEffect(LuxController target, float strength);
    public abstract void RemoveEffect(LuxController target, float strength);
}