using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class NetworkCooldownManager : NetworkBehaviour {
    public static NetworkCooldownManager Instance;

    // Dictionary to map each player's GameObject to their list of abilities on cooldown
    private Dictionary<GameObject, List<NetworkAbilityData>> _cooldowns = new();
    
    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        Debug.Log("NetworkCooldownManager - OnNetWorkSpawn");
        Debug.Log("IsServer: " + IsServer);
        if (IsServer) {
            if (Instance == null) {
                Instance = this;
            }
            else if (Instance != this) {
                Destroy(this);
            }
        }
        else {
            gameObject.SetActive(false);
        }
    }

    private void Awake() {
        Debug.Log("NetworkCooldownManager - Awake");
    }

    private void Start() {
        Debug.Log("NetworkCooldownManager - Start");
    }

    private void Update() {
        foreach (var playerAbilities in _cooldowns.ToList()) {
            var player = playerAbilities.Key;
            var abilities = playerAbilities.Value;
            
            foreach (var ability in abilities.ToList()) {
                ability.currentCooldown -= Time.deltaTime;

                if (ability.currentCooldown <= 0) {
                    ability.currentCooldown = 0;
                    abilities.Remove(ability); 
                }
            }

            if (!abilities.Any()) {
                _cooldowns.Remove(player);
            }
        }
    }

    /// <summary>
    /// Start cooldown timer of an ability for a specific player
    /// </summary>
    /// <param name="player">The player GameObject</param>
    /// <param name="ability">The ability to put on cooldown</param>
    public void StartCooldown(GameObject player, NetworkAbilityData ability) {
        if (!_cooldowns.ContainsKey(player)) {
            _cooldowns[player] = new List<NetworkAbilityData>();
        }
        
        if (!_cooldowns[player].Contains(ability)) {
            ability.currentCooldown = ability.maxCooldown;
            _cooldowns[player].Add(ability);
        }
    }

    /// <summary>
    /// Check if a player's ability is on cooldown
    /// </summary>
    /// <param name="player">The player GameObject</param>
    /// <param name="ability">The ability to check</param>
    /// <returns>True if the ability is on cooldown, otherwise false</returns>
    public bool IsAbilityOnCooldown(GameObject player, NetworkAbilityData ability) {
        Debug.Log("IsAbilityOnCooldown");
        var playerIsNull = player == null;
        var abilityIsNull = ability == null;
        var cooldownsIsNull = _cooldowns == null;

        Debug.Log("Player is null? " + playerIsNull);
        Debug.Log("Ability is null? " + abilityIsNull);
        Debug.Log("Cooldowns is null? " +  cooldownsIsNull);
        
        try {
            if (_cooldowns.ContainsKey(player) && _cooldowns[player].Contains(ability)) {
                return _cooldowns[player].Find(ele => ele.key == ability?.key).currentCooldown > 0;
            }
        }
        catch (Exception e) {
            Debug.Log("Error in IsAbilityOnCooldown: " + e);
            return false;
        }

        Debug.Log("couldn't find cooldown/player! ):");
        return false;
    }
}
