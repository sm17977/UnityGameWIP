using System;
using System.Collections;
using System.Collections.Generic;
using Mono.CSharp;
using Newtonsoft.Json;
using QFSW.QC;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

/*
Lux_Q_Mis.cs

Controls the movement of the Lux Q missile (Networked version)
Sets VFX properties for Q_Orb and Q_Vortex_Trails
Component of: Lux_Q_Mis prefab

*/

public class Lux_Q_Mis_Net : NetworkProjectileAbility {
    
    private Vector3 _initialPosition; 

    // Projectile hitbox
    private GameObject _hitbox;
    
    private GameObject _localProjectile;
    private Transform _localProjectilePool;
    
    // Hit VFX
    public GameObject qHit;
    public Lux_Q_Hit hitScript;
    private GameObject _newQHit;

    // Collision
    private LuxController _target;

    private void Start() {
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

    // Detect projectile hitbox collision with enemy 
    private void OnCollisionEnter(Collision collision) {
        ProcessMultiplayerCollision(collision);
    }
    
    private void ProcessMultiplayerCollision(Collision collision) {
        if (!IsServer) return;
        
        try {
            if (!collision.gameObject.CompareTag("Player")) return;
            
            Debug.Log("Collision with " + collision.gameObject.name + " " + collision.gameObject.transform.GetInstanceID());
  
            var playerNetworkObject = collision.gameObject.GetComponent<NetworkObject>();
            var playerNetworkBehaviour= collision.gameObject.GetComponent<NetworkBehaviour>();
            var enemyClientId = playerNetworkObject.OwnerClientId;
            var otherId = playerNetworkBehaviour.NetworkManager.LocalClientId;

            Debug.Log("networkObject.OwnerClientId: " + enemyClientId);
            Debug.Log("networkBehaviour.NetworkManager.LocalClientId: " + otherId);
        
            var isDifferentPlayer = enemyClientId != spawnedByClientId;
            Debug.Log("Source ID: " + spawnedByClientId + ", Enemy ID: " + enemyClientId + ", Different Player?: " +
                      isDifferentPlayer + ", hasHit: " + hasHit);


            if (playerType == PlayerType.Player && isDifferentPlayer && !hasHit) {
                Debug.Log("Hit enemy!");
                hasHit = true;
                try {
                    _target = collision.gameObject.GetComponent<LuxController>();
                }
                catch (Exception e) {
                    Debug.Log("Error trying to get LuxController: " + e.Message);
                }
                //SpawnHitVfx(collision.gameObject);

                // if(!target.BuffManager.HasBuffApplied(ability.buff)){
                //     SpawnHitVfx(collision.gameObject);
                //
                // if(playerType == PlayerType.Bot){
                //     target.ProcessPlayerDeath();
                // }
                // }

                //ability.buff.Apply(target);
                
                Debug.Log("Before calling ToJSON");
                if (Mappings == null) {
                    Debug.Log("Mappings is null!");
                    Debug.Log("IsServer? " + IsServer);
                }
                string jsonMappings = JsonConvert.SerializeObject(Mappings);
                Debug.Log("Before calling Client RPC to update mappings: " + jsonMappings);
                Debug.Log("Mappings keys length: " + (Mappings?.Keys?.Count));
                TriggerCollisionClientRpc(jsonMappings);
                DestroyProjectile();
            }
        }
        catch(Exception e) {
            Debug.LogError(e);
        }
    }

    // Instantiate the hit vfx prefab
    private void SpawnHitVfx(GameObject target) {
        // Spawn the prefab 
        _newQHit = Instantiate(qHit, target.transform.position, Quaternion.identity);
        projectiles.Add(_newQHit);
        hitScript = _newQHit.GetComponent<Lux_Q_Hit>();
        hitScript.target = target;
    }

    private void DestroyProjectile() {
        Debug.Log("Destroying network projectile");
        ServerProjectilePool.Instance.ReturnProjectileToPool(gameObject);
        canBeDestroyed = false;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerCollisionClientRpc(string jsonMappings) {
        _localProjectilePool = GameObject.Find("Client Projectile Pool").transform;
        Debug.Log("Client RPC Trigger Collision");
        Debug.Log("Mappings string: " + jsonMappings);
        Dictionary<int, ulong> mappings = JsonConvert.DeserializeObject<Dictionary<int, ulong>>(jsonMappings);
        foreach (var key in mappings.Keys) {
            if (NetworkManager.LocalClientId == mappings[key]) {
                Debug.Log("Local Projectile: " + key);
                _localProjectile = _localProjectilePool?.Find(key.ToString())?.gameObject;
                if (_localProjectile != null) {
                    Debug.Log("Detected client collision on " + _localProjectile.gameObject.name + " " + _localProjectile.transform.GetInstanceID());
                    ClientProjectilePool.Instance.ReturnProjectileToPool(_localProjectile);
                }
                else {
                    Debug.Log("Couldn't find the local projectile!");
                }
            }
        }
    }
}