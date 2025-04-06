using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;

public class NetworkCooldownManager : NetworkBehaviour {
    public static NetworkCooldownManager Instance;

    // Dictionary to map each player's GameObject to their list of abilities on cooldown
    private Dictionary<ulong, List<NetworkAbilityData>> _cooldowns = new();
    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
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
    }

    private void Start() {
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

            // Remove player from mapping if they have no ability cooldowns in their list
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
    public void StartCooldown(ulong player, NetworkAbilityData ability) {
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
    public bool IsAbilityOnCooldown(ulong player, NetworkAbilityData ability) {
        string jsonCooldowns = JsonConvert.SerializeObject(_cooldowns);
        Debug.Log("==Network Cooldowns==");
        Debug.Log(jsonCooldowns);
        
        try {
            if (_cooldowns.ContainsKey(player)) {
                return _cooldowns[player].Find(ele => ele.key == ability?.key).currentCooldown > 0;
            }
        }
        catch (Exception e) {
            Debug.Log("Error in IsAbilityOnCooldown: " + e);
            Debug.Log("Client ID: " + player);
            Debug.Log("Ability key: " + ability?.key);
            Debug.Log("_cooldowns[player]: " + _cooldowns[player]);
        }
        return false;
    }
}
