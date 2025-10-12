using UnityEngine;


// Area of Effect abilities spawn at a target position
public class AoeCastBehaviour : ICastBehaviour {
    public void Cast(Ability ability, CastContext ctx, LuxPlayerController playerController) {
        var newProjectile = ClientObjectPool.Instance.GetPooledObject(ability, AbilityPrefabType.Spawn);
        if (newProjectile == null) {
            Debug.Log("No available projectiles in the pool");
            return;
        }
        
        // Store prefab in a dictionary in case we need to access it later
        playerController.ActiveAbilityPrefabs[ability.key] = newProjectile;

        // Set the position and rotation of the projectile
        newProjectile.transform.position = ctx.SpawnPos;
        // Quaternion lookRotation = Quaternion.LookRotation(ctx.Direction, Vector3.up);
        // newProjectile.transform.rotation = Quaternion.Euler(0, lookRotation.eulerAngles.y - 90, 0);
        newProjectile.transform.rotation = Quaternion.LookRotation(ctx.Direction, Vector3.up);


        // Activate the projectile
        newProjectile.SetActive(true);
            
        // Initialize projectile properties
        var projectileScript = newProjectile.GetComponent<ClientAbilityBehaviour>();
        if (projectileScript != null) {
            projectileScript.InitialiseProperties(ability, playerController,ctx.TargetPos, ctx.Direction);
            projectileScript.ResetVFX();
        }
        else Debug.Log("ProjectileAbility component is missing on the projectile");
    }

    public void Recast(Ability data, GameObject activeObject) {}
}
