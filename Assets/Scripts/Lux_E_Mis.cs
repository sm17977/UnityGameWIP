using UnityEngine;
using UnityEngine.VFX;

public class Lux_E_Mis : ProjectileAbility
{
    private Vector3 initialPosition;
    private GameObject ring;
    private GameObject particlesIn;
    private GameObject distortionQuad;
    private VisualEffect ringVfx;
    private VisualEffect particlesInVfx;
    private bool canPlayRing = true;
   
    void Start(){

        // Store projectile start position in order to calculate remaining distance
        initialPosition = transform.position;
        distortionQuad = transform.GetChild(0).gameObject;
        ring = transform.GetChild(3).gameObject;
        ringVfx = ring.GetComponent<VisualEffect>();
        particlesIn = transform.GetChild(4).gameObject;
        particlesInVfx = particlesIn.GetComponent<VisualEffect>();

    }

    void Update(){

        // Move object
        MoveProjectile(transform, initialPosition);

        // Play disc and particles when the projectile reaches target
        if(remainingDistance <= 0 && canPlayRing){
            ringVfx.Play();
            distortionQuad.SetActive(false);
            particlesInVfx.Play();
            canPlayRing = false;
        }



    
    }
}
