using System;
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


    void Start(){

        // Store projectile start position in order to calculate remaining distance
        initialPosition = transform.position;

        // Set the projectile hitbox transform to move along the ground
        hitbox = gameObject.transform.Find("Hitbox").gameObject;
        hitbox.transform.position = new Vector3(hitbox.transform.position.x, 0.5f, hitbox.transform.position.z);
        hitbox.transform.localScale = new Vector3(hitbox.transform.localScale.x, 0.1f, hitbox.transform.localScale.z);
        
        // Get Orb VFX
        orbVfx = GetComponent<VisualEffect>();

        // Get Trails VFX
        qTrails = gameObject.transform.Find("Q_Trails").gameObject;
        qTrailsVfx = qTrails.GetComponent<VisualEffect>();
      
    }
    
    void Update(){
     
        // The time it takes to reach projectile range 
        float lifetime = projectileRange / projectileSpeed;

        // Set Orb VFX liftetime so the VFX stops when projectile range has been reached
        orbVfx.SetFloat("lifetime", lifetime);

        // Move object
        MoveProjectile(transform, initialPosition);

        // Handle end of life
        if(remainingDistance <= 0){
            // Before end, set Trails VFX spawn rate block to inactive 
            if(qTrailsVfx != null && qTrailsVfx.GetBool("setActive")){
                qTrailsVfx.SetBool("setActive", false);
            }
            StartCoroutine(DelayBeforeDestroy(3f));
        }
    }

    IEnumerator DelayBeforeDestroy(float delayInSeconds){
        yield return new WaitForSeconds(delayInSeconds);
        canBeDestroyed = true;
    }
}
