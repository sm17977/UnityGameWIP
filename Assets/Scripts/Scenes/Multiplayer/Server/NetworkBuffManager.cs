using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class NetworkBuffManager : NetworkBehaviour {

    public static NetworkBuffManager Instance;
    private Dictionary<ulong, List<Buff>> _buffMappings;
        
    public override void OnNetworkSpawn(){
        base.OnNetworkSpawn();
        Debug.Log("NetworkBuffManager Spawn");
        if (IsServer) {
            Debug.Log("NetworkBuffManager IsServer");
            if (Instance == null) {
                Instance = this;
                Debug.Log("NetworkBuffManager Get Instance");
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
        Debug.Log("NetworkBuffManager - Awake");
    }

    private void Start() {
        Debug.Log("NetworkBuffManager - Start");
    }

    
    /// <summary>
    /// Update buff mappings when a player joins/leaves
    /// </summary>
    public void AddMappings() {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList) {
            _buffMappings[client.ClientId] = new List<Buff>();
        }
    }

    public void RemoveMapping(ulong clientId) {
        if (_buffMappings.ContainsKey(clientId)) {
            _buffMappings.Remove(clientId);
        }
    }

    /// <summary>
    /// Update the buff manager to handle buff duration
    /// </summary>
    public void Update() {
        // Check if there are any clients connected before updating buffs
        if (NetworkManager.Singleton.ConnectedClientsList.Count == 0) return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList) {
            var clientId = client.ClientId;

            if (_buffMappings.TryGetValue(clientId, out var buffs)) {
                var buffsToRemove = new List<Buff>();

                foreach (var buff in buffs) {
                    buff.currentTimer -= Time.deltaTime;
                    if (buff.currentTimer <= 0) {
                        Debug.Log("Clearing buff");
                        buffsToRemove.Add(buff);
                        UpdateBuffOnServer(clientId, buff, false);
                        UpdateBuffOnClients(clientId, buff, false);
                    }
                }
                
                foreach (var buff in buffsToRemove) {
                    buffs.Remove(buff);
                }
            }
        }
    }

    /// <summary>
    /// Add a buff to the buff list for a player
    /// </summary>
    /// <param name="buff">The buff to add</param>
    /// /// <param name="clientId">The client of the player</param>
    public void AddBuff(Buff buff, ulong clientId) {
        if (_buffMappings.ContainsKey(clientId)) {
            if (!_buffMappings[clientId].Contains(buff)) {
                buff.currentTimer = buff.duration;
                _buffMappings[clientId].Add(buff);
                UpdateBuffOnServer(clientId, buff, true);
                UpdateBuffOnClients(clientId, buff, true);
            }
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
            buff.effect.ApplyEffect(playerScript, buff.effectStrength);
        }
        else {
            buff.effect.RemoveEffect(playerScript, buff.effectStrength);
        }
    }

    /// <summary>
    /// Apply buff to player on all clients
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="buff"></param>
    /// <param name="apply">Whether the buff is applied or removed</param>
    private void UpdateBuffOnClients(ulong clientId, Buff buff, bool apply) {
        var player = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject;
        var rpcController = player.GetComponent<RPCController>();
        rpcController.UpdateBuffRpc(clientId, buff.name, apply);
    }
    
    
    /// <summary>
    /// Check if buff has been applied to a player
    /// </summary>
    /// <param name="buff">The buff to check</param>
    /// <param name="clientId">The client of the player</param>
    /// <returns>boolean</returns>
    public bool HasBuffApplied(Buff buff, ulong clientId) {
        if (_buffMappings.ContainsKey(clientId)) {
            return _buffMappings[clientId].Contains(buff);
        }
        return false;
    }
}
