using System;
using System.Collections;
using Mono.CSharp;
using Multiplayer;
using QFSW.QC;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

/*
Lux_Q_Mis.cs

Controls the movement of the Lux Q missile
Sets VFX properties for Q_Orb and Q_Vortex_Trails
Component of: Lux_Q_Mis prefab

*/

public class Lux_Q_Mis : ProjectileAbility {
    private Vector3 initialPosition; // Add intialPosition field to Lux.cs?

    // Projectile hitbox
    private GameObject hitbox;

    // VFX Assets
    private VisualEffect orbVfx;
    private GameObject qTrails;
    private VisualEffect qTrailsVfx;

    // Hit VFX
    public GameObject qHit;
    public Lux_Q_Hit hitScript;
    private GameObject newQHit;

    // Collision
    private LuxController target;

    private void Start() {
        #if DEDICATED_SERVER
            Debug.Log("LuxQMis - Start (Server)");
        #endif

        // Store projectile start position in order to calculate remaining distance
        initialPosition = transform.position;

        // Set the projectile hitbox transform to move along the ground
        hitbox = gameObject.transform.Find("Hitbox").gameObject;
        hitbox.transform.position = new Vector3(hitbox.transform.position.x, 0.5f, hitbox.transform.position.z);
        hitbox.transform.localScale = new Vector3(hitbox.transform.localScale.x, 0.1f, hitbox.transform.localScale.z);

        // Get Orb VFX
        orbVfx = GetComponent<VisualEffect>();
        // Set Orb VFX liftetime so the VFX stops when projectile range has been reached
        orbVfx.SetFloat("lifetime", projectileLifetime);

        // Get Trails VFX
        qTrails = gameObject.transform.Find("Q_Trails").gameObject;
        qTrailsVfx = qTrails.GetComponent<VisualEffect>();
    }

    private void Update() {
        // Handle end of life
        if (remainingDistance <= 0) {
            canBeDestroyed = true;
            // Before end, set Trails VFX spawn rate block to inactive 
            if (qTrailsVfx != null && qTrailsVfx.GetBool("setActive")) qTrailsVfx.SetBool("setActive", false);
            StartCoroutine(DelayBeforeDestroy(1f));
        }
        else {
            // Move object
            MoveProjectile(transform, initialPosition);
        }
    }

    private IEnumerator DelayBeforeDestroy(float delayInSeconds) {
        yield return new WaitForSeconds(delayInSeconds);
        if(canBeDestroyed) DestroyProjectile();
    }

    // Detect projectile hitbox collision with enemy 
    private void OnCollisionEnter(Collision collision) {
        #if DEDICATED_SERVER
            Debug.Log("Projectile OnCollisionEnter triggered by " + collision.gameObject.name);
        #endif
        
        Debug.Log("Collision with " + collision.gameObject.name + " " + collision.gameObject.transform.GetInstanceID());

        if (GlobalState.IsMultiplayer) {
            #if DEDICATED_SERVER
                ProcessMultiplayerCollision(collision);
            #endif
        }
        else {
            ProcessSinglePlayerCollision(collision);
        }
    }

    private void ProcessSinglePlayerCollision(Collision collision) {
        if (((playerType == PlayerType.Player && collision.gameObject.name == "Lux_AI") ||
             (playerType == PlayerType.Bot && collision.gameObject.name == "Lux_Player")) && !hasHit) {
            hasHit = true;
            target = collision.gameObject.GetComponent<LuxController>();

            if (!target.BuffManager.HasBuffApplied(ability.buff)) {
                SpawnHitVfx(collision.gameObject);

                if (playerType == PlayerType.Bot) target.ProcessPlayerDeath();
            }

            ability.buff.Apply(target);
        }
    }

    private void ProcessMultiplayerCollision(Collision collision) {
        
        try {
            ulong enemyClientId = 999;
            if (!collision.gameObject.CompareTag("Player")) return;
            
            enemyClientId = collision.gameObject.GetComponent<NetworkObject>().OwnerClientId;
        
            var isDifferentPlayer = enemyClientId != sourceClientId;
            Debug.Log("Source ID: " + sourceClientId + ", Enemy ID: " + enemyClientId + ", Different Player?: " +
                      isDifferentPlayer + ", hasHit: " + hasHit);


            if (playerType == PlayerType.Player && isDifferentPlayer && !hasHit) {
                Debug.Log("Hit enemy!");
                hasHit = true;
                target = collision.gameObject.GetComponent<LuxController>();
                //SpawnHitVfx(collision.gameObject);

                // if(!target.BuffManager.HasBuffApplied(ability.buff)){
                //     SpawnHitVfx(collision.gameObject);
                //
                // if(playerType == PlayerType.Bot){
                //     target.ProcessPlayerDeath();
                // }
                // }

                //ability.buff.Apply(target);

                DestroyProjectileClientRpc();
            }
        }
        catch(Exception e) {
            Debug.LogError(e);
        }
    }

    // Instantiate the hit vfx prefab
    private void SpawnHitVfx(GameObject target) {
        // Spawn the prefab 
        newQHit = Instantiate(qHit, target.transform.position, Quaternion.identity);
        projectiles.Add(newQHit);
        hitScript = newQHit.GetComponent<Lux_Q_Hit>();
        hitScript.target = target;
    }

    private void DestroyProjectile() {
        #if !DEDICATED_SERVER
            var networkObject = gameObject.GetComponent<NetworkObject>();
            if (networkObject.IsSpawned && IsOwnedByServer) {
                canBeDestroyed = false;
                return;
            }
            ClientProjectilePool.Instance.ReturnProjectileToPool(gameObject);
        #endif
        
        #if DEDICATED_SERVER
            ServerProjectilePool.Instance.ReturnProjectileToPool(gameObject);
        #endif
        canBeDestroyed = false;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void DestroyProjectileClientRpc() {
        Debug.Log("Client RPC Delete Projectile: " + gameObject.transform.GetInstanceID());
        DestroyProjectile();
    }
}