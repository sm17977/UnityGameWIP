using UnityEngine;
using UnityEngine.VFX;

public class Lux_E_Mis_Net : NetworkProjectileAbility {
    
    private Vector3 _initialPosition;

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
   
    void Start(){

        _totalLifetime = projectileLifetime + ability.lingeringLifetime;
        _currentLingeringLifetime = ability.lingeringLifetime;
        
        _initialPosition = transform.position;

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

        // Move object
        MoveProjectile(transform, _initialPosition);

        // Play ring and particles when the projectile reaches target and stops moving
        if(remainingDistance <= 0){

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
                // Turn off pinch distortion
                _distortionQuad.SetActive(false);

                // Turn on shockwave distortion
                _shockwaveQuad.SetActive(true);

                _explosionVfx.Play();

                _canTestQuads = false;
                _canPlayExplosionVfx = false;
            }

            if(_currentLingeringLifetime <= -0.5){
                // Turn off shockwave distortion
                _shockwaveQuad.SetActive(false);
                canBeDestroyed = true;
                ResetVFX();
                ClientObjectPool.Instance.ReturnObjectToPool(ability, AbilityPrefabType.Projectile, gameObject);
            }
        }
    }

    void ResetVFX() {
        _currentLingeringLifetime = ability.lingeringLifetime;
        _canPlayRingVfx = true;
        _canPlayExplosionVfx = true;
        _distortionQuad.SetActive(true);
    }
}
