using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Ability", menuName = "Scriptable Objects/Ability")]
public class Ability : ScriptableObject
{

    [Header("Ability Info")]
    public string abilityName;
    public string abilityDescription;

    [Header("Object Data")]
    public GameObject missile;
    public float spawnHeight;

    [Header("Scripts")]
    public UnityEngine.Object spawnScript;
    public UnityEngine.Object hitScript;
   
    [Header("Ability Stats")]
    public float currentCooldown = 0;
    public float maxCooldown;
    public float range;
    public float speed;
    public float lingeringLifetime;


    public void PutOnCooldown(){
        Cooldown_Manager.instance.StartCooldown(this);
    }

    public bool OnCooldown(){
        return currentCooldown > 0;
    }

}
