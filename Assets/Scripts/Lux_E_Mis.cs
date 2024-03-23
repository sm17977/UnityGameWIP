using UnityEngine;
using UnityEngine.VFX;

public class Lux_E_Mis : ProjectileAbility
{
    private Vector3 initialPosition;

    // GameObjects
    private GameObject particles;
    private GameObject orb;
    private GameObject ring;
    private GameObject particlesIn;
    private GameObject distortionQuad;
    private GameObject explosion;
    private GameObject shockwaveQuad;

    // VFX
    private VisualEffect particlesVfx;
    private VisualEffect orbVfx;
    private VisualEffect ringVfx;
    private VisualEffect particlesInVfx;
    private VisualEffect explosionVfx;

    private bool canPlayRingVfx = true;
    private bool canPlayExplosionVfx = true;
    private bool canTestQuads = true;
    private float currentLingeringLifetime;
    private float totalLifetime;
   
    void Start(){

        totalLifetime = projectileLifetime + abilityData.lingeringLifetime;
        currentLingeringLifetime = abilityData.lingeringLifetime;

        // Store projectile start position in order to calculate remaining distance
        initialPosition = transform.position;

        particles = transform.GetChild(1).gameObject;
        particlesVfx = particles.GetComponent<VisualEffect>();
        particlesVfx.SetFloat("lifetime", totalLifetime);
        particlesVfx.Play();

        orb = transform.GetChild(2).gameObject;
        orbVfx = orb.GetComponent<VisualEffect>();
        orbVfx.SetFloat("lifetime", totalLifetime);
        orbVfx.Play();

        distortionQuad = transform.GetChild(0).gameObject;
        shockwaveQuad = transform.GetChild(6).gameObject;

        ring = transform.GetChild(3).gameObject;
        ringVfx = ring.GetComponent<VisualEffect>();
        ringVfx.SetFloat("lifetime", abilityData.lingeringLifetime);

        particlesIn = transform.GetChild(4).gameObject;
        particlesInVfx = particlesIn.GetComponent<VisualEffect>();
        particlesInVfx.SetFloat("lifetime", abilityData.lingeringLifetime);
         
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
                // Turn off pinch distortion
                distortionQuad.SetActive(false);

                // Turn on shockwave distortion
                shockwaveQuad.SetActive(true);

                explosionVfx.Play();

                canTestQuads = false;
                canPlayExplosionVfx = false;
            }


        }
    }
}
