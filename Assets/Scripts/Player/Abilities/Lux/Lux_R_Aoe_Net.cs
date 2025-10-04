using System;
using Unity.Netcode;
using UnityEngine;
public class Lux_R_Aoe_Net : NetworkAbilityBehaviour {

    // The hit detection time window
    private const float HitWindowStart = 0.7f;
    private bool _canHit;
    private float _lifetime;

    private void Start() {
        
    }

    private void Update() {
        if(!IsServer) return;

        if (_lifetime >= Ability.lifetime) {
            Debug.Log("Destroying Lux R AOE (Server)");
            DestroyAbilityPrefab();
        }
        
        _lifetime += Time.deltaTime;
        if (!_canHit && _lifetime >= HitWindowStart) {
            _canHit = true;
            
            if(!HasHit && IsColliding && ActiveCollision != null) {
                HandleServerCollision(ActiveCollision);
            }
        }
    }
    
    protected override void HandleServerCollision(Collision collision) {
        
        if(!_canHit) return;

        Debug.Log("Lux R AOE hit player: " + collision.gameObject.name);
        
        var prefabNetworkObjectId = GetComponent<NetworkObject>().NetworkObjectId;
        var playerNetworkObject = collision.gameObject.GetComponent<NetworkObject>();
        var target = collision.gameObject.GetComponent<LuxPlayerController>();
        
        HasHit = true;
        target.health.TakeDamage(Ability.damage);
        _canHit = false;
        
        TriggerCollisionClientRpc(prefabNetworkObjectId, playerNetworkObject.NetworkObjectId, Ability.key);
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerCollisionClientRpc(ulong prefabNetworkObjectId, ulong collisionNetworkObjectId, string abilityKey) {
        
        // Get the prefab that collided
        var clientPrefab = GetClientPrefab(prefabNetworkObjectId);
        if (clientPrefab == null) return;

        // Get player that was hit
        var player = GetHitPlayer(collisionNetworkObjectId);
        if(player == null) return;
        
        var playerScript = player.GetComponent<LuxPlayerController>();
        var ability = playerScript.Abilities[abilityKey];
        
        // Deactivate the projectile
        ClientObjectPool.Instance.ReturnObjectToPool(ability, AbilityPrefabType.Spawn, clientPrefab);
        ClientPrefabManager.Instance.UnregisterPrefab(prefabNetworkObjectId);
    }
}
