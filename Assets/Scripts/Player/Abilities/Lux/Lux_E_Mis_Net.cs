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
    private float _lingeringLifetime;
    private const float DetonationDelay = 1f;
    
    private GameObject _hitbox;
    
    private void Start() {
        _currentLingeringLifetime = LingeringLifetime;
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
            
            if(_currentLingeringLifetime <= 0){
                Detonate();
                ScheduleDestroy(DetonationDelay);
                _currentLingeringLifetime = LingeringLifetime;
            }
        }
        else {
            Move();
        }
    }
    
    private IEnumerator DelayBeforeDestroy(float delayInSeconds) {
        yield return new WaitForSeconds(delayInSeconds);
        if(CanBeDestroyed) DestroyAbilityPrefab();
    }
    
    private void ScheduleDestroy(float delay) {
        if (!gameObject.activeInHierarchy) return;
        if (DestructionScheduled) return;
        DestructionScheduled = true;
        CanBeDestroyed = true;
        StartCoroutine(DelayBeforeDestroy(delay));
    }

    private void Detonate() {
        if (!(ActiveCollision != null && IsColliding)) return;
        
        var playerNetworkObject = ActiveCollision.gameObject.GetComponent<NetworkObject>();
        var enemyClientId = playerNetworkObject.OwnerClientId;
        var target = ActiveCollision.gameObject.GetComponent<LuxPlayerController>();
        
        target.health.TakeDamage(Ability.damage);
        NetworkBuffManager.Instance.AddBuff(Ability.buff, spawnedByClientId, enemyClientId);
    }

    public override void Recast() {
        
        var jsonMappings = JsonConvert.SerializeObject(ServerAbilityMappings, Formatting.Indented);
        
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
            DetonateClientRpc(jsonMappings);
        }
        ScheduleDestroy(DetonationDelay);
    }

    protected override void HandleServerCollision(Collision collision) {
        
    }
    
    protected override void HandleClientCollision(Vector3 position, GameObject player, Ability projectileAbility, GameObject prefab) {
        if(IsOwner) return;
        var projectileScript = prefab.GetComponent<Lux_E_Mis>();
        projectileScript.isRecast = true;
    }

    [Rpc(SendTo.NotOwner)]
    private void DetonateClientRpc(string mappings) {
        // Get the projectile that collided
        var clientProjectile = GetClientCollidedPrefab(mappings);
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
