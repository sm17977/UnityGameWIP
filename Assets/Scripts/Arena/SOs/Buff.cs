using Google.Protobuf.WellKnownTypes;
using UnityEngine;

[CreateAssetMenu(fileName = "Buff", menuName = "Scriptable Objects/Buff")]
public class Buff : ScriptableObject
{

    [Header("Buff Overview")]
    public string name;
    public float duration;
    public float currentTimer;

    public void Apply(Lux_Controller target){
        target.buffManager.AddBuff(this);
        target.canMove = false;
    }

    public void Clear(Lux_Controller target){
        target.canMove = true;
    }
}
