using System;
using Multiplayer;
using QFSW.QC;
using Unity.Netcode;
using UnityEngine;

public class RPCController : NetworkBehaviour {
    
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
        if (IsServer) {
            _players = GameObject.Find("Players");
            transform.SetParent(_players.transform);
        }
    }

    [Rpc(SendTo.Server)]
    public void SpawnProjectileServerRpc(Vector3 direction, Vector3 position, ulong clientId, int instanceId) {
        
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
            projectileScript.InitProjectileProperties(direction, _playerController.LuxQAbility, _playerController.playerType, clientId);
            projectileScript.Mappings[instanceId] = clientId;
        }
        SpawnProjectileClientRpc(direction, position, networkInstanceId);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SpawnProjectileClientRpc(Vector3 direction, Vector3 position, int networkInstanceId) {
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
                projectileScript.InitProjectileProperties(direction, _playerController.LuxQAbility,
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
}
