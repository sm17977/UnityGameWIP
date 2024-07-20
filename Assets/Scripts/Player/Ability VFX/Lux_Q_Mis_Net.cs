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

            Debug.Log("Collision with Player: " + collision.gameObject.name);

            var collisionPos = collision.gameObject.transform.position;
            var playerNetworkObject = collision.gameObject.GetComponent<NetworkObject>();
            var enemyClientId = playerNetworkObject.OwnerClientId;
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


                string jsonMappings = JsonConvert.SerializeObject(Mappings);
                TriggerCollisionClientRpc(jsonMappings, collisionPos, playerNetworkObject.NetworkObjectId);
                DestroyProjectile();
            }
        }
        catch(Exception e) {
            Debug.LogError(e);
        }
    }
    
    private void DestroyProjectile() {
        ServerProjectilePool.Instance.ReturnProjectileToPool(gameObject);
        canBeDestroyed = false;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerCollisionClientRpc(string jsonMappings, Vector3 position, ulong collisionNetworkObjectId) {
        _localProjectilePool = GameObject.Find("Client Projectile Pool").transform;
        Debug.Log("Client RPC Trigger Collision");
        Dictionary<int, ulong> mappings = JsonConvert.DeserializeObject<Dictionary<int, ulong>>(jsonMappings);
        foreach (var key in mappings.Keys) {
            if (NetworkManager.LocalClientId == mappings[key]) {
                _localProjectile = _localProjectilePool?.Find(key.ToString())?.gameObject;
                if (_localProjectile != null) {
                    Debug.Log("Detected client collision on " + _localProjectile.gameObject.name + " " + _localProjectile.transform.GetInstanceID());
                    ClientProjectilePool.Instance.ReturnObjectToPool(_localProjectile);
                    var hit = ClientProjectilePool.Instance.GetPooledObject(ProjectileType.Hit);
                    hit.transform.position = position;
                    var hitScript = hit.GetComponent<Lux_Q_Hit>();
                    var players = GameObject.Find("Players").transform;

                    if (NetworkManager.LocalClient.PlayerObject.NetworkObjectId == collisionNetworkObjectId) {
                        hitScript.target = NetworkManager.LocalClient.PlayerObject.gameObject;
                    }
                    else {
                        hitScript.target = players.Find(collisionNetworkObjectId.ToString()).gameObject;
                    }
                    
                    hit.SetActive(true);
                }
                else {
                    Debug.Log("Couldn't find the local projectile!");
                }
            }
        }
    }
}