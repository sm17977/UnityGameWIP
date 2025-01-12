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

    private const float DestroyDelay = 1f;
    
    private void Update() {
        if(!IsServer) return;
        // Handle end of life
        if (RemainingDistance <= 0) {
            CanBeDestroyed = true;
            StartCoroutine(DelayBeforeDestroy(DestroyDelay));
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
    
    protected override void HandleServerCollision(Collision collision) {

        var prefabNetworkObjectId = GetComponent<NetworkObject>().NetworkObjectId;
        var collisionPos = collision.gameObject.transform.position;
        var playerNetworkObject = collision.gameObject.GetComponent<NetworkObject>();
        var enemyClientId = playerNetworkObject.OwnerClientId;
        var target = collision.gameObject.GetComponent<LuxPlayerController>();
        
        HasHit = true;
        target.health.TakeDamage(Ability.damage);
        NetworkBuffManager.Instance.AddBuff(Ability.buff, spawnedByClientId, enemyClientId);
        
        TriggerCollisionClientRpc(prefabNetworkObjectId, collisionPos, playerNetworkObject.NetworkObjectId, Ability.key);
        DestroyAbilityPrefab();
    }
    
     
    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerCollisionClientRpc(ulong prefabNetworkObjectId, Vector3 position, ulong collisionNetworkObjectId, string abilityKey) {
        
        // Get the prefab that collided
        var clientPrefab = GetClientPrefab(prefabNetworkObjectId);
        if (clientPrefab == null) return;

        // Get player that was hit
        var player = GetHitPlayer(collisionNetworkObjectId);
        if(player == null) return;
        
        var playerScript = player.GetComponent<LuxPlayerController>();
        var ability = playerScript.Abilities[abilityKey];
        
        // Deactivate the projectile
        ClientObjectPool.Instance.ReturnObjectToPool(ability, AbilityPrefabType.Projectile, clientPrefab);
        ClientPrefabManager.Instance.UnregisterPrefab(prefabNetworkObjectId);
        
        // Spawn the on hit VFX on all clients
        SpawnClientHitVFX(position, player);
    }

    protected override void Move() {
        
        // Move ability to max range
        
        float distance = Time.deltaTime * Ability.speed;
        RemainingDistance = (float)Math.Round(Ability.range - Vector3.Distance(transform.position, InitialPosition), 2);
        float travelDistance = Mathf.Min(distance, RemainingDistance);
        transform.Translate(TargetDirection * travelDistance, Space.World);
    }
}