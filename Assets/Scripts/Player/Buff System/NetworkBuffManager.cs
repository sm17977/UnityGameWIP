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
    
    private void Awake() {
    }

    private void Start() {
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

    public Dictionary<string, string> GetBuffs(ulong clientId) {
        var buffIds = new Dictionary<string, string>();

        foreach (var buffRecord in _buffStore[clientId]) {
            buffIds["Q"] = buffRecord.Buff.ID;
            break;
        }

        return buffIds;
    }

    /// <summary>
    /// Update the buff manager to handle buff duration
    /// </summary>
    public void Update() {

        if (!IsSpawned) return;
        if (_buffStore.Count == 0) return;

        // Iterate over buff store
        foreach (var entry in _buffStore) {
            var targetClientId = entry.Key;
            if (_buffStore.TryGetValue(targetClientId, out var buffRecords)) {
                var buffsToRemove = new List<BuffRecord>();
                
                foreach (var buffRecord in buffRecords) {
                    var buff = buffRecord.Buff;
                    
                    // Store buffs that have ended
                    if (buff.CurrentTimer <= 0) {
                        buffsToRemove.Add(buffRecord);
                        // Notify server and clients to remove the buff with attacker and target IDs
                        UpdateBuffOnServer(targetClientId, buff, false);
                    }
                    
                    // Reduce the duration of any buffs applied to a player
                    buff.CurrentTimer -= Time.deltaTime;
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
            buff.CurrentTimer = buff.Duration;
            var buffRecord = new BuffRecord(buff, sourceClientId);
            _buffStore[targetClientId].Add(buffRecord);
            Debug.Log("Adding buff for client ID:" + targetClientId);

            // Update buff on server and clients with attackerClientId
            UpdateBuffOnServer(targetClientId, buff, true);
            ApplyBuffOnClient(targetClientId, sourceClientId, buff.Key);
        }
    }


    /// <summary>
    /// Apply buff to the player on the server 
    /// </summary>
    /// <param name="targetClientId"></param>
    /// <param name="buff"></param>
    /// <param name="apply">Whether the buff is applied or removed</param>
    private void UpdateBuffOnServer(ulong targetClientId, Buff buff, bool apply) {
        var player = NetworkManager.Singleton.ConnectedClients[targetClientId].PlayerObject.gameObject;
        var playerScript = player.GetComponent<LuxPlayerController>();
        if (apply) {
            playerScript.ApplyBuff(buff);
        }
        else {
            playerScript.RemoveBuff(buff);
        }
    }

    /// <summary>
    /// Apply buff to the client receiving the buff
    /// </summary>
    /// <param name="targetClientId"></param>
    /// <param name="sourceClientId"></param>
    /// <param name="buffId"></param>
    /// <param name="apply">Whether the buff is applied or removed</param>
    private void ApplyBuffOnClient(ulong targetClientId, ulong sourceClientId, string abilityKey) {
        var targetPlayer = NetworkManager.Singleton.ConnectedClients[targetClientId].PlayerObject.gameObject;
        var sourcePlayer = NetworkManager.Singleton.ConnectedClients[sourceClientId].PlayerObject.gameObject;
        var test = NetworkManager.Singleton.ConnectedClients[sourceClientId].PlayerObject.gameObject
            .GetComponent<NetworkObject>();
        var sourcePlayerScript = sourcePlayer.GetComponent<LuxController>();
        var champion = sourcePlayerScript.champion.championName;
        var rpcController = targetPlayer.GetComponent<RPCController>();
        rpcController.ApplyBuffRpc(targetClientId, champion, abilityKey, test);
    }

    public bool FindBuff(ulong clientId, string abilityKey) {
        if(_buffStore.TryGetValue(clientId, out var buffRecords)) {
            return buffRecords.Any(buffRecord => buffRecord.Buff.Key == abilityKey);
        }
        return false;
    }
}
