using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Ability", menuName = "Scriptable Objects/Ability")]
public class Ability : ScriptableObject
{

    [Header("Ability Info")]
    public string abilityName;
    public string abilityDescription;
    public Champion champion;
    public string key;

    [Header("Ability Stats")]
    public float currentCooldown = 0;
    public float maxCooldown;
    public float range;
    public float speed;
    public float lingeringLifetime;

    [Header("Buffs/Debuffs")]
    public Buff buff;

   [Header("Prefab Data")]
    public GameObject missilePrefab;
    public GameObject networkMissilePrefab;
    public GameObject hitPrefab;
    public float spawnHeight;

    [Header("Scripts")]
    public UnityEngine.Object spawnScript;
    public UnityEngine.Object hitScript;

    [Header("Animations")]
    public Motion animationClip;
    public string animationTrigger;
    public string animationState;
   
    private ICastingStrategy _castingStrategy;

    public void SetCastingStrategy(ICastingStrategy strategy) {
        _castingStrategy = strategy;
    }

    public void Cast(Vector3 direction, Vector3 abilitySpawnPos) {
        _castingStrategy.Cast(this, direction, abilitySpawnPos);
    }
 
    public float GetProjectileLifetime(){
        return range / speed;
    }

    public float GetTotalLifetime(){
        return GetProjectileLifetime() + lingeringLifetime;
    }

    public void PutOnCooldown(){
        CooldownManager.Instance.StartCooldown(this);
    }

    public bool OnCooldown(){
        return currentCooldown > 0;
    }
}
