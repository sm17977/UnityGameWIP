using System.Collections;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;


public class Lux_E_Mis_Net : NetworkProjectileAbility {
   
    private float _currentLingeringLifetime;
    private float _totalLifetime;
    
    // Projectile hitbox
    private GameObject _hitbox;
    
    void Start(){
        
        _currentLingeringLifetime = 4;
        
        // Set the projectile hitbox transform to move along the ground
        _hitbox = gameObject.transform.Find("Hitbox").gameObject;
        _hitbox.transform.position = new Vector3(_hitbox.transform.position.x, 0.5f, _hitbox.transform.position.z);
        _hitbox.transform.localScale = new Vector3(_hitbox.transform.localScale.x, 0.1f, _hitbox.transform.localScale.z);
    }
    
    private void Update(){
        if(!IsServer) return;
        
        // Play ring and particles when the projectile reaches target and stops moving
        if(remainingDistance <= 0){
            if (isRecast) {
                ReCast();
                isRecast = false;
                return;
            }
            _currentLingeringLifetime -= Time.deltaTime;
            
            if(_currentLingeringLifetime <= -0.5){
                Debug.Log("Update Destroy");
                canBeDestroyed = true;
                ScheduleDestroy(1);
                _currentLingeringLifetime = 4;
            }
        }
        else {
            // Move object
            MoveProjectile(transform, initialPosition);
        }
    }
    
    private IEnumerator DelayBeforeDestroy(float delayInSeconds) {
        yield return new WaitForSeconds(delayInSeconds);
        if(canBeDestroyed) DestroyProjectile();
    }
    
    private void ScheduleDestroy(float delay) {
        if (!gameObject.activeInHierarchy) return;
        if (destructionScheduled) return;
        destructionScheduled = true;
        StartCoroutine(DelayBeforeDestroy(delay));
    }

    public override void ReCast() {
        
        var jsonMappings = JsonConvert.SerializeObject(Mappings, Formatting.Indented);
        
        Debug.Log("ReCast Lux E Net");
        Debug.Log("isColliding: " + isColliding);
        
        if (ActiveCollision != null && isColliding) {
            Debug.Log("ReCast valid collision");
            var collisionPos = ActiveCollision.gameObject.transform.position;
            var playerNetworkObject = ActiveCollision.gameObject.GetComponent<NetworkObject>();
            var enemyClientId = playerNetworkObject.OwnerClientId;
            var target = ActiveCollision.gameObject.GetComponent<LuxPlayerController>();
            
            target.health.TakeDamage(ability.damage);
            NetworkBuffManager.Instance.AddBuff(ability.buff, spawnedByClientId, enemyClientId);
            TriggerCollisionClientRpc(jsonMappings, collisionPos, playerNetworkObject.NetworkObjectId, ability.key);
        }
        else {
            Debug.Log("ReCast non collision");
            DetonateClientRpc(jsonMappings);
        }

        Debug.Log("ReCast Destroy");
        canBeDestroyed = true;
        ScheduleDestroy(1);
        isColliding = false;
    }

    protected override void HandleServerCollision(Collision collision) {
        
    }
    
    protected override void HandleClientCollision(Vector3 position, GameObject player, Ability projectileAbility, GameObject projectile) {
        
        if(IsOwner) return;
        
        var projectileScript = projectile.GetComponent<Lux_E_Mis>();
        projectileScript.isRecast = true;
    }

    [Rpc(SendTo.NotOwner)]
    private void DetonateClientRpc(string mappings) {
        // Get the projectile that collided
        var clientProjectile = GetClientCollidedProjectile(mappings);
        if (clientProjectile == null) return;

        var clientProjectileScript = clientProjectile.GetComponent<Lux_E_Mis>();
        clientProjectileScript.isRecast = true;
    }
}
