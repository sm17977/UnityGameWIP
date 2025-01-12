using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Unity.Netcode;

public abstract class NetworkAbilityBehaviour : NetworkBehaviour {
    
    public ulong spawnedByClientId;
    public bool isRecast;
    
    protected Ability Ability;
    protected Collision ActiveCollision;
    
    protected Vector3 InitialPosition;
    protected Vector3 TargetDirection;
    protected Vector3 TargetPosition;

    protected float LingeringLifetime;
    protected float RemainingDistance;
    
    protected bool CanBeDestroyed;
    protected bool DestructionScheduled;
    protected bool HasHit;
    protected bool IsColliding;
    
    private Transform _clientObjectPoolParent;
    private LuxPlayerController _target;
    private PlayerType _playerType;
    
    protected virtual void HandleServerCollision(Collision collision){}
    
    /// <summary>
    /// Set the direction, speed and range of an ability prefab
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

        LingeringLifetime = ability.lingeringLifetime;
        RemainingDistance = Mathf.Infinity;
        _playerType = type;
      
        HasHit = false;
        CanBeDestroyed = false;
        isRecast = false;
        DestructionScheduled = false;
        IsColliding = false;
        
        spawnedByClientId = clientId;
    }

    protected void OnCollisionEnter(Collision collision){
        if (!IsServer || !collision.gameObject.CompareTag("Player")) return;
        
        var playerNetworkObject = collision.gameObject.GetComponent<NetworkObject>();
        var enemyClientId = playerNetworkObject.OwnerClientId;
        var isDifferentPlayer = enemyClientId != spawnedByClientId;
        
        if(!(_playerType == PlayerType.Player && isDifferentPlayer && !HasHit)) return;
        
        HandleServerCollision(collision);
    }
    
    protected void OnCollisionStay(Collision collision) {
        if (!IsServer || !collision.gameObject.CompareTag("Player")) return;
        
        var playerNetworkObject = collision.gameObject.GetComponent<NetworkObject>();
        var enemyClientId = playerNetworkObject.OwnerClientId;
        var isDifferentPlayer = enemyClientId != spawnedByClientId;

        if (_playerType == PlayerType.Player && isDifferentPlayer && !HasHit) {
            IsColliding = true;
            ActiveCollision = collision;
        }
        else IsColliding = false;
    }
    
    /// <summary>
    /// Get the client version of the prefab game object
    /// </summary>
    /// <param name="networkObjectId"></param>
    /// <returns>The client prefab game object</returns>
    protected GameObject GetClientPrefab(ulong networkObjectId) {
        return ClientPrefabManager.Instance.GetLocalPrefab(networkObjectId);
    }
    
    /// <summary>
    /// Get the game object of the player hit by the prefab
    /// </summary>
    /// <param name="networkObjectId">The network object ID of the player</param>
    /// <returns>The player game object</returns>
    protected GameObject GetHitPlayer(ulong networkObjectId) {

        GameObject player = null;
        
        if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var playerNetworkObj)) {
            player = playerNetworkObj.gameObject;
        }
        return player;
    }
    
    protected void DestroyAbilityPrefab() {
        ServerObjectPool.Instance.ReturnObjectToPool(Ability, AbilityPrefabType.Projectile, gameObject);
        CanBeDestroyed = false;
        DestructionScheduled = false; 
    }
    
    protected virtual void Move(){}
    public virtual void Recast(){}
}