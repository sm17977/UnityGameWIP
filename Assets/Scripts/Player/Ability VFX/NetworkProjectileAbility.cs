using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine.Serialization;

public abstract class NetworkProjectileAbility : NetworkBehaviour {
    
    public Vector3 initialPosition;
    public Vector3 projectileDirection;
    public float projectileSpeed;
    public float projectileRange;
    public float projectileLifetime;
    public float remainingDistance;
    public bool canBeDestroyed;
    public bool destructionScheduled;
    public Ability ability;
    public List<GameObject> projectiles;
    public PlayerType playerType;
    public bool hasHit;
    public bool isColliding;
    public bool isRecast;
    public Collision ActiveCollision;
    public Vector3 projectileTargetPosition;
    
    public Dictionary<int, ulong> Mappings;
    public ulong spawnedByClientId;
    
    private Transform _clientProjectilePool;
    private LuxPlayerController _target;
    
    protected abstract void HandleServerCollision(Collision collision);
    protected abstract void HandleClientCollision(Vector3 position, GameObject player, Ability ability, GameObject projectile);

    /// <summary>
    /// Set the direction, speed and range of a projectile and other ability data
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="targetPos"></param>
    /// <param name="abilityData"></param>
    /// <param name="type"></param>
    /// <param name="clientId"></param>
    public void InitProjectileProperties(Vector3 direction, Vector3 targetPos, Ability abilityData, PlayerType type, ulong clientId) {

        initialPosition = transform.position;
        projectileTargetPosition = targetPos;
        projectileDirection = direction;
        projectileSpeed = abilityData.speed;
        projectileRange = abilityData.range;
        projectileLifetime = abilityData.GetProjectileLifetime();
        
        ability = abilityData;
        
        playerType = type;
        hasHit = false;
        remainingDistance = Mathf.Infinity;
        canBeDestroyed = false;
        destructionScheduled = false;
        isRecast = false;
        
        Mappings = new Dictionary<int, ulong>();
        spawnedByClientId = clientId;
    }

    protected void OnCollisionEnter(Collision collision){
        if (!IsServer) return;
        if (!collision.gameObject.CompareTag("Player")) return;
        
        var playerNetworkObject = collision.gameObject.GetComponent<NetworkObject>();
        var enemyClientId = playerNetworkObject.OwnerClientId;
        var isDifferentPlayer = enemyClientId != spawnedByClientId;
        
        if(!(playerType == PlayerType.Player && isDifferentPlayer && !hasHit)) return;
        
        HandleServerCollision(collision);
    }
    
    protected void OnCollisionStay(Collision collision) {
        
        if (!IsServer) return;
        
        if (!collision.gameObject.CompareTag("Player")) {
            isColliding = false;
            return;
        }
        
        var playerNetworkObject = collision.gameObject.GetComponent<NetworkObject>();
        var enemyClientId = playerNetworkObject.OwnerClientId;
        var isDifferentPlayer = enemyClientId != spawnedByClientId;

        if (playerType == PlayerType.Player && isDifferentPlayer) {
            isColliding = true;
            ActiveCollision = collision;
        }
        else isColliding = false;
    }
    
    
    [Rpc(SendTo.ClientsAndHost)]
    protected void TriggerCollisionClientRpc(string jsonMappings, Vector3 position, ulong collisionNetworkObjectId, string abilityKey) {

        Debug.Log("Detected a collision!");
        
        // Get the projectile that collided
        var clientProjectile = GetClientCollidedProjectile(jsonMappings);
        if (clientProjectile == null) return;

        // Get player that was hit
        var player = GetHitPlayer(collisionNetworkObjectId);
        if(player == null) return;
        
        var playerScript = player.GetComponent<LuxPlayerController>();
        var projectileAbility = playerScript.Abilities[abilityKey];
        
        HandleClientCollision(position, player, projectileAbility, clientProjectile);
    }
    
    /// <summary>
    /// Get the client version of the projectile game object that collided with a player (on the server)
    /// </summary>
    /// <param name="jsonMappings">The JSON string mappings of projectile game objects to client IDs</param>
    /// <returns>The client projectile game object</returns>
    protected GameObject GetClientCollidedProjectile(string jsonMappings) {

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
    
    protected void DestroyProjectile() {
        Debug.Log("Destroying projectile on the server");
        ServerObjectPool.Instance.ReturnObjectToPool(ability, AbilityPrefabType.Projectile, gameObject);
        canBeDestroyed = false;
        destructionScheduled = false; 
    }
    
    protected virtual void MoveProjectile(){}
    
    public virtual void ReCast() {}
    
}