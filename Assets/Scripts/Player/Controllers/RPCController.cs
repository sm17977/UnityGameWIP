using System;
using System.Collections.Generic;
using QFSW.QC;
using Scenes.Multiplayer.GameChat;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class RPCController : NetworkBehaviour {
    public event Action<bool> OnCooldownReceived;
    public event Action OnPlayerNameSet;
    public static event Action<GameObject> NetworkSpawn;
    
    public ChatServer chatServer;
    
    private Dictionary<string, GameObject> _networkActiveAbilityPrefabs;
    private LuxPlayerController _playerController;
    private NetworkStateManager _networkState;
    private GameObject _player;
    private GameObject _players;
    
    private void Start() {
        _networkActiveAbilityPrefabs = new Dictionary<string, GameObject>();
        _playerController = GetComponent<LuxPlayerController>();
        _networkState = GetComponent<NetworkStateManager>();
        _player = gameObject;
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        
        // Set local player game object name to 'Local Player'
        gameObject.name = IsLocalPlayer ? "Local Player" : GetComponent<NetworkObject>().NetworkObjectId.ToString();
        
        // Make the player a child object of the 'Players' game object
        _players = GameObject.Find("Players");
        if(IsServer) transform.SetParent(_players.transform, true);

        if (IsServer) {
            _players = GameObject.Find("Players");
        }
        
        var chatServerGameObj =  GameObject.Find("Chat Server");
        chatServer = chatServerGameObj.GetComponent<ChatServer>();
        
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
    /// <param name="targetDirection"></param>
    /// <param name="position"></param>
    /// <param name="targetPosition"></param>
    /// <param name="clientId"></param>
    /// <param name="instanceId"></param>
    /// <param name="abilityKey"></param>
    [Rpc(SendTo.Server)]
    public void SpawnProjectileServerRpc(Vector3 targetDirection, Vector3 position, Vector3 targetPosition, ulong clientId, string abilityKey) {

        var ability = _playerController.Abilities[abilityKey];
        var newNetworkProjectile = ServerObjectPool.Instance.GetPooledObject(ability, AbilityPrefabType.Spawn);
        if (newNetworkProjectile == null) {
            Debug.Log("No available projectiles in the pool");
            return;
        }

        _networkActiveAbilityPrefabs[abilityKey] = newNetworkProjectile;
        
        // Set the position and rotation of the projectile
        newNetworkProjectile.transform.position = position;
        newNetworkProjectile.transform.rotation = Quaternion.LookRotation(targetDirection, Vector3.up);
        
        // Initialize projectile properties on the server
        var projectileScript = newNetworkProjectile.GetComponent<NetworkAbilityBehaviour>();
        projectileScript.InitialiseProperties(ability, targetDirection, targetPosition, _playerController.playerType, clientId);
        
        // Activate the projectile
        newNetworkProjectile.SetActive(true);
        
        // Make sure it's spawned
        var networkObject = newNetworkProjectile.GetComponent<NetworkObject>();
        if (!networkObject.IsSpawned) {
            networkObject.Spawn(true);
        }

        var networkObjectId = networkObject.NetworkObjectId;
        
        SpawnProjectileClientRpc(targetDirection, targetPosition, position, networkObjectId, abilityKey);
        SendNetworkObjectIdClientRpc(networkObjectId, abilityKey);
    }

    [Rpc(SendTo.Server)]
    public void SpawnAAServerRpc(NetworkObjectReference targetRef, Vector3 startPos, Quaternion startRot) {
        var newNetworkAA = ServerObjectPool.Instance.GetPooledAutoAttack();
        var autoAttackScript = newNetworkAA.GetComponent<NetworkAutoAttackController>();
        var networkObjectAA = newNetworkAA.GetComponent<NetworkObject>();
        if (targetRef.TryGet(out NetworkObject obj)) {
            var targetPlayer = obj.gameObject;
            var direction = (targetPlayer.transform.position - startPos).normalized; 
            newNetworkAA.SetActive(true);
            
            if (networkObjectAA != null && !networkObjectAA.IsSpawned) {
                networkObjectAA.Spawn();
            }
            
            autoAttackScript.Initialise(_player, targetPlayer, startPos);
            var sourceRef = _player.gameObject.GetComponent<NetworkObject>();
            SpawnAAClientRpc(sourceRef, targetRef, startPos, startRot, direction);
        }
    }

    [Rpc(SendTo.NotOwner)]
    private void SpawnAAClientRpc(NetworkObjectReference sourceRef, NetworkObjectReference targetRef, Vector3 startPos, Quaternion startRot, Vector3 direction) {
        if (!IsServer) {
            if (sourceRef.TryGet(out NetworkObject sourceNetworkObj) &&
                targetRef.TryGet(out NetworkObject targetNetworkObj)) {
                var sourcePlayer = sourceNetworkObj.gameObject;
                var targetPlayer = targetNetworkObj.gameObject;

                var sourcePlayerScript = sourcePlayer.GetComponent<LuxPlayerController>();
                sourcePlayerScript.currentAATarget = targetPlayer;
                
                var autoAttack = ClientObjectPool.Instance.GetPooledAutoAttack();
                if (autoAttack != null) {
                    autoAttack.SetActive(true);
                    var autoAttackController = autoAttack.GetComponent<ClientAutoAttackController>();
                    autoAttackController.Initialise(sourcePlayer, startPos, startRot, direction);
                }
            }
        }
    }
    
    [Rpc(SendTo.Owner)]
    private void SendNetworkObjectIdClientRpc(ulong networkObjectId, string abilityKey) {
        if (IsServer) return;
        var prefab = _playerController.ActiveAbilityPrefabs[abilityKey];
        var prefabScript = prefab.GetComponent<ClientAbilityBehaviour>();
        prefabScript.linkedNetworkObjectId = networkObjectId;
        ClientPrefabManager.Instance.RegisterPrefab(networkObjectId, prefab);
    }

    /// <summary>
    /// Spawn the projectile for all clients excluding the source
    /// </summary>
    /// <param name="targetDir"></param>
    /// <param name="targetPos"></param>
    /// <param name="position"></param>
    /// <param name="networkObjectId"></param>
    /// <param name="abilityKey"></param>
    [Rpc(SendTo.NotOwner)]
    private void SpawnProjectileClientRpc(Vector3 targetDir, Vector3 targetPos, Vector3 position, ulong networkObjectId, string abilityKey) {
        if (!IsServer) {

            var ability = _playerController.Abilities[abilityKey];

            var newProjectile = ClientObjectPool.Instance.GetPooledObject(ability, AbilityPrefabType.Spawn);
            if (newProjectile == null) {
                Debug.Log("No available projectiles in the pool");
            }

            // Set the position and rotation of the projectile
            newProjectile.transform.position = position;
            newProjectile.transform.rotation = Quaternion.LookRotation(targetDir, Vector3.up);

            // Initialize projectile properties on the server
            var projectileScript = newProjectile.GetComponent<ClientAbilityBehaviour>();
            projectileScript.InitialiseProperties(ability, _playerController, targetPos, targetDir);
            projectileScript.ResetVFX();
            projectileScript.linkedNetworkObjectId = networkObjectId;
            ClientPrefabManager.Instance.RegisterPrefab(networkObjectId, newProjectile);

            // Activate the projectile
            newProjectile.SetActive(true);
        }
    }
    
    [Rpc(SendTo.Server)]
    public void RecastAbilityServerRpc(string abilityKey) {
        var networkProjectile = _networkActiveAbilityPrefabs[abilityKey];
        var networkProjectileScript = networkProjectile.GetComponent<NetworkAbilityBehaviour>();
        networkProjectileScript.isRecast = true;
    }

    /// <summary>
    /// Add ability cooldown on the server
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="abilityId"></param>
    /// <param name="maxCooldownSeconds"></param>
    [Rpc(SendTo.Server)]
    public void AddCooldownRpc(ulong clientId, int abilityId, double maxCooldownSeconds) {
        NetworkCooldownManager.Instance.StartCooldown(clientId, abilityId, maxCooldownSeconds);
    }
    
    /// <summary>
    /// Check ability cooldown on the server
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="abilityId"></param>
    [Rpc(SendTo.Server)]
    public void IsAbilityOnCooldownRpc(ulong clientId, int abilityId) {
        var cd = NetworkCooldownManager.Instance.IsAbilityOnCooldown(clientId, abilityId);
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

    /// <summary>
    /// Set the player's name on the server
    /// </summary>
    /// <param name="playerName"></param>
    /// <param name="sourceNetworkObject"></param>
    [Rpc(SendTo.Server)]
    public void SetPlayerNameServerRpc(string playerName, NetworkObjectReference sourceNetworkObject) {
        
        Debug.Log("Set Player Name On Server RPC");
        
        if (sourceNetworkObject.TryGet(out NetworkObject obj)) {
            var player = obj.gameObject;
            var playerScript = player.GetComponent<LuxPlayerController>();
            playerScript.playerName = playerName;
            SetPlayerNameClientRpc(playerName, sourceNetworkObject);
        }
    }

    /// <summary>
    /// Set player's name on clients
    /// </summary>
    /// <param name="playerName"></param>
    /// <param name="sourceNetworkObject"></param>
    [Rpc(SendTo.NotServer)]
    private void SetPlayerNameClientRpc(string playerName, NetworkObjectReference sourceNetworkObject) {
        
        Debug.Log("Set Player Name On Client RPC");
        
        if (sourceNetworkObject.TryGet(out NetworkObject obj)) {
            var player = obj.gameObject;
            var playerScript = player.GetComponent<LuxPlayerController>();
            playerScript.playerName = playerName;
            OnPlayerNameSet?.Invoke();
        }
    }

    [Rpc(SendTo.Server)]
    public void SendChatMessageServerRpc(string message, string playerName, NetworkObjectReference networkObjectRef) {
        var chatMessage = new ChatMessage(0, message, playerName);
        chatServer.AddMessage(chatMessage, networkObjectRef);
    }
}
