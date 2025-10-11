using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;

public class NetworkCooldownManager : NetworkBehaviour {
    
    public static NetworkCooldownManager Instance;

    private readonly Dictionary<ulong, Dictionary<int, double>> _cooldowns = new();
    
    private double ServerNow => 
        NetworkManager != null
            ? NetworkManager.ServerTime.Time
            : Time.unscaledTimeAsDouble;
    
    // Dictionary to map each player's GameObject to their list of abilities on cooldown
    //private Dictionary<ulong, List<NetworkAbilityData>> _cooldowns = new();
    public override void OnNetworkSpawn() {
        
        base.OnNetworkSpawn();

        if (!IsServer) {
            enabled = false;
            gameObject.SetActive(false);
            return;
        }
        
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(this);
    }
    
    /// <summary>
    /// Start a cooldown timer of an ability for a specific player
    /// </summary>
    /// <param name="playerId">The player GameObject</param>
    /// <param name="abilityId">The ability to put on cooldown</param>
    /// <param name="maxCooldownSeconds">Ability cd time in seconds</param>
    public void StartCooldown(ulong playerId, int abilityId, double maxCooldownSeconds) {
        if (!_cooldowns.TryGetValue(playerId, out var map)) {
            map = new Dictionary<int, double>(4);
            _cooldowns[playerId] = map;
        }

        var expiry = ServerNow + Math.Max(0d, maxCooldownSeconds);
        map[abilityId] = expiry;
    }

    /// <summary>
    /// Check if a player's ability is on cooldown
    /// </summary>
    /// <param name="playerId">The player GameObject</param>
    /// <param name="abilityId">The ability to check</param>
    /// <returns>True if the ability is on cooldown, otherwise false</returns>
    public bool IsAbilityOnCooldown(ulong playerId, int abilityId) {
        
        if (!_cooldowns.TryGetValue(playerId, out var map)) return false;
        if (!map.TryGetValue(abilityId, out var expiry)) return false;

        var now = ServerNow;
        if (expiry > now) return true;

        map.Remove(abilityId);
        if (map.Count == 0) _cooldowns.Remove(playerId);
        return false;
    }
}
