using System;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "Ability", menuName = "Scriptable Objects/Ability")]
public class Ability : ScriptableObject {

    [Header("Ability Info")]
    public string abilityName;
    public string abilityDescription;
    public Champion champion;
    public string key;

    [Header("Ability Properties")] 
    public float damage;
    public float currentCooldown = 0;
    public float maxCooldown;
    public float range;
    public float speed;
    public float lingeringLifetime;
    public bool hasRecast;
    public bool canRecast;


    [Header("Buffs/Debuffs")]
    public Buff buff;

   [Header("Prefab Data")]
    public GameObject missilePrefab;
    public GameObject networkMissilePrefab;
    public GameObject hitPrefab;
    public GameObject spellIndicatorPrefab;
    public float spawnHeight;
    public float hitboxRadius;

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

    public void Cast(Vector3 direction, Vector3 targetPos, Vector3 abilitySpawnPos) {
        _castingStrategy.Cast(this, direction, targetPos, abilitySpawnPos);
    }

    public void Recast(GameObject projectile) {
        _castingStrategy.Recast(projectile, key);
    }
 
    public float GetLifetime(){
        return range / speed;
    }

    public float GetTotalLifetime(){
        return GetLifetime() + lingeringLifetime;
    }

    public void PutOnCooldown(){
        CooldownManager.Instance.StartCooldown(this);
    }

    /// <summary>
    /// Networked version
    /// </summary>
    /// <param name="player"></param>
    public void PutOnCooldown_Net(GameObject player) {
        var rpcController = player.GetComponent<RPCController>();
        var clientId = NetworkManager.Singleton.LocalClientId;
        
        NetworkAbilityData abilityData = new NetworkAbilityData(this);
        rpcController.AddCooldownRpc(clientId, abilityData);
    }

    public bool OnCooldown(){
        return currentCooldown > 0;
    }
    
    /// <summary>
    /// Networked version
    /// </summary>
    /// <param name="player"></param>
    /// <param name="onCooldownReceived"></param>
    /// <returns></returns>
    public void OnCooldown_Net(GameObject player, Action<bool> onCooldownReceived){
        var rpcController = player.GetComponent<RPCController>();
        var clientId = NetworkManager.Singleton.LocalClientId;
        NetworkAbilityData abilityData = new NetworkAbilityData(this);
        
        rpcController.IsAbilityOnCooldownRpc(clientId, abilityData);
        rpcController.OnCooldownReceived += onCooldownReceived;
    }
}
