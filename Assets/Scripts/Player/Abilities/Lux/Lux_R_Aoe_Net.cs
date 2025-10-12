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
        
        var target = collision.gameObject.GetComponent<LuxPlayerController>();
        HasHit = true;
        target.health.TakeDamage(Ability.damage);
        _canHit = false;
        
    }
}
