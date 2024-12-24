using UnityEngine;
using UnityEngine.VFX;

public class Lux_E_Mis_Net : NetworkProjectileAbility {
    
    private Vector3 _initialPosition;
    
    private bool _canPlayRingVfx = true;
    private bool _canPlayExplosionVfx = true;
    private bool _canTestQuads = true;
    private float _currentLingeringLifetime;
    private float _totalLifetime;
   
    void Start(){

        _totalLifetime = projectileLifetime + ability.lingeringLifetime;
        _currentLingeringLifetime = ability.lingeringLifetime;
        
        _initialPosition = transform.position;

    }

    void Update(){

        // Move object
        MoveProjectile(transform, _initialPosition);

        // Play ring and particles when the projectile reaches target and stops moving
        if(remainingDistance <= 0){
            _currentLingeringLifetime -= Time.deltaTime;
            
            if(_currentLingeringLifetime <= -0.5){
   
                canBeDestroyed = true;
            
                ClientObjectPool.Instance.ReturnObjectToPool(ability, AbilityPrefabType.Projectile, gameObject);
            }
        }
    }
}
