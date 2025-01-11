using System;
using System.Collections;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;

/*
Lux_E_Mis_Net.cs

Controls the movement of the Lux E AOE ability (Networked version)
Component of: Lux_E_Mis_Net prefab

*/

public class Lux_E_Mis_Net : NetworkAbilityBehaviour {
   
    private float _currentLingeringLifetime;
    private float _totalLifetime;
    
    // Projectile hitbox
    private GameObject _hitbox;
    
    private void Start(){
        
        _currentLingeringLifetime = 4;
        
        // Set the projectile hitbox transform to move along the ground
        _hitbox = gameObject.transform.Find("Hitbox").gameObject;
        _hitbox.transform.position = new Vector3(_hitbox.transform.position.x, 0.5f, _hitbox.transform.position.z);
        _hitbox.transform.localScale = new Vector3(_hitbox.transform.localScale.x, 0.1f, _hitbox.transform.localScale.z);
    }
    
    private void Update(){
        if(!IsServer) return;
        
        // Play ring and particles when the projectile reaches target and stops moving
        if(RemainingDistance <= 0){
            if (isRecast) {
                Recast();
                isRecast = false;
                return;
            }
            _currentLingeringLifetime -= Time.deltaTime;
            
            if(_currentLingeringLifetime <= -0.5){
                Detonate();
                Debug.Log("Update Destroy");
                CanBeDestroyed = true;
                ScheduleDestroy(1);
                _currentLingeringLifetime = 4;
            }
        }
        else {
            // Move object
            Move();
        }
    }
    
    private IEnumerator DelayBeforeDestroy(float delayInSeconds) {
        yield return new WaitForSeconds(delayInSeconds);
        if(CanBeDestroyed) DestroyProjectile();
    }
    
    private void ScheduleDestroy(float delay) {
        if (!gameObject.activeInHierarchy) return;
        if (DestructionScheduled) return;
        DestructionScheduled = true;
        StartCoroutine(DelayBeforeDestroy(delay));
    }

    private void Detonate() {
        
        Debug.Log("ReCast Lux E Net");
        Debug.Log("isColliding: " + IsColliding);
        
        if (ActiveCollision != null && IsColliding) {
            Debug.Log("ReCast valid collision");
            var playerNetworkObject = ActiveCollision.gameObject.GetComponent<NetworkObject>();
            var enemyClientId = playerNetworkObject.OwnerClientId;
            var target = ActiveCollision.gameObject.GetComponent<LuxPlayerController>();
            
            target.health.TakeDamage(Ability.damage);
            NetworkBuffManager.Instance.AddBuff(Ability.buff, spawnedByClientId, enemyClientId);
        }
    }

    public override void Recast() {
        
        var jsonMappings = JsonConvert.SerializeObject(ServerAbilityMappings, Formatting.Indented);
        
        Debug.Log("ReCast Lux E Net");
        Debug.Log("isColliding: " + IsColliding);
        
        if (ActiveCollision != null && IsColliding) {
            Debug.Log("ReCast valid collision");
            var collisionPos = ActiveCollision.gameObject.transform.position;
            var playerNetworkObject = ActiveCollision.gameObject.GetComponent<NetworkObject>();
            var enemyClientId = playerNetworkObject.OwnerClientId;
            var target = ActiveCollision.gameObject.GetComponent<LuxPlayerController>();
            
            target.health.TakeDamage(Ability.damage);
            NetworkBuffManager.Instance.AddBuff(Ability.buff, spawnedByClientId, enemyClientId);
            TriggerCollisionClientRpc(jsonMappings, collisionPos, playerNetworkObject.NetworkObjectId, Ability.key);
        }
        else {
            Debug.Log("ReCast non collision");
            DetonateClientRpc(jsonMappings);
        }

        Debug.Log("ReCast Destroy");
        CanBeDestroyed = true;
        ScheduleDestroy(1);
        IsColliding = false;
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
    
    protected override void Move() {
        
        // The distance the projectile moves per frame
        float distance = Time.deltaTime * Ability.speed;

        float inputRange = Vector3.Distance(TargetPosition, InitialPosition);

        // The current remaining distance the projectile must travel to reach the mouse position (at time of cast)
        RemainingDistance = (float)Math.Round(Math.Min(inputRange, Ability.range) - Vector3.Distance(transform.position, InitialPosition), 2);

        // Ensures the projectile stops moving once remaining distance is zero 
        float travelDistance = Mathf.Min(distance, RemainingDistance);

        // Move the projectile
        transform.Translate(TargetDirection * travelDistance, Space.World);
    }
}
