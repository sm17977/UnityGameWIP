using UnityEngine;

public class SinglePlayerCastingStrategy : ICastingStrategy {
    public void Cast(Ability ability, Vector3 direction, Vector3 abilitySpawnPos, LuxPlayerController playerController) {
        GameObject newProjectile = Object.Instantiate(ability.missile, abilitySpawnPos, Quaternion.LookRotation(direction, Vector3.up));
        playerController.projectiles.Add(newProjectile);
        ProjectileAbility projectileScript = newProjectile.GetComponent<ProjectileAbility>();
        projectileScript?.InitProjectileProperties(direction, ability, playerController.projectiles, playerController.playerType);
    }
}

public class MultiplayerCastingStrategy : ICastingStrategy {
    public void Cast(Ability ability, Vector3 direction, Vector3 abilitySpawnPos, LuxPlayerController playerController) {
        playerController.SpawnProjectileServerRpc(direction, abilitySpawnPos, playerController.OwnerClientId);
    }
}
