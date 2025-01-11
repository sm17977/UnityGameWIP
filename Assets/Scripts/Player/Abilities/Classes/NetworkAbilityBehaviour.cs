using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using Unity.Netcode;

public abstract class NetworkAbilityBehaviour : NetworkBehaviour {
    
    public Dictionary<int, ulong> ServerAbilityMappings; // Maps prefab IDs to client IDs
    public ulong spawnedByClientId;
    public bool isRecast;
    
    protected Ability Ability;
    protected Collision ActiveCollision;
    
    protected Vector3 InitialPosition;
    protected Vector3 TargetDirection;
    protected Vector3 TargetPosition;

    protected float LifeTime;
    protected float RemainingDistance;
    
    protected bool CanBeDestroyed;
    protected bool DestructionScheduled;
    protected bool HasHit;
    protected bool IsColliding;
    
    private Transform _clientProjectilePool;
    private LuxPlayerController _target;
    private PlayerType _playerType;
    
    protected abstract void HandleServerCollision(Collision collision);
    protected abstract void HandleClientCollision(Vector3 position, GameObject player, Ability ability, GameObject projectile);

    /// <summary>
    /// Set the direction, speed and range of a projectile and other ability data
    /// </summary>
    /// <param name="targetDirection"></param>
    /// <param name="targetPosition"></param>
    /// <param name="ability"></param>
    /// <param name="type"></param>
    /// <param name="clientId"></param>
    public void InitialiseProperties(Ability ability, Vector3 targetDirection, Vector3 targetPosition, PlayerType type, ulong clientId) {

        Ability = ability;
        InitialPosition = transform.position;
        TargetPosition = targetPosition;
        TargetDirection = targetDirection;
        
        LifeTime = ability.GetLifetime();
        RemainingDistance = Mathf.Infinity;
        _playerType = type;
      
        HasHit = false;
        CanBeDestroyed = false;
        isRecast = false;
        DestructionScheduled = false;
        
        ServerAbilityMappings = new Dictionary<int, ulong>();
        spawnedByClientId = clientId;
    }

    protected void OnCollisionEnter(Collision collision){
        if (!IsServer) return;
        if (!collision.gameObject.CompareTag("Player")) return;
        
        var playerNetworkObject = collision.gameObject.GetComponent<NetworkObject>();
        var enemyClientId = playerNetworkObject.OwnerClientId;
        var isDifferentPlayer = enemyClientId != spawnedByClientId;
        
        if(!(_playerType == PlayerType.Player && isDifferentPlayer && !HasHit)) return;
        
        HandleServerCollision(collision);
    }
    
    protected void OnCollisionStay(Collision collision) {
        
        if (!IsServer) return;
        
        if (!collision.gameObject.CompareTag("Player")) {
            IsColliding = false;
            return;
        }
        
        var playerNetworkObject = collision.gameObject.GetComponent<NetworkObject>();
        var enemyClientId = playerNetworkObject.OwnerClientId;
        var isDifferentPlayer = enemyClientId != spawnedByClientId;

        if (_playerType == PlayerType.Player && isDifferentPlayer) {
            IsColliding = true;
            ActiveCollision = collision;
        }
        else IsColliding = false;
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
        ServerObjectPool.Instance.ReturnObjectToPool(Ability, AbilityPrefabType.Projectile, gameObject);
        CanBeDestroyed = false;
        DestructionScheduled = false; 
    }
    
    protected virtual void Move(){}
    public virtual void Recast(){}
}