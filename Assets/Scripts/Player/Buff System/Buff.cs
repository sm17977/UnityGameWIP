using System;
using Unity.Netcode;

public class Buff{
    
    public BuffEffect Effect;
    public string Key;
    public float Duration;
    public float EffectStrength;
    public float CurrentTimer;
    public string ID;
    private const float RPCLatency = 0.15f;
    
    public Buff(BuffEffect effect, string key, float duration, float effectStrength) {
        Effect = effect;
        Key = key;
        Duration = duration - RPCLatency;
        EffectStrength = effectStrength;
        CurrentTimer = 0;
        ID = Guid.NewGuid().ToString();
    }
    
    public void Apply(LuxController target){
        target.BuffManager.AddBuff(this);
        Effect?.ApplyEffect(target, EffectStrength);
    }

    public void Clear(LuxController target){
        Effect?.RemoveEffect(target, EffectStrength);
    }
}
