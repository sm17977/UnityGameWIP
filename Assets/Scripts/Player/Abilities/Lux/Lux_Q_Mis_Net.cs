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
    
    // Projectile hitbox
    private GameObject _hitbox;
    
    private void Start() {
        
        // Set the projectile hitbox transform to move along the ground
        _hitbox = gameObject.transform.Find("Hitbox").gameObject;
        _hitbox.transform.position = new Vector3(_hitbox.transform.position.x, 0.5f, _hitbox.transform.position.z);
        _hitbox.transform.localScale = new Vector3(_hitbox.transform.localScale.x, 0.1f, _hitbox.transform.localScale.z);
        
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
        if(CanBeDestroyed) DestroyProjectile();
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

    protected override void HandleClientCollision(Vector3 position, GameObject player, Ability projectileAbility, GameObject projectile) {
        // Deactivate the projectile
        ClientObjectPool.Instance.ReturnObjectToPool(projectileAbility, AbilityPrefabType.Projectile, projectile);
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
        
        DestroyProjectile();
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