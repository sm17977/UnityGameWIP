using System;
using System.Collections.Generic;
using QFSW.QC;
using Unity.Netcode;
using UnityEngine;

public class RPCController : NetworkBehaviour {

    public event Action<bool> OnCooldownReceived;
    public static event Action<GameObject> NetworkSpawn;
    
    public Dictionary<string, GameObject> NetworkActiveAbilityPrefabs;
    
    private LuxPlayerController _playerController;
    private NetworkStateManager _networkState;
    private GameObject _player;
    private GameObject _players;
    
    
    private void Start() {
        NetworkActiveAbilityPrefabs = new Dictionary<string, GameObject>();
        _playerController = GetComponent<LuxPlayerController>();
        _networkState = GetComponent<NetworkStateManager>();
        _player = gameObject;
    }

    public override void OnNetworkSpawn() {
        // Set local player game object name to 'Local Player'
        gameObject.name = IsLocalPlayer ? "Local Player" : GetComponent<NetworkObject>().NetworkObjectId.ToString();
        
        // Make the player a child object of the 'Players' game object
        _players = GameObject.Find("Players");
        if(IsServer)transform.SetParent(_players.transform, true);
        
        // Trigger event to let the UI script know the player has spawned on the network
        if(IsOwner)NetworkSpawn?.Invoke(gameObject);
    }
    
    /// <summary>
    /// Add an input command to the server's input queue
    /// </summary>
    /// <param name="input"></param>
    [Rpc(SendTo.Server)]
    public void SendInputRpc(InputPayload input) {
        _networkState.serverInputQueue.Enqueue(input);
    }
    
    /// <summary>
    /// Update the client with the last server state
    /// </summary>
    /// <param name="statePayload"></param>
    [Rpc(SendTo.ClientsAndHost)]
    public void SendStateRpc(StatePayload statePayload) {
        if (!IsOwner) return;
        _networkState.lastServerState = statePayload;
    }

    /// <summary>
    /// Spawn a projectile on the server
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="position"></param>
    /// <param name="clientId"></param>
    /// <param name="instanceId"></param>
    /// <param name="abilityKey"></param>
    [Rpc(SendTo.Server)]
    public void SpawnProjectileServerRpc(Vector3 direction, Vector3 position, ulong clientId, int instanceId, string abilityKey) {
        
        var newNetworkProjectile = ServerObjectPool.Instance.GetPooledObject(_playerController.Abilities[abilityKey], AbilityPrefabType.Projectile);
        if (newNetworkProjectile == null) {
            Debug.Log("No available projectiles in the pool");
            return;
        }

        NetworkActiveAbilityPrefabs[abilityKey] = newNetworkProjectile;
        var networkInstanceId = newNetworkProjectile.transform.GetInstanceID();
        
        // Set the position and rotation of the projectile
        newNetworkProjectile.transform.position = position;
        newNetworkProjectile.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        // Activate the projectile
        newNetworkProjectile.SetActive(true);
        
        var networkObject = newNetworkProjectile.GetComponent<NetworkObject>();
        if (!networkObject.IsSpawned) {
            networkObject.Spawn(true);
        }
        
        // Initialize projectile properties on the server
        var projectileScript = newNetworkProjectile.GetComponent<NetworkProjectileAbility>();
        if (projectileScript != null) {
            Debug.Log("Init Projectile Server Ability - " + abilityKey);
            projectileScript.InitProjectileProperties(direction, _playerController.Abilities[abilityKey], _playerController.playerType, clientId);
            projectileScript.Mappings[instanceId] = clientId;
        }
        else {
            Debug.Log("Projectile Script is null");
        }
        SpawnProjectileClientRpc(direction, position, networkInstanceId, abilityKey);
    }

    /// <summary>
    /// Spawn the projectile for all clients excluding the source
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="position"></param>
    /// <param name="networkInstanceId"></param>
    /// <param name="abilityKey"></param>
    [Rpc(SendTo.NotOwner)]
    private void SpawnProjectileClientRpc(Vector3 direction, Vector3 position, int networkInstanceId, string abilityKey) {
        if (!IsOwner && !IsServer) {

            var ability = _playerController.Abilities[abilityKey];
        
            var newProjectile = ClientObjectPool.Instance.GetPooledObject(ability, AbilityPrefabType.Projectile);
            if (newProjectile == null) {
                Debug.Log("No available projectiles in the pool");
            }

            // Set the position and rotation of the projectile
            newProjectile.transform.position = position;
            newProjectile.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

            // Activate the projectile
            newProjectile.SetActive(true);
            
            // Initialize projectile properties on the server
            var projectileScript = newProjectile.GetComponent<ProjectileAbility>();
            if (projectileScript != null) {
                projectileScript.InitProjectileProperties(direction, ability,
                    _playerController.projectiles, _playerController.playerType, _playerController);
                projectileScript.ResetVFX();
            }
            else {
                Debug.Log("ProjectileAbility component is missing on the projectile");
            }
            
            var playerNetworkBehaviour = _player.GetComponent<NetworkBehaviour>();
            var localClientId = playerNetworkBehaviour.NetworkManager.LocalClientId;
            var localInstanceId = newProjectile.transform.GetInstanceID();
            
            UpdateMappingsServerRpc(localClientId, localInstanceId, networkInstanceId);
        }
    }

    /// <summary>
    /// Update projectile mappings on the server
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="localProjectileId"></param>
    /// <param name="networkInstanceId"></param>
    [Rpc(SendTo.Server)]
    private void UpdateMappingsServerRpc(ulong clientId, int localProjectileId, int networkInstanceId) {
        var networkProjectile = GameObject.Find(networkInstanceId.ToString());
        var networkProjectileScript = networkProjectile.GetComponent<NetworkProjectileAbility>();
        networkProjectileScript.Mappings[localProjectileId] = clientId;
    }

    [Rpc(SendTo.Server)]
    public void ReCastAbilityServerRpc(ulong clientId, string abilityKey) {
        var networkProjectile = NetworkActiveAbilityPrefabs[abilityKey];
        var networkProjectileScript = networkProjectile.GetComponent<NetworkProjectileAbility>();
        networkProjectileScript.ReCast();
    }

    /// <summary>
    /// Add ability cooldown on the server
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="abilityData"></param>
    [Rpc(SendTo.Server)]
    public void AddCooldownRpc(ulong clientId, NetworkAbilityData abilityData) {
        NetworkCooldownManager.Instance.StartCooldown(clientId, abilityData);
    }
    
    /// <summary>
    /// Check ability cooldown on the server
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="abilityData"></param>
    [Rpc(SendTo.Server)]
    public void IsAbilityOnCooldownRpc(ulong clientId, NetworkAbilityData abilityData) {
        var cd = NetworkCooldownManager.Instance.IsAbilityOnCooldown(clientId, abilityData);
        UpdateCooldownRpc(cd);
    }
    
    /// <summary>
    /// Update cooldown on the client
    /// </summary>
    /// <param name="serverCooldown"></param>
    [Rpc(SendTo.Owner)]
    private void UpdateCooldownRpc(bool serverCooldown) {
        if (IsLocalPlayer && IsOwner) {
            OnCooldownReceived?.Invoke(serverCooldown);
            OnCooldownReceived = null;
        }
    }

    /// <summary>
    /// Apply buff to client
    /// </summary>
    /// <param name="targetClientId"></param>
    /// <param name="champion"></param>
    /// <param name="key"></param>
    /// <param name="sourceNetworkObject"></param>
    [Rpc(SendTo.Owner)]
    public void ApplyBuffRpc(ulong targetClientId, string champion, string key, NetworkObjectReference sourceNetworkObject) {

        // Only apply buff to the player and client who is having the buff applied
        if(!IsOwner) return;
        if(NetworkObject.OwnerClientId != targetClientId) return;

        Debug.Log("Apply buff RPC");
        
        // Get the buff from the source player's ability and apply it to the target player
        if (sourceNetworkObject.TryGet(out NetworkObject obj)) {
            var sourcePlayer = obj.gameObject;
            var sourcePlayerScript = sourcePlayer.GetComponent<LuxPlayerController>();
            var ability = sourcePlayerScript.Abilities[key];
            var buff = ability.buff;
            _playerController.ApplyBuff(buff);
        }
    }
}
