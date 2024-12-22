using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;

/*
Lux_Q_Mis.cs

Controls the movement of the Lux Q missile (Networked version)
Sets VFX properties for Q_Orb and Q_Vortex_Trails
Component of: Lux_Q_Mis_Net prefab

*/

public class Lux_Q_Mis_Net : NetworkProjectileAbility {
    
    private Vector3 _initialPosition; 

    // Projectile hitbox
    private GameObject _hitbox;
    private Transform _clientProjectilePool;
    
    // Collision
    private LuxPlayerController _target;

    private void Start() {
        // Store projectile start position in order to calculate remaining distance
        _initialPosition = transform.position;

        // Set the projectile hitbox transform to move along the ground
        _hitbox = gameObject.transform.Find("Hitbox").gameObject;
        _hitbox.transform.position = new Vector3(_hitbox.transform.position.x, 0.5f, _hitbox.transform.position.z);
        _hitbox.transform.localScale = new Vector3(_hitbox.transform.localScale.x, 0.1f, _hitbox.transform.localScale.z);
    }

    private void Update() {
        if(!IsServer) return;
        // Handle end of life
        if (remainingDistance <= 0) {
            canBeDestroyed = true;
            StartCoroutine(DelayBeforeDestroy(1f));
        }
        else {
            // Move object
            MoveProjectile(transform, _initialPosition);
        }
    }
    private IEnumerator DelayBeforeDestroy(float delayInSeconds) {
        yield return new WaitForSeconds(delayInSeconds);
        if(canBeDestroyed) DestroyProjectile();
    }

    // Detect projectile hitbox collision with enemy 
    private void OnCollisionEnter(Collision collision) {
        if (!IsServer) return;
        if (!collision.gameObject.CompareTag("Player")) return;

        Debug.Log("Collision with Player: " + collision.gameObject.name);

        var collisionPos = collision.gameObject.transform.position;
        var playerNetworkObject = collision.gameObject.GetComponent<NetworkObject>();
        var enemyClientId = playerNetworkObject.OwnerClientId;
        var isDifferentPlayer = enemyClientId != spawnedByClientId;
        
        Debug.Log("Source ID: " + spawnedByClientId + ", Enemy ID: " + enemyClientId + ", Different Player?: " +
                  isDifferentPlayer + ", hasHit: " + hasHit);
        
        if (playerType == PlayerType.Player && isDifferentPlayer && !hasHit) {
            Debug.Log("Hit enemy!");
            hasHit = true;
            _target = collision.gameObject.GetComponent<LuxPlayerController>();
            _target.health.TakeDamage(ability.damage);
            
            string jsonMappings = JsonConvert.SerializeObject(Mappings);
            TriggerCollisionClientRpc(jsonMappings, collisionPos, playerNetworkObject.NetworkObjectId);
            NetworkBuffManager.Instance.AddBuff(ability.buff, spawnedByClientId, enemyClientId);
            DestroyProjectile();
        }
    }
    
    private void DestroyProjectile() {
        ServerProjectilePool.Instance.ReturnObjectToPool(ability, AbilityPrefabType.Projectile, gameObject);
        canBeDestroyed = false;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerCollisionClientRpc(string jsonMappings, Vector3 position, ulong collisionNetworkObjectId) {

        // Get the projectile that collided
        var clientProjectile = GetClientCollidedProjectile(jsonMappings);
        if (clientProjectile == null) return;

        // Get player that was hit
        var player = GetHitPlayer(collisionNetworkObjectId);
        if(player == null) return;
        
        var playerScript = player.GetComponent<LuxPlayerController>();
        var projectileAbility = playerScript.Abilities["Q"];
        
        // Deactivate the projectile
        ClientObjectPool.Instance.ReturnObjectToPool(projectileAbility, AbilityPrefabType.Projectile, clientProjectile);
        
        // Spawn the hit VFX
        SpawnClientHitVFX(position, player);
    }
    
    /// <summary>
    /// Get the client version of the projectile game object that collided with a player (on the server)
    /// </summary>
    /// <param name="jsonMappings">The JSON string mappings of projectile game objects to client IDs</param>
    /// <returns>The client projectile game object</returns>
    private GameObject GetClientCollidedProjectile(string jsonMappings) {

        GameObject localProjectile = null;
        
        // First find the parent game object that holds all the projectiles
        _clientProjectilePool = GameObject.Find("Client Object Pool").transform;
        
        // Deserialize the json mappings into a dictionary
        Dictionary<int, ulong> mappings = JsonConvert.DeserializeObject<Dictionary<int, ulong>>(jsonMappings);

        // Iterate over mappings to find the correct projectile for this client
        foreach (var key in mappings.Keys) {
            if (NetworkManager.LocalClientId == mappings[key]) {
                localProjectile = _clientProjectilePool?.Find(key.ToString())?.gameObject;
                break;
            }
        }
        return localProjectile;
    }
    
    /// <summary>
    /// Get the game object of the player hit by the projectile
    /// </summary>
    /// <param name="networkObjectId">The network object ID of the player</param>
    /// <returns>The player game object</returns>
    private GameObject GetHitPlayer(ulong networkObjectId) {

        GameObject player;
        
        // If the local client has been hit
        if (NetworkManager.LocalClient.PlayerObject.NetworkObjectId == networkObjectId) {
            player = NetworkManager.LocalClient.PlayerObject.gameObject;
        }
        // If a different client has been hit
        else {
            var players = GameObject.Find("Players").transform;
            player = players.Find(networkObjectId.ToString()).gameObject;
        }
        return player;
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
        
        // We have to do this because we don't call InitProjectileProperties on the hit script
        // TODO: Add function specifically to init hit VFX scripts?
        hitScript.SetAbility(hitAbility);
        hitScript.target = player;
        hitPrefab.transform.position = position;
        hitPrefab.SetActive(true);
        hitScript.ResetVFX();
    }
}