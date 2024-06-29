using Multiplayer;
using Unity.Netcode;
using UnityEngine;

public class RPCController : NetworkBehaviour {
    private LuxPlayerController _playerController;

    private void Start() {
        _playerController = GetComponent<LuxPlayerController>();
    }

    [Rpc(SendTo.Server)]
    public void SpawnProjectileServerRpc(Vector3 direction, Vector3 position, ulong clientId) {
        Debug.Log("Spawning Lux Q Missile");
        Debug.Log("Spawn vals, dir: " + direction + ", pos" + position);

        
        var newProjectile = ProjectilePool.Instance.GetPooledProjectile();
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
        else
            Debug.Log("ProjectileAbility component is missing on the projectile");
    }
}
