using UnityEngine;
using System;
using System.Collections.Generic;

public class ProjectileAbility : MonoBehaviour
{

    public Vector3 projectileDirection;
    public float projectileSpeed;
    public float projectileRange;
    public float projectileLifetime;
    public float remainingDistance = Mathf.Infinity;
    public bool canBeDestroyed = false;  
    public Ability abilityData;
    public List<GameObject> projectiles;
    public bool isCastFromPlayer;

   /// <summary>
   /// Set the direction, speed and range of a projectile and other ability data
   /// </summary>
   /// <param name="direction"></param>
   /// <param name="ability"></param>
   /// <param name="playerProjectiles"></param>
   /// <param name="castFromPlayer"></param>
    public void InitProjectileProperties(Vector3 direction, Ability ability, List<GameObject> playerProjectiles, bool castFromPlayer){

        projectileDirection = direction;
        projectileSpeed = ability.speed;
        projectileRange = ability.range;
        projectileLifetime = ability.GetProjectileLifetime();
        projectiles = playerProjectiles;

        abilityData = ability;
        isCastFromPlayer = castFromPlayer;

    }

    /// <summary>
    /// Moves a projectile transform towards target position
    /// </summary>
    /// <param name="transform"></param>
    /// <param name="initialPosition"></param>
    public void MoveProjectile(Transform transform, Vector3 initialPosition){
        // The distance the projectile moves per frame
        float distance = Time.deltaTime * projectileSpeed;

        // The current remaining distance the projectile must travel to reach projectile range
        remainingDistance = (float)Math.Round(projectileRange - Vector3.Distance(transform.position, initialPosition), 2);

        // Ensures the projectile stops moving once remaining distance is zero 
        float travelDistance = Mathf.Min(distance, remainingDistance);

        // Move the projectile
        transform.Translate(projectileDirection * travelDistance, Space.World);
    }
}