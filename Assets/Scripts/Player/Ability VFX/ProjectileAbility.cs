using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;

public delegate void OnRecast();

public class ProjectileAbility : MonoBehaviour {
    
    public event OnRecast Recast;
    
    public Vector3 projectileDirection;
    public float projectileSpeed;
    public float projectileRange;
    public float projectileLifetime;
    public float remainingDistance;
    public bool canBeDestroyed = false;  
    public Ability ability;
    public List<GameObject> projectiles;
    public PlayerType playerType;
    public bool hasHit;
    public bool isHit;
    public LuxPlayerController player;
    
   /// <summary>
   /// Set the direction, speed and range of a projectile and other ability data
   /// </summary>
   /// <param name="direction"></param>
   /// <param name="abilityData"></param>
   /// <param name="projectileList"></param>
   /// <param name="type"></param>
    public void InitProjectileProperties(Vector3 direction, Ability abilityData, List<GameObject> projectileList, PlayerType type, LuxPlayerController playerScript) {

        player = playerScript;
        projectileDirection = direction;
        projectileSpeed = abilityData.speed;
        projectileRange = abilityData.range;
        projectileLifetime = abilityData.GetProjectileLifetime();
        
        projectiles = projectileList;
        ability = abilityData;
        playerType = type;
        hasHit = false;
        remainingDistance = Mathf.Infinity;
        canBeDestroyed = false;
    }

    public virtual void ResetVFX() {}
   

    /// <summary>
    /// Moves a projectile transform towards target position
    /// </summary>
    /// <param name="transform"></param>
    /// <param name="initialPosition"></param>
    protected void MoveProjectile(Transform transform, Vector3 initialPosition){
        
        // The distance the projectile moves per frame
        float distance = Time.deltaTime * projectileSpeed;

        // The current remaining distance the projectile must travel to reach projectile range
        remainingDistance = (float)Math.Round(projectileRange - Vector3.Distance(transform.position, initialPosition), 2);

        // Ensures the projectile stops moving once remaining distance is zero 
        float travelDistance = Mathf.Min(distance, remainingDistance);

        // Move the projectile
        transform.Translate(projectileDirection * travelDistance, Space.World);
    }

    public void InvokeRecast() {
        Recast.Invoke();
    }
}