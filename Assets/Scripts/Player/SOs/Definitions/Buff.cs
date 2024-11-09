using System;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Buff", menuName = "Scriptable Objects/Buff")]
public class Buff : ScriptableObject {
    
    [FormerlySerializedAs("Effect")] [Header("Buff Overview")] 
    public BuffEffect effect;
    public string name;
    public float duration;
    public float effectStrength;
    public float currentTimer;
    public string id;

    public void Init() {
        if (String.IsNullOrEmpty(id)) {
            id = System.Guid.NewGuid().ToString();
        }
    }
    
    public void Apply(LuxController target){
        target.BuffManager.AddBuff(this);
        effect?.ApplyEffect(target, effectStrength);
    }

    public void Clear(LuxController target){
        effect?.RemoveEffect(target, effectStrength);
    }
}
