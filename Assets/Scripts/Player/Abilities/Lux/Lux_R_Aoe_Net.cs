using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
public class Lux_R_Aoe_Net : NetworkAbilityBehaviour {

    // The hit detection time window
    private const float HitWindowStart = 0.7f;
    private const float HitWindowEnd = 0.9f;
    private bool _hasProcessedHits;
    private HashSet<GameObject> _potentialTargets = new HashSet<GameObject>();

    
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
        if (!_hasProcessedHits && _lifetime >= HitWindowStart && _lifetime <= HitWindowEnd) {
            _hasProcessedHits = true;
            ProcessAllHits();
        }
    }
    
    private void ProcessAllHits() {
        foreach (var target in _potentialTargets) {
            if (target == null) continue;
            
            Debug.Log("Lux R AOE hit player: " + target.name);
            
            var playerController = target.GetComponent<LuxPlayerController>();
            if (playerController != null) {
                playerController.health.TakeDamage(Ability.damage);
            }
        }
        
        _potentialTargets.Clear();
        HasHit = true;
    }
    
    protected override void HandleServerCollision(Collision collision) {
        if (!_hasProcessedHits) {
            _potentialTargets.Add(collision.gameObject);
            Debug.Log($"Added potential target: {collision.gameObject.name} at time {_lifetime}");
        }
    }
}
