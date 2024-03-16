using UnityEngine;
using System;

public class ProjectileAbility : MonoBehaviour
{

    public Vector3 projectileDirection;
    public float projectileSpeed;
    public float projectileRange;
    public float remainingDistance = Mathf.Infinity;
    public bool canBeDestroyed = false;  

   /// <summary>
   /// Sets the direction, speed and range of a projectile
   /// </summary>
   /// <param name="direction"></param>
   /// <param name="speed"></param>
   /// <param name="range"></param>
    public void InitProjectileProperties(Vector3 direction, float speed, float range){
        projectileDirection = direction;
        projectileSpeed = speed;
        projectileRange = range;
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