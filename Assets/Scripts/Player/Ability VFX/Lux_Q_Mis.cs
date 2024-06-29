using System.Collections;
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

public class Lux_Q_Mis : ProjectileAbility
{

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
    private bool hasHit = false;

    void Start(){
        if (IsServer) {
            Debug.Log("SERVER START - Lux Q Mis ");
        }

        // Store projectile start position in order to calculate remaining distance
        initialPosition = transform.position;

        // Set the projectile hitbox transform to move along the ground
        hitbox = gameObject.transform.Find("Hitbox").gameObject;
        hitbox.transform.position = new Vector3(hitbox.transform.position.x, 0.5f, hitbox.transform.position.z);
        hitbox.transform.localScale = new Vector3(hitbox.transform.localScale.x, 0.1f, hitbox.transform.localScale.z);
        
        // Get Orb VFX
        orbVfx = GetComponent<VisualEffect>();
        // Set Orb VFX liftetime so the VFX stops when projectile range has been reached
        orbVfx.SetFloat("lifetime", 5);

        // Get Trails VFX
        qTrails = gameObject.transform.Find("Q_Trails").gameObject;
        qTrailsVfx = qTrails.GetComponent<VisualEffect>();
    }
    
    void Update(){
     
        // Move object
        MoveProjectile(transform, initialPosition);

        // Handle end of life
        if(remainingDistance <= 0){
            // Before end, set Trails VFX spawn rate block to inactive 
            if(qTrailsVfx != null && qTrailsVfx.GetBool("setActive")){
                qTrailsVfx.SetBool("setActive", false);
            }
            StartCoroutine(DelayBeforeDestroy(1f));
        }
    }

    IEnumerator DelayBeforeDestroy(float delayInSeconds){
        yield return new WaitForSeconds(delayInSeconds);
        canBeDestroyed = true;
#if DEDICATED_SERVER
        ProjectilePool.Instance.ReturnProjectileToPool(gameObject);
#endif
    }

    // Detect projectile hitbox collision with enemy 
    void OnCollisionEnter(Collision collision){
        
        if(GlobalState.IsMultiplayer) ProcessMultiplayerCollision(collision);
        else ProcessSinglePlayerCollision(collision);
        
    }

    void ProcessSinglePlayerCollision(Collision collision) {
        if(((playerType == PlayerType.Player && collision.gameObject.name == "Lux_AI") || (playerType == PlayerType.Bot && collision.gameObject.name == "Lux_Player" ))  && !hasHit){
            hasHit = true;
            target = collision.gameObject.GetComponent<LuxController>();

            if(!target.BuffManager.HasBuffApplied(ability.buff)){
                SpawnHitVfx(collision.gameObject);

                if(playerType == PlayerType.Bot){
                    target.ProcessPlayerDeath();
                }
            }
            ability.buff.Apply(target);
        }
    }

    void ProcessMultiplayerCollision(Collision collision) {
        if (!IsServer) return;

        var enemyClientId = collision.gameObject.GetComponent<NetworkObject>().OwnerClientId;
        var isDifferentPlayer = collision.gameObject.CompareTag("Player") && enemyClientId != sourceClientId;
        Debug.Log("Attacker ID: " + sourceClientId + " Enemy ID: " + enemyClientId + " Different Player: " + isDifferentPlayer + " playerType: " + playerType + " hasHit: " + hasHit);
        
        
        if(playerType == PlayerType.Player && isDifferentPlayer && !hasHit){
            Debug.Log("Collision!");
            hasHit = true;
            target = collision.gameObject.GetComponent<LuxController>();
            SpawnHitVfx(collision.gameObject);

            // if(!target.BuffManager.HasBuffApplied(ability.buff)){
            //     SpawnHitVfx(collision.gameObject);
            //
                // if(playerType == PlayerType.Bot){
                //     target.ProcessPlayerDeath();
                // }
            // }

            //ability.buff.Apply(target);

            ProjectilePool.Instance.ReturnProjectileToPool(gameObject);
        }
    }

    // Instantiate the hit vfx prefab
    void SpawnHitVfx(GameObject target){
        // Spawn the prefab 
        newQHit = Instantiate(qHit, target.transform.position, Quaternion.identity);
        projectiles.Add(newQHit);
        hitScript = newQHit.GetComponent<Lux_Q_Hit>();
        hitScript.target = target;
    }
    
}
