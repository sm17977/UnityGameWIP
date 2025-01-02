using System;
using System.Collections;
using Mono.CSharp;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

public class Lux_E_Mis_Net : NetworkProjectileAbility {
    
    private Vector3 _initialPosition;
    private float _currentLingeringLifetime;
    private float _totalLifetime;
    
    // Projectile hitbox
    private GameObject _hitbox;
    
    void Start(){
        
        _initialPosition = transform.position;
        _currentLingeringLifetime = 4;
        
        // Set the projectile hitbox transform to move along the ground
        _hitbox = gameObject.transform.Find("Hitbox").gameObject;
        _hitbox.transform.position = new Vector3(_hitbox.transform.position.x, 0.5f, _hitbox.transform.position.z);
        _hitbox.transform.localScale = new Vector3(_hitbox.transform.localScale.x, 0.1f, _hitbox.transform.localScale.z);
    }
    
    private IEnumerator DelayBeforeDestroy(float delayInSeconds) {
        yield return new WaitForSeconds(delayInSeconds);
        if(canBeDestroyed) DestroyProjectile();
    }
    
    private void Update(){
        if(!IsServer) return;

        // Move object
        MoveProjectile(transform, _initialPosition);

        // Play ring and particles when the projectile reaches target and stops moving
        if(remainingDistance <= 0){
            _currentLingeringLifetime -= Time.deltaTime;
            
            if(_currentLingeringLifetime <= -0.5){
   
                canBeDestroyed = true;
                StartCoroutine(DelayBeforeDestroy(1f));
            }
        }
    }

    public override void ReCast() {
        
        var collisionPos = ActiveCollision.gameObject.transform.position;
        var playerNetworkObject = ActiveCollision.gameObject.GetComponent<NetworkObject>();
        var enemyClientId = playerNetworkObject.OwnerClientId;

        if (ActiveCollision != null && isColliding) {
            var target = ActiveCollision.gameObject.GetComponent<LuxPlayerController>();
            target.health.TakeDamage(ability.damage);
            NetworkBuffManager.Instance.AddBuff(ability.buff, spawnedByClientId, enemyClientId);
        }

        string jsonMappings = JsonConvert.SerializeObject(Mappings, Formatting.Indented);
        TriggerCollisionClientRpc(jsonMappings, collisionPos, playerNetworkObject.NetworkObjectId, ability.key);
        DestroyProjectile();
    }

    protected override void HandleServerCollision(Collision collision) {
        
    }
    
    protected override void HandleClientCollision(Vector3 position, GameObject player, Ability projectileAbility, GameObject projectile) {
    
        var projectileScript = projectile.GetComponent<Lux_E_Mis>();
        projectileScript.ReCast();
    }
}
