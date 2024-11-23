using System;

public class Buff {
    
    public BuffEffect Effect;
    public string Name;
    public float Duration;
    public float EffectStrength;
    public float CurrentTimer;
    public string ID;

    public Buff(BuffEffect effect, string name, float duration, float effectStrength) {
        Effect = effect;
        Name = name;
        Duration = duration;
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
