using UnityEngine;
using System;
using System.Collections.Generic;
using Multiplayer;
using Unity.Netcode;

public class NetworkProjectileAbility : NetworkBehaviour {
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
    
    public Dictionary<int, ulong> Mappings;
    public ulong spawnedByClientId;
    
   /// <summary>
   /// Set the direction, speed and range of a projectile and other ability data
   /// </summary>
   /// <param name="direction"></param>
   /// <param name="abilityData"></param>
   /// <param name="type"></param>
   /// <param name="clientId"></param>
    public void InitProjectileProperties(Vector3 direction, Ability abilityData, PlayerType type, ulong clientId){

        projectileDirection = direction;
        projectileSpeed = abilityData.speed;
        projectileRange = abilityData.range;
        projectileLifetime = abilityData.GetProjectileLifetime();
        
        ability = abilityData;
        
        playerType = type;
        hasHit = false;
        remainingDistance = Mathf.Infinity;
        canBeDestroyed = false;
        
        Mappings = new Dictionary<int, ulong>();
        spawnedByClientId = clientId;
    }

    /// <summary>
    /// Moves a projectile transform towards target position
    /// </summary>
    /// <param name="missileTransform"></param>
    /// <param name="initialPosition"></param>
    protected void MoveProjectile(Transform missileTransform, Vector3 initialPosition){
        
        // The distance the projectile moves per frame
        float distance = Time.deltaTime * projectileSpeed;

        // The current remaining distance the projectile must travel to reach projectile range
        remainingDistance = (float)Math.Round(projectileRange - Vector3.Distance(missileTransform.position, initialPosition), 2);

        // Ensures the projectile stops moving once remaining distance is zero 
        float travelDistance = Mathf.Min(distance, remainingDistance);

        // Move the projectile
        missileTransform.Translate(projectileDirection * travelDistance, Space.World);
    }
}