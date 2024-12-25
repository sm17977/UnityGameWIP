using UnityEngine;
using System;
using System.Collections.Generic;
using Multiplayer;
using Newtonsoft.Json;
using Unity.Netcode;

public delegate void OnNetworkCollision(Vector3 position, GameObject player);

public class NetworkProjectileAbility : NetworkBehaviour {
    
    public event OnNetworkCollision NetworkCollision;
    
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
    
    private Transform _clientProjectilePool;
    private LuxPlayerController _target;
    
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

    protected void OnCollisionEnter(Collision collision){
        if (!IsServer) return;
        if (!collision.gameObject.CompareTag("Player")) return;
        
        var collisionPos = collision.gameObject.transform.position;
        var playerNetworkObject = collision.gameObject.GetComponent<NetworkObject>();
        var enemyClientId = playerNetworkObject.OwnerClientId;
        var isDifferentPlayer = enemyClientId != spawnedByClientId;

        if (playerType == PlayerType.Player && isDifferentPlayer && !hasHit) {
            hasHit = true;
            _target = collision.gameObject.GetComponent<LuxPlayerController>();
            _target.health.TakeDamage(ability.damage);
            
            string jsonMappings = JsonConvert.SerializeObject(Mappings);
            TriggerCollisionClientRpc(jsonMappings, collisionPos, playerNetworkObject.NetworkObjectId, ability.key);
            NetworkBuffManager.Instance.AddBuff(ability.buff, spawnedByClientId, enemyClientId);
            DestroyProjectile();
        }
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerCollisionClientRpc(string jsonMappings, Vector3 position, ulong collisionNetworkObjectId, string abilityKey) {

        Debug.Log("Detected a collision!");
        
        // Get the projectile that collided
        var clientProjectile = GetClientCollidedProjectile(jsonMappings);
        if (clientProjectile == null) return;

        // Get player that was hit
        var player = GetHitPlayer(collisionNetworkObjectId);
        if(player == null) return;
        
        var playerScript = player.GetComponent<LuxPlayerController>();
        var projectileAbility = playerScript.Abilities[abilityKey];
        
        // Deactivate the projectile
        ClientObjectPool.Instance.ReturnObjectToPool(projectileAbility, AbilityPrefabType.Projectile, clientProjectile);
        
        NetworkCollision.Invoke(position, player);
  
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
    
    protected void DestroyProjectile() {
        ServerObjectPool.Instance.ReturnObjectToPool(ability, AbilityPrefabType.Projectile, gameObject);
        canBeDestroyed = false;
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