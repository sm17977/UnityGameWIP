using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Ability", menuName = "Scriptable Objects/Ability")]
public class Ability : ScriptableObject
{

    [Header("Ability Info")]
    public string abilityName;
    public string abilityDescription;


    [Header("Ability Stats")]
    public float currentCooldown = 0;
    public float maxCooldown;


    public void PutOnCooldown(){
        Cooldown_Manager.instance.StartCooldown(this);
    }

    public bool OnCooldown(){
        return currentCooldown > 0;
    }

}
