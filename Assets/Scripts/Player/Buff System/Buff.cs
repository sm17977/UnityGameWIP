using System;
using Unity.Netcode;
using UnityEngine;

public class Buff{
    
    public BuffEffect Effect;
    public string Key;
    public float Duration;
    public float EffectStrength;
    public string ID;
   
    public double BuffEndTime { get;  set; }
    
    public Buff(BuffEffect effect, string key, float duration, float effectStrength) {
        Effect = effect;
        Key = key;
        Duration = duration;
        EffectStrength = effectStrength;
        ID = Guid.NewGuid().ToString();
    }
    
    public void Apply(LuxController target){
        target.ClientBuffManager.AddBuff(this);
        Effect?.ApplyEffect(target, EffectStrength);
    }

    public void Clear(LuxController target){
        Effect?.RemoveEffect(target, EffectStrength);
    }

    public bool IsExpired(double serverTimeNow) {
        return serverTimeNow >= BuffEndTime;
    }
}
