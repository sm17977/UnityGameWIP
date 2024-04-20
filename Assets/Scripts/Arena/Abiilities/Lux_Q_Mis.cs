using System.Collections;
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
    private Lux_Controller target;
    private bool hasHit = false;

    void Start(){

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
    }

    // Detect projectile hitbox collision with enemy 
    void OnCollisionEnter(Collision collision){
        if(((playerType == PlayerType.Player && collision.gameObject.name == "Lux_AI") || (playerType == PlayerType.Bot && collision.gameObject.name == "Lux_Player" ))  && !hasHit){
            target = collision.gameObject.GetComponent<Lux_Controller>();

            if(!target.buffManager.HasBuffApplied(ability.buff)){
               SpawnHitVfx(collision.gameObject);

               if(playerType == PlayerType.Bot){
                   target.ProcessPlayerDeath();
               }
            }

            ability.buff.Apply(target);
            hasHit = true;
        }
    }

    // Intantiate the hit vfx prefab
    void SpawnHitVfx(GameObject target){
        // Spawn the prefab 
        newQHit = Instantiate(qHit, target.transform.position, Quaternion.identity);
        projectiles.Add(newQHit);
        hitScript = newQHit.GetComponent<Lux_Q_Hit>();
        hitScript.target = target;
    }






}
