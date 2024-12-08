using System;
using QFSW.QC;
using Unity.Netcode;
using UnityEngine;

public class RPCController : NetworkBehaviour {

    public event Action<bool> OnCooldownReceived;
    public static event Action<GameObject> NetworkSpawn;
    
    private LuxPlayerController _playerController;
    private GameObject _player;
    private GameObject _players;
    
    private void Start() {
        _playerController = GetComponent<LuxPlayerController>();
        _player = gameObject;
    }

    public override void OnNetworkSpawn() {
      
        Debug.Log("RPC OnNetWorkSpawn");
        gameObject.name = IsLocalPlayer ? "Local Player" : GetComponent<NetworkObject>().NetworkObjectId.ToString();
        _players = GameObject.Find("Players");
        

        if (IsServer) {
            transform.SetParent(_players.transform, true);
        }

        if (IsOwner) {
            NetworkSpawn?.Invoke(gameObject);
        }
    }

    [Rpc(SendTo.Server)]
    public void SpawnProjectileServerRpc(Vector3 direction, Vector3 position, ulong clientId, int instanceId, string abilityKey) {
        
        var newNetworkProjectile = ServerProjectilePool.Instance.GetPooledProjectile();
        if (newNetworkProjectile == null) {
            Debug.Log("No available projectiles in the pool");
            return;
        }

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
            Debug.Log("Init Projectile Server Ability - " + _playerController.Abilities[abilityKey].buff.ID);
            projectileScript.InitProjectileProperties(direction, _playerController.Abilities[abilityKey], _playerController.playerType, clientId);
            projectileScript.Mappings[instanceId] = clientId;
        }
        else {
            Debug.Log("Projectile Script is null");
        }
        SpawnProjectileClientRpc(direction, position, networkInstanceId, abilityKey);
    }

    [Rpc(SendTo.NotOwner)]
    private void SpawnProjectileClientRpc(Vector3 direction, Vector3 position, int networkInstanceId, string abilityKey) {
        if (!IsOwner && !IsServer) {
        
            var newProjectile = ClientProjectilePool.Instance.GetPooledObject(ProjectileType.Projectile);
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
                projectileScript.InitProjectileProperties(direction, _playerController.Abilities[abilityKey],
                    _playerController.projectiles, _playerController.playerType);
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

    [Rpc(SendTo.Server)]
    private void UpdateMappingsServerRpc(ulong clientId, int localProjectileId, int networkInstanceId) {
        var networkProjectile = GameObject.Find(networkInstanceId.ToString());
        var networkProjectileScript = networkProjectile.GetComponent<Lux_Q_Mis_Net>();
        networkProjectileScript.Mappings[localProjectileId] = clientId;
    }

    [Rpc(SendTo.Server)]
    public void AddCooldownRpc(ulong clientId, NetworkAbilityData abilityData) {
        NetworkCooldownManager.Instance.StartCooldown(clientId, abilityData);
    }

    [Rpc(SendTo.Server)]
    public void IsAbilityOnCooldownRpc(ulong clientId, NetworkAbilityData abilityData) {
        var cd = NetworkCooldownManager.Instance.IsAbilityOnCooldown(clientId, abilityData);
        UpdateCooldownRpc(cd);
    }

    [Rpc(SendTo.Owner)]
    private void UpdateCooldownRpc(bool serverCooldown) {
        if (IsLocalPlayer && IsOwner) {
            OnCooldownReceived?.Invoke(serverCooldown);
            OnCooldownReceived = null;
        }
    }

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

    [Rpc(SendTo.Server)]
    public void RequestMovementRpc(Vector3 targetPosition) {
        if (!_playerController.canMove) return;
        _playerController._stateManager.ChangeState(new MovingState(_playerController, targetPosition, _playerController.GetStoppingDistance(), gameObject, false));
    }
}
