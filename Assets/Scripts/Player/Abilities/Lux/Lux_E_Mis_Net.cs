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
    
    private void Start() {
        _currentLingeringLifetime = LingeringLifetime;
    }
    
    private void Update(){
        if(!IsServer) return;
        
        // Play ring and particles when the projectile reaches target and stops moving
        if(RemainingDistance <= 0){
            if (isRecast) {
                Detonate(true);
                isRecast = false;
                return;
            }
            
            _currentLingeringLifetime -= Time.deltaTime;
            
            if(_currentLingeringLifetime <= 0){
                Detonate(false);
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

    private void Detonate(bool recast) {
        
        var jsonMappings = JsonConvert.SerializeObject(ServerAbilityMappings, Formatting.Indented);
        
        if (ActiveCollision != null && IsColliding) {

            var playerNetworkObject = ActiveCollision.gameObject.GetComponent<NetworkObject>();
            var enemyClientId = playerNetworkObject.OwnerClientId;
            var target = ActiveCollision.gameObject.GetComponent<LuxPlayerController>();

            HasHit = true;
            target.health.TakeDamage(Ability.damage);
            NetworkBuffManager.Instance.AddBuff(Ability.buff, spawnedByClientId, enemyClientId);
        }

        // If it's a recast, force detonate VFX earlier on all clients
        if (recast) DetonateClientRpc(jsonMappings);
        
        ScheduleDestroy(DetonationDelay);
    }
    
    [Rpc(SendTo.NotOwner)]
    private void DetonateClientRpc(string mappings) {
        // Get the prefab that collided
        var clientPrefab = GetClientPrefab(mappings);
        if (clientPrefab == null) return;

        var clientPrefabScript = clientPrefab.GetComponent<Lux_E_Mis>();
        clientPrefabScript.isRecast = true;
    }
    
    
    protected override void Move() {
        
        // Move ability to mouse position or max range if it's outside it
        
        float distance = Time.deltaTime * Ability.speed;
        float inputRange = Vector3.Distance(TargetPosition, InitialPosition);
        RemainingDistance = (float)Math.Round(Math.Min(inputRange, Ability.range) - Vector3.Distance(transform.position, InitialPosition), 2);
        float travelDistance = Mathf.Min(distance, RemainingDistance);
        transform.Translate(TargetDirection * travelDistance, Space.World);
    }
}
