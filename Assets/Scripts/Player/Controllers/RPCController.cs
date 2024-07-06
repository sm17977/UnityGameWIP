using Multiplayer;
using Unity.Netcode;
using UnityEngine;

public class RPCController : NetworkBehaviour {
    private LuxPlayerController _playerController;

    private void Start() {
        _playerController = GetComponent<LuxPlayerController>();
    }

    [Rpc(SendTo.Server)]
    public void SpawnProjectileServerRpc(Vector3 direction, Vector3 position, ulong clientId, int instanceId) {
        Debug.Log("Server Spawning Lux Q Missile");
        
        var newProjectile = ServerProjectilePool.Instance.GetPooledProjectile();
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
        if (!networkObject.IsSpawned) networkObject.Spawn(true);
            
        // Initialize projectile properties on the server
        var projectileScript = newProjectile.GetComponent<ProjectileAbility>();
        if (projectileScript != null) {
            projectileScript.SetClientId(_playerController.OwnerClientId);
            projectileScript.instanceId = instanceId;
            projectileScript.InitProjectileProperties(direction, _playerController.LuxQAbility,
                _playerController.projectiles, _playerController.playerType);
        }
        else {
            Debug.Log("ProjectileAbility component is missing on the projectile");
        }
        SpawnProjectileClientRpc(direction, position, clientId);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void SpawnProjectileClientRpc(Vector3 direction, Vector3 position, ulong clientId) {
        if (!IsOwner && !IsServer) {
            Debug.Log("CLIENT Spawning Lux Q Missile");
        
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
            
            // Initialize projectile properties on the server
            var projectileScript = newProjectile.GetComponent<ProjectileAbility>();
            if (projectileScript != null) {
                projectileScript.SetClientId(clientId);
                projectileScript.InitProjectileProperties(direction, _playerController.LuxQAbility,
                    _playerController.projectiles, _playerController.playerType);
            }
            else {
                Debug.Log("ProjectileAbility component is missing on the projectile");
            }
        }
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
            projectileScript.SetClientId(clientId);
            projectileScript.InitProjectileProperties(direction, _playerController.LuxQAbility,
                _playerController.projectiles, _playerController.playerType);
        }
        else {
            Debug.Log("ProjectileAbility component is missing on the projectile");
        }
    }
    
    
}
