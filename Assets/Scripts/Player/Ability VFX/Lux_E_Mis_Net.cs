using System;
using System.Collections;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

public class Lux_E_Mis_Net : NetworkProjectileAbility {
    
    private Vector3 _initialPosition;
    
    private bool _canPlayRingVfx = true;
    private bool _canPlayExplosionVfx = true;
    private bool _canTestQuads = true;
    private float _currentLingeringLifetime;
    private float _totalLifetime;
    
    // Projectile hitbox
    private GameObject _hitbox;
    
    
    
    void Start(){
        
        NetworkCollision += OnNetworkCollision;
        NetworkRecast += OnNetworkRecast;
        
        _initialPosition = transform.position;
        _currentLingeringLifetime = 4;
        
        // Set the projectile hitbox transform to move along the ground
        _hitbox = gameObject.transform.Find("Hitbox").gameObject;
        _hitbox.transform.position = new Vector3(_hitbox.transform.position.x, 0.5f, _hitbox.transform.position.z);
        _hitbox.transform.localScale = new Vector3(_hitbox.transform.localScale.x, 0.1f, _hitbox.transform.localScale.z);
        
        _hitbox.SetActive(false);
        
    }
    
    private IEnumerator DelayBeforeDestroy(float delayInSeconds) {
        yield return new WaitForSeconds(delayInSeconds);
        if(canBeDestroyed) DestroyProjectile();
    }
    
    private void OnNetworkCollision(Vector3 position, GameObject player) {
            
    }
    
    void Update(){
        if(!IsServer) return;

        // Move object
        MoveProjectile(transform, _initialPosition);

        // Play ring and particles when the projectile reaches target and stops moving
        if(remainingDistance <= 0){
            _hitbox.SetActive(true);
            _currentLingeringLifetime -= Time.deltaTime;
            
            if(_currentLingeringLifetime <= -0.5){
   
                canBeDestroyed = true;
                StartCoroutine(DelayBeforeDestroy(1f));
            }
        }
    }

    private void OnNetworkRecast() {
        if (isColliding && ActiveCollision != null) {
            
            var collisionPos = ActiveCollision.gameObject.transform.position;
            var playerNetworkObject = ActiveCollision.gameObject.GetComponent<NetworkObject>();
            var enemyClientId = playerNetworkObject.OwnerClientId;
            
            var target = ActiveCollision.gameObject.GetComponent<LuxPlayerController>();
            target.health.TakeDamage(ability.damage);
            
            string jsonMappings = JsonConvert.SerializeObject(Mappings, Formatting.Indented);
            TriggerCollisionClientRpc(jsonMappings, collisionPos, playerNetworkObject.NetworkObjectId, ability.key);

            NetworkBuffManager.Instance.AddBuff(ability.buff, spawnedByClientId, enemyClientId);
            DestroyProjectile();
        }
    }
}
