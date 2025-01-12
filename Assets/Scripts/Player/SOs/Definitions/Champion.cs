using UnityEngine;

[CreateAssetMenu(fileName = "Champion", menuName = "Scriptable Objects/Champion")]
public class Champion : ScriptableObject {

    [Header("Description")]
    public string championName;

    [Header("Base Stats")] 
    public int health;
    public float movementSpeed;
    public float turnSpeed;
    public float windupTime;
    public float stoppingDistance;

    [Header("Abilities")] 
    public Ability abilityQ;
    public Ability abilityW;
    public Ability abilityE;
    public Ability abilityR;
    
    [Header("Auto Attacks")] 
    public float AA_range;
    public float AA_missileSpeed;
    public double AA_attackSpeed; 
    public Vector3 AA_direction;

    [Header("Properties")] 
    public float modelHeight;
    public float hitBoxRadius;

}
