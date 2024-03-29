using Google.Protobuf.WellKnownTypes;
using UnityEngine;

[CreateAssetMenu(fileName = "Buff", menuName = "Scriptable Objects/Buff")]
public class Buff : ScriptableObject
{

    [Header("Buff Overview")]
    public string name;
    public float duration;
    public float currentTimer;

    public void ApplyBuff(){
        Buff_Manager.instance.AddBuff(this);
    }
}
