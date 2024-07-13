using Multiplayer;
using Unity.Netcode;
using UnityEngine;

public class RPCController : NetworkBehaviour {
    
    private LuxPlayerController _playerController;
    private GameObject _player;
    
    private void Start() {
        _playerController = GetComponent<LuxPlayerController>();
        _player = gameObject;
    }

    [Rpc(SendTo.Server)]
    public void SpawnProjectileServerRpc(Vector3 direction, Vector3 position, ulong clientId, int instanceId) {
        Debug.Log("Server Spawning Lux Q Missile");
        
        var newNetworkProjectile = ServerProjectilePool.Instance.GetPooledProjectile();
        if (newNetworkProjectile == null) {
            Debug.Log("No available projectiles in the pool");
            return;
        }
        
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
            Debug.Log("Added first entry to mappings, is mappings null?");
            Debug.Log( projectileScript.Mappings == null);
            Debug.Log( "Key: " + instanceId + ",Value: " + clientId);
        }
        else {
            Debug.Log("ProjectileAbility component is missing on the projectile");
        }
        
        SpawnProjectileClientRpc(direction, position);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SpawnProjectileClientRpc(Vector3 direction, Vector3 position) {
        if (!IsOwner && !IsServer) {
            Debug.Log("CLIENT Spawning Lux Q Missile");
        
            var newProjectile = ClientProjectilePool.Instance.GetPooledProjectile();
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
            }
            else {
                Debug.Log("ProjectileAbility component is missing on the projectile");
            }
            
            var playerNetworkBehaviour = _player.GetComponent<NetworkBehaviour>();
            var localClientId = playerNetworkBehaviour.NetworkManager.LocalClientId;
            
            UpdateMappingsServerRpc(localClientId, newProjectile.transform.GetInstanceID());
        }
    }

    [Rpc(SendTo.Server)]
    private void UpdateMappingsServerRpc(ulong clientId, int localProjectileId) {
        var networkProjectile = GameObject.FindWithTag("NetworkProjectile");
        var networkProjectileScript = networkProjectile.GetComponent<Lux_Q_Mis_Net>();
        networkProjectileScript.Mappings[localProjectileId] = clientId;
        Debug.Log("Added new mapping: Key: " + localProjectileId + ", Value: " + clientId);
    }
    
    public void Rpc(Vector3 direction, Vector3 position, ulong clientId) {
        Debug.Log("Spawning Lux Q Missile");
        
        var newProjectile = ClientProjectilePool.Instance.GetPooledProjectile();
        if (newProjectile == null) {
            Debug.Log("No available projectiles in the pool");
            return;
        }

        // Set the position and rotation of the projectile
        newProjectile.transform.position = position;
        newProjectile.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        // Activate the projectile
        newProjectile.SetActive(true);

        var networkObject = newProjectile.GetComponent<NetworkObject>();
        if (!networkObject.IsSpawned) networkObject.Spawn();

        // Initialize projectile properties on the server
        var projectileScript = newProjectile.GetComponent<ProjectileAbility>();
        if (projectileScript != null) {
            projectileScript.InitProjectileProperties(direction, _playerController.LuxQAbility,
                _playerController.projectiles, _playerController.playerType);
        }
        else {
            Debug.Log("ProjectileAbility component is missing on the projectile");
        }
    }
}
