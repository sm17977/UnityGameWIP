using UnityEngine;
public abstract class ClientAbilityBehaviour : MonoBehaviour {
    
    public bool isRecast;
    public ulong linkedNetworkObjectId;
    
    protected Ability Ability;
    protected GameObject Hitbox;
    protected PlayerType PlayerType;
    
    protected Vector3 InitialPosition;
    protected Vector3 TargetPosition;
    protected Vector3 TargetDirection;
    
    protected float RemainingDistance;
    protected float LifeTime;
    
    protected bool CanBeDestroyed;  
    protected bool HasHit;
    
    protected LuxPlayerController Player;
    
    /// <summary>
    /// Initialise the base properties of an Ability
    /// </summary>
    /// <param name="ability"></param>
    /// <param name="player"></param>
    /// <param name="targetPosition"></param>
    /// <param name="targetDirection"></param>
    /// <param name="playerType"></param>
    public virtual void InitialiseProperties(Ability ability, LuxPlayerController player, Vector3 targetPosition, Vector3 targetDirection) {
        Player = player;
        Ability = ability;
        InitialPosition = transform.position;
        TargetPosition = targetPosition;
        TargetDirection = targetDirection;
        
        LifeTime = ability.GetLifetime();
        RemainingDistance = Mathf.Infinity;
        PlayerType = player.playerType;
        
        HasHit = false;
        CanBeDestroyed = false;
        isRecast = false;
    }
    
    /// <summary>
    /// Set the hitbox for the ability prefab
    /// </summary>
    protected virtual void SetHitbox() {
        Hitbox = gameObject.transform.Find("Hitbox").gameObject;
        if (gameObject.transform.Find("Hitbox").gameObject == null) {
            Debug.LogError("Missing hitbox on this prefab!");
        }
    }

    /// <summary>
    /// Return an ability prefab back to the client object pool
    /// </summary>
    /// <param name="type"></param>
    protected void DestroyAbilityPrefab(AbilityPrefabType type) {
        ClientPrefabManager.Instance.UnregisterPrefab(linkedNetworkObjectId);
        Player.ActiveAbilityPrefabs.Remove(Ability.key);
        ClientObjectPool.Instance.ReturnObjectToPool(Ability, type, gameObject);
        CanBeDestroyed = false;
    }
    
    protected virtual void Move(){}
    public virtual void Recast(){}
    public virtual void ResetVFX(){}

}
