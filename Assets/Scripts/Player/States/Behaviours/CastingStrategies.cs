using Multiplayer;
using UnityEngine;

public class SinglePlayerCastingStrategy : ICastingStrategy {

    private readonly LuxPlayerController _playerController;

    public SinglePlayerCastingStrategy(LuxPlayerController playerController) {
        _playerController = playerController;
    }
    
    public void Cast(Ability ability, Vector3 direction, Vector3 abilitySpawnPos) {
        GameObject newProjectile = Object.Instantiate(ability.missile, abilitySpawnPos, Quaternion.LookRotation(direction, Vector3.up));
        _playerController.projectiles.Add(newProjectile);
        ProjectileAbility projectileScript = newProjectile.GetComponent<ProjectileAbility>();
        projectileScript?.InitProjectileProperties(direction, ability, _playerController.projectiles, _playerController.playerType);
    }
}

public class MultiplayerCastingStrategy : ICastingStrategy {
    
    private readonly RPCController _rpcController;
    private readonly LuxPlayerController _playerController; 
    
    public MultiplayerCastingStrategy(LuxPlayerController playerController, RPCController rpcController) {
        _playerController = playerController;
        _rpcController = rpcController;
    }
    
    public void Cast(Ability ability, Vector3 direction, Vector3 abilitySpawnPos) {
        
        Debug.Log("OWNER Spawning Lux Q Missile");
        
        var newProjectile = ClientProjectilePool.Instance.GetPooledProjectile();
        if (newProjectile == null) {
            Debug.Log("No available projectiles in the pool");
            return;
        }

        // Set the position and rotation of the projectile
        newProjectile.transform.position = abilitySpawnPos;
        newProjectile.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        // Activate the projectile
        newProjectile.SetActive(true);
            
        // Initialize projectile properties on the server
        var projectileScript = newProjectile.GetComponent<ProjectileAbility>();
        if (projectileScript != null) {
            projectileScript.SetClientId(_playerController.OwnerClientId);
            projectileScript.InitProjectileProperties(direction, _playerController.LuxQAbility,
                _playerController.projectiles, _playerController.playerType);
        }
        else {
            Debug.Log("ProjectileAbility component is missing on the projectile");
        }
        _rpcController.SpawnProjectileServerRpc(direction, abilitySpawnPos, _playerController.OwnerClientId, newProjectile.transform.GetInstanceID());
    }
}
