using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;

/*
Lux_Q_Mis.cs

Controls the movement of the Lux Q missile (Networked version)
Sets VFX properties for Q_Orb and Q_Vortex_Trails
Component of: Lux_Q_Mis_Net prefab

*/

public class Lux_Q_Mis_Net : NetworkProjectileAbility {
    
    private Vector3 _initialPosition; 

    // Projectile hitbox
    private GameObject _hitbox;
    
    private void Start() {
        NetworkCollision += OnNetworkCollision;
        
        // Store projectile start position in order to calculate remaining distance
        _initialPosition = transform.position;

        // Set the projectile hitbox transform to move along the ground
        _hitbox = gameObject.transform.Find("Hitbox").gameObject;
        _hitbox.transform.position = new Vector3(_hitbox.transform.position.x, 0.5f, _hitbox.transform.position.z);
        _hitbox.transform.localScale = new Vector3(_hitbox.transform.localScale.x, 0.1f, _hitbox.transform.localScale.z);
    }

    private void Update() {
        if(!IsServer) return;
        // Handle end of life
        if (remainingDistance <= 0) {
            canBeDestroyed = true;
            StartCoroutine(DelayBeforeDestroy(1f));
        }
        else {
            // Move object
            MoveProjectile(transform, _initialPosition);
        }
    }
    private IEnumerator DelayBeforeDestroy(float delayInSeconds) {
        yield return new WaitForSeconds(delayInSeconds);
        if(canBeDestroyed) DestroyProjectile();
    }
    
    private void OnNetworkCollision(Vector3 position, GameObject player) {
        SpawnClientHitVFX(position, player);
    }
    
    /// <summary>
    /// Spawn the hit VFX on the client when a projectile collides with a player
    /// </summary>
    /// <param name="position">The position to spawn the hit VFX</param>
    /// <param name="player">The player that was hit</param>
    private void SpawnClientHitVFX(Vector3 position, GameObject player) {
        var playerScript = player.GetComponent<LuxPlayerController>();
        var hitAbility = playerScript.Abilities["Q"];
        var hitPrefab = ClientObjectPool.Instance.GetPooledObject(hitAbility, AbilityPrefabType.Hit);
        var hitScript = hitPrefab.GetComponent<Lux_Q_Hit>();
        
        // We have to do this because we don't call InitProjectileProperties on the hit script
        // TODO: Add function specifically to init hit VFX scripts?
        hitScript.SetAbility(hitAbility);
        hitScript.target = player;
        hitPrefab.transform.position = position;
        hitPrefab.SetActive(true);
        hitScript.ResetVFX();
    }
}