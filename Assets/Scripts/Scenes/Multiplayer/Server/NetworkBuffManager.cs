using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class NetworkBuffManager : NetworkBehaviour {

    public static NetworkBuffManager Instance;
    private Dictionary<ulong, List<BuffRecord>> _buffMappings;
        
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
        _buffMappings = new();
    }
    
    private void Awake() {
    }

    private void Start() {
    }

    
    /// <summary>
    /// Update buff mappings when a player joins/leaves
    /// </summary>
    public void AddMappings() {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList) {
            _buffMappings[client.ClientId] = new List<BuffRecord>();
        }
    }

    public void RemoveMapping(ulong clientId) {
        if (_buffMappings.ContainsKey(clientId)) {
            _buffMappings.Remove(clientId);
        }
    }

    public Dictionary<string, string> GetBuffs(ulong clientId) {
        var buffIds = new Dictionary<string, string>();

        foreach (var buffRecord in _buffMappings[clientId]) {
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
        
        // Check if there are any clients connected before updating buffs
        if (NetworkManager.Singleton.ConnectedClientsList.Count == 0) return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList) {
            var targetClientId = client.ClientId;

            if (_buffMappings.TryGetValue(targetClientId, out var buffRecords)) {
                var buffsToRemove = new List<BuffRecord>();

                foreach (var buffRecord in buffRecords) {
                    var buff = buffRecord.Buff;
                    buff.CurrentTimer -= Time.deltaTime;
                
                    if (buff.CurrentTimer <= 0) {
                        buffsToRemove.Add(buffRecord);
                    
                        // Notify server and clients to remove the buff with attacker and target IDs
                        UpdateBuffOnServer(targetClientId, buff, false);
                        UpdateBuffOnClients(buffRecord.AttackerClientId, targetClientId, buff.ID, false);
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
    /// /// <param name="attackerClientId">The client of the player delegating the buff</param>
    public void AddBuff(Buff buff, ulong attackerClientId, ulong targetClientId) {
        if (!_buffMappings.ContainsKey(targetClientId)) {
            _buffMappings[targetClientId] = new List<BuffRecord>();
        }

        // Check if this buff is already applied to prevent duplicates
        if (_buffMappings[targetClientId].All(record => record.Buff.ID != buff.ID)) {
            buff.CurrentTimer = buff.Duration;
            var buffRecord = new BuffRecord(buff, attackerClientId);
            _buffMappings[targetClientId].Add(buffRecord);
            Debug.Log("BUFF MAPPING ADD - " + buff.ID);

            // Update buff on server and clients with attackerClientId
            UpdateBuffOnServer(targetClientId, buff, true);
            UpdateBuffOnClients(attackerClientId, targetClientId, buff.ID, true);
        }
    }


    /// <summary>
    /// Apply buff to the player on the server 
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="buff"></param>
    /// <param name="apply">Whether the buff is applied or removed</param>
    private void UpdateBuffOnServer(ulong clientId, Buff buff, bool apply) {
        var player = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject;
        var playerScript = player.GetComponent<LuxController>();
        if (apply) {
            buff.Effect.ApplyEffect(playerScript, buff.EffectStrength);
        }
        else {
            buff.Effect.RemoveEffect(playerScript, buff.EffectStrength);
        }
    }

    /// <summary>
    /// Apply buff to player on all clients
    /// </summary>
    /// <param name="targetClientId"></param>
    /// <param name="sourceClientId"></param>
    /// <param name="buffId"></param>
    /// <param name="apply">Whether the buff is applied or removed</param>
    private void UpdateBuffOnClients(ulong sourceClientId, ulong targetClientId, string buffId, bool apply) {
        var player = NetworkManager.Singleton.ConnectedClients[targetClientId].PlayerObject.gameObject;
        var rpcController = player.GetComponent<RPCController>();
        rpcController.UpdateBuffRpc(sourceClientId, targetClientId, buffId, apply);
    }
}
