using System;
using UnityEngine;
using UnityEngine.VFX;

public class Lux_E_Mis : ProjectileAbility {
    
    // GameObjects
    private GameObject _particles;
    private GameObject _orb;
    private GameObject _ring;
    private GameObject _particlesIn;
    private GameObject _distortionQuad;
    private GameObject _explosion;
    private GameObject _shockwaveQuad;

    // VFX
    private VisualEffect _particlesVfx;
    private VisualEffect _orbVfx;
    private VisualEffect _ringVfx;
    private VisualEffect _particlesInVfx;
    private VisualEffect _explosionVfx;

    private bool _canPlayRingVfx = true;
    private bool _canPlayExplosionVfx = true;
    private bool _canTestQuads = true;
    private float _currentLingeringLifetime;
    private float _totalLifetime;
   
    void Start() {
        
        _totalLifetime = projectileLifetime + ability.lingeringLifetime;
        _currentLingeringLifetime = ability.lingeringLifetime;
        
        _particles = transform.GetChild(1).gameObject;
        _particlesVfx = _particles.GetComponent<VisualEffect>();
        _particlesVfx.SetFloat("lifetime", _totalLifetime);
        _particlesVfx.Play();

        _orb = transform.GetChild(2).gameObject;
        _orbVfx = _orb.GetComponent<VisualEffect>();
        _orbVfx.SetFloat("lifetime", _totalLifetime);
        _orbVfx.Play();

        _distortionQuad = transform.GetChild(0).gameObject;
        _shockwaveQuad = transform.GetChild(6).gameObject;

        _ring = transform.GetChild(3).gameObject;
        _ringVfx = _ring.GetComponent<VisualEffect>();
        _ringVfx.SetFloat("lifetime", ability.lingeringLifetime);

        _particlesIn = transform.GetChild(4).gameObject;
        _particlesInVfx = _particlesIn.GetComponent<VisualEffect>();
        _particlesInVfx.SetFloat("lifetime", ability.lingeringLifetime);
         
        _explosion = transform.GetChild(5).gameObject;
        _explosionVfx = _explosion.GetComponent<VisualEffect>();

    }

    void Update(){
        
        // Play ring and particles when the projectile reaches target and stops moving
        if(remainingDistance <= 0){
            
            if (isRecast) {
                ReCast();
                isRecast = false;
                return;
            }

            if(_canPlayRingVfx){

                // Play the E Ring and Particles VFX now the projectile has landed
                _ringVfx.Play();
                _particlesInVfx.Play();
                _canPlayRingVfx = false;
            }
            else{
                _currentLingeringLifetime -= Time.deltaTime;
            }

            if(_currentLingeringLifetime <= 0 && _canPlayExplosionVfx){
                Detonate();
            }

            if(_currentLingeringLifetime <= -0.5){
                // Turn off shockwave distortion
                _shockwaveQuad.SetActive(false);
                canBeDestroyed = true;
                player.ActiveAbilityPrefabs.Remove(ability.key);
                ClientObjectPool.Instance.ReturnObjectToPool(ability, AbilityPrefabType.Projectile, gameObject);
            }
        }
        else {
            // Move object
            MoveProjectile();
        }
    }
    
    public void Detonate() {
        // Turn off pinch distortion
        _distortionQuad.SetActive(false);
        // Turn on shockwave distortion
        _shockwaveQuad.SetActive(true);
        _explosionVfx.Play();
        _canTestQuads = false;
        _canPlayExplosionVfx = false;
    }
    
    public override void ResetVFX() {
        
        Start();
        
        _particlesVfx.Stop();
        _orbVfx.Stop();
        _ringVfx.Stop();
        _particlesInVfx.Stop();
        _explosionVfx.Stop();
        
        _canPlayRingVfx = true;
        _canPlayExplosionVfx = true;
        _canTestQuads = true;
        canBeDestroyed = false;  
        
        _distortionQuad.SetActive(true);
        _shockwaveQuad.SetActive(false);

        _particlesVfx.Play();
        _orbVfx.Play();
    }

    public override void ReCast() {
        _currentLingeringLifetime = 0;
    }

    protected override void MoveProjectile() {
        // The distance the projectile moves per frame
        float distance = Time.deltaTime * projectileSpeed;

        float inputRange = Vector3.Distance(projectileTargetPosition, initialPosition);

        // The current remaining distance the projectile must travel to reach the mouse position (at time of cast)
        remainingDistance = (float)Math.Round(Math.Min(inputRange, projectileRange) - Vector3.Distance(transform.position, initialPosition), 2);

        // Ensures the projectile stops moving once remaining distance is zero 
        float travelDistance = Mathf.Min(distance, remainingDistance);

        // Move the projectile
        transform.Translate(projectileDirection * travelDistance, Space.World);
    }
}
