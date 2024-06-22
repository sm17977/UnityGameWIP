using UnityEngine;

[CreateAssetMenu(fileName = "Buff", menuName = "Scriptable Objects/Buff")]
public class Buff : ScriptableObject
{

    [Header("Buff Overview")]
    public string name;
    public float duration;
    public float currentTimer;

    public void Apply(LuxController target){
        target.BuffManager.AddBuff(this);
        target.canMove = false;
    }

    public void Clear(LuxController target){
        target.canMove = true;
    }
}
