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


public class Lux_Q_Mis : MonoBehaviour, IProjectileAbility
{

    // Projectile properties are set in CastingState
    private Vector3 _projectileDirection;
    private float _projectileSpeed;
    private float _projectileRange;


    public bool canBeDestroyed = false;  
    private float remainingDistance = Mathf.Infinity;
    private Vector3 initialPosition; // Add intialPosition field to Lux.cs?


    // Projectile hitbox
    private GameObject hitbox;
    

    // VFX Assets
    private VisualEffect orbVfx;
    private GameObject qTrails;
    private VisualEffect qTrailsVfx;

    public Vector3 ProjectileDirection { get => _projectileDirection; set => _projectileDirection = value; }
    public float ProjectileSpeed { get => _projectileSpeed; set => _projectileSpeed = value; }
    public float ProjectileRange { get => _projectileRange; set => _projectileRange = value; }

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
        float lifetime = _projectileRange / _projectileSpeed;

        // Set Orb VFX liftetime so the VFX stops when projectile range has been reached
        orbVfx.SetFloat("lifetime", lifetime);

        // Move object
        MoveProjectile();

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

    public void MoveProjectile(){
        // The distance the projectile moves per frame
        float distance = Time.deltaTime * _projectileSpeed;

        // The current remaining distance the projectile must travel to reach projectile range
        remainingDistance = (float)Math.Round(_projectileRange - Vector3.Distance(transform.position, initialPosition), 2);

        // Ensures the projectile stops moving once remaining distance is zero 
        float travelDistance = Mathf.Min(distance, remainingDistance);

        // Move the projectile
        transform.Translate(_projectileDirection * travelDistance, Space.World);
    }

    public void InitializeProjectile(Vector3 direction, float speed, float range){
        _projectileDirection = direction;
        _projectileSpeed = speed;
        _projectileRange = range;
    }
}
