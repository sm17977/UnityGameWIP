using System;
using System.Collections;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;

/*
Lux_Q_Mis_Net.cs

Controls the movement of the Lux Q missile (Networked version)
Sets VFX properties for Q_Orb and Q_Vortex_Trails
Component of: Lux_Q_Mis_Net prefab

*/

public class Lux_Q_Mis_Net : NetworkAbilityBehaviour {
    
    private GameObject _hitbox;
    
    private void Start() {
        
    }

    private void Update() {
        if(!IsServer) return;
        // Handle end of life
        if (RemainingDistance <= 0) {
            CanBeDestroyed = true;
            StartCoroutine(DelayBeforeDestroy(1f));
        }
        else {
            // Move object
            Move();
        }
    }
    private IEnumerator DelayBeforeDestroy(float delayInSeconds) {
        yield return new WaitForSeconds(delayInSeconds);
        if(CanBeDestroyed) DestroyAbilityPrefab();
    }
    
    /// <summary>
    /// Spawn the hit VFX on the client when a projectile collides with a player
    /// </summary>
    /// <param name="position">The position to spawn the hit VFX</param>
    /// <param name="player">The player that was hit</param>
    private void SpawnClientHitVFX(Vector3 position, GameObject player) {
        var playerScript = player.GetComponent<LuxPlayerController>();
        var hitAbility = playerScript.Abilities["Q"];
        var hitPrefab = ClientObjectPool.Instance.GetPooledObject(hitAbility, AbilityPrefabType.Hit);
        var hitScript = hitPrefab.GetComponent<Lux_Q_Hit>();
        
        hitScript.InitialiseProperties(hitAbility, playerScript, player, position);
        hitPrefab.SetActive(true);
        hitScript.ResetVFX();
    }

    protected override void HandleClientCollision(Vector3 position, GameObject player, Ability projectileAbility, GameObject prefab) {
        // Deactivate the projectile
        ClientObjectPool.Instance.ReturnObjectToPool(projectileAbility, AbilityPrefabType.Projectile, prefab);
        SpawnClientHitVFX(position, player);
    }
    
    protected override void HandleServerCollision(Collision collision) {
        
        var collisionPos = collision.gameObject.transform.position;
        var playerNetworkObject = collision.gameObject.GetComponent<NetworkObject>();
        var enemyClientId = playerNetworkObject.OwnerClientId;
        var target = collision.gameObject.GetComponent<LuxPlayerController>();
        
        HasHit = true;
        target.health.TakeDamage(Ability.damage);
        
        var jsonMappings = JsonConvert.SerializeObject(ServerAbilityMappings, Formatting.Indented);
        TriggerCollisionClientRpc(jsonMappings, collisionPos, playerNetworkObject.NetworkObjectId, Ability.key);
        
        NetworkBuffManager.Instance.AddBuff(Ability.buff, spawnedByClientId, enemyClientId);
        
        DestroyAbilityPrefab();
    }

    protected override void Move() {
        
        // The distance the projectile moves per frame
        float distance = Time.deltaTime * Ability.speed;

        // The current remaining distance the projectile must travel to reach projectile range
        RemainingDistance = (float)Math.Round(Ability.range - Vector3.Distance(transform.position, InitialPosition), 2);

        // Ensures the projectile stops moving once remaining distance is zero 
        float travelDistance = Mathf.Min(distance, RemainingDistance);

        // Move the projectile
        transform.Translate(TargetDirection * travelDistance, Space.World);
    }
}