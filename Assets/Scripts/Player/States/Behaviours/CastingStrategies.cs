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
        _rpcController.SpawnProjectileServerRpc(direction, abilitySpawnPos, _playerController.OwnerClientId);
    }
}
