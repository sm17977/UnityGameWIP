using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class NetworkBuffManager : NetworkBehaviour {

    public static NetworkBuffManager Instance;
    private Dictionary<ulong, List<BuffRecord>> _buffStore;
        
    public override void OnNetworkSpawn(){
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
        _buffStore = new();
    }
    
    /// <summary>
    /// Add a player to the buff store
    /// </summary>
    public void AddPlayerToBuffStore(ulong clientId) {
        if(!_buffStore.ContainsKey(clientId)) _buffStore[clientId] = new List<BuffRecord>();
    }

    /// <summary>
    /// Remove a player from the buff store
    /// </summary>
    /// <param name="clientId"></param>
    public void RemovePlayerFromBuffStore(ulong clientId) {
        if (_buffStore.ContainsKey(clientId)) _buffStore.Remove(clientId);
    }

    
    /// <summary>
    /// Update the buff manager to handle buff duration
    /// </summary>
    public void Update() {

        if (!IsSpawned || _buffStore.Count == 0) return;
        double serverTimeNow = NetworkManager.ServerTime.Time;
        
        // Iterate over buff store
        foreach (var entry in _buffStore) {
            
            var targetClientId = entry.Key;
            
            if (_buffStore.TryGetValue(targetClientId, out var buffRecords)) {
                
                var buffsToRemove = new List<BuffRecord>();
                
                foreach (var buffRecord in buffRecords) {
                    
                    var buff = buffRecord.Buff;
                    
                    // Store buffs that have ended
                    if (buff.IsExpired(serverTimeNow)) {
                        buffsToRemove.Add(buffRecord);
                        UpdateBuffOnServer(targetClientId, buff, false);
                    }
                }

                // Remove expired buffs
                foreach (var buffRecord in buffsToRemove) {
                    buffRecords.Remove(buffRecord);
                }
            }
        }
    }
    
    /// <summary>
    /// Add a buff to the buff list for a player
    /// </summary>
    /// <param name="buff">The buff to add</param>
    /// /// <param name="targetClientId">The client of the player receving the buff</param>
    /// /// <param name="sourceClientId">The client of the player delegating the buff</param>
    public void AddBuff(Buff buff, ulong sourceClientId, ulong targetClientId) {
        if (!_buffStore.ContainsKey(targetClientId)) {
            _buffStore[targetClientId] = new List<BuffRecord>();
        }

        // Check if this buff is already applied to prevent duplicates
        if (_buffStore[targetClientId].All(record => record.Buff.ID != buff.ID)) {
            buff.BuffEndTime = NetworkManager.ServerTime.Time + buff.Duration;
            var buffRecord = new BuffRecord(buff, sourceClientId);
            _buffStore[targetClientId].Add(buffRecord);
            UpdateBuffOnServer(targetClientId, buff, true);
        }
    }

    /// <summary>
    /// Apply buff to the player on the server 
    /// </summary>
    /// <param name="targetClientId"></param>
    /// <param name="buff"></param>
    /// <param name="apply">Whether the buff is applied or removed</param>
    private void UpdateBuffOnServer(ulong targetClientId, Buff buff, bool apply) {
        if(!IsServer) return;
        var player = NetworkManager.Singleton.ConnectedClients[targetClientId].PlayerObject.gameObject;
        var playerScript = player.GetComponent<LuxPlayerController>();
        if (apply) {
            playerScript.ApplyBuff(buff);
        }
        else {
            playerScript.RemoveBuff(buff);
        }
    }
}
