using UnityEngine;
using UnityEngine.VFX;

public class Lux_E_Mis : ProjectileAbility
{
    private Vector3 initialPosition;

    // GameObjects
    private GameObject orb;
    private GameObject ring;
    private GameObject particlesIn;
    private GameObject distortionQuad;
    private GameObject explosion;

    // VFX
    private VisualEffect orbVfx;
    private VisualEffect ringVfx;
    private VisualEffect particlesInVfx;
    private VisualEffect explosionVfx;

    private bool canPlayRingVfx = true;
    private bool canPlayExplosionVfx = true;
    private float currentLingeringLifetime;
    private float totalLifetime;
   
    void Start(){

        totalLifetime = projectileLifetime + abilityData.maxLingeringLifetime;
        currentLingeringLifetime = abilityData.maxLingeringLifetime;

        // Store projectile start position in order to calculate remaining distance
        initialPosition = transform.position;

        orb = transform.GetChild(2).gameObject;
        orbVfx = orb.GetComponent<VisualEffect>();
        orbVfx.SetFloat("lifetime", totalLifetime);

        distortionQuad = transform.GetChild(0).gameObject;

        ring = transform.GetChild(3).gameObject;
        ringVfx = ring.GetComponent<VisualEffect>();
        ringVfx.SetFloat("lifetime", abilityData.maxLingeringLifetime);

        particlesIn = transform.GetChild(4).gameObject;
        particlesInVfx = particlesIn.GetComponent<VisualEffect>();
        particlesInVfx.SetFloat("lifetime", abilityData.maxLingeringLifetime);
         
        explosion = transform.GetChild(5).gameObject;
        explosionVfx = explosion.GetComponent<VisualEffect>();

    }

    void Update(){

        // Move object
        MoveProjectile(transform, initialPosition);

        // Play ring and particles when the projectile reaches target and stops moving
        if(remainingDistance <= 0){

            if(canPlayRingVfx){

                // Play the E Ring and Particles VFX now the projectile has landed
                ringVfx.Play();
                particlesInVfx.Play();
                canPlayRingVfx = false;
            }
            else{
                currentLingeringLifetime -= Time.deltaTime;
            }

            if(currentLingeringLifetime <= 0 && canPlayExplosionVfx){

                // Turn off distortion
                distortionQuad.SetActive(false);

                explosionVfx.Play();
                canPlayExplosionVfx = false;
            }

        }
    }
}
