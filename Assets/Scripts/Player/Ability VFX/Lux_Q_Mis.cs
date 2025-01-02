using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

/*
Lux_Q_Mis.cs

Controls the movement of the Lux Q missile
Sets VFX properties for Q_Orb and Q_Vortex_Trails
Component of: Lux_Q_Mis prefab

*/

public class Lux_Q_Mis : ProjectileAbility {
    
    private Vector3 _initialPosition; 

    // Projectile hitbox
    private GameObject _hitbox;

    // VFX Assets
    private VisualEffect _orbVfx;
    private GameObject _qTrails;
    private VisualEffect _qTrailsVfx;

    // Hit VFX
    public GameObject qHit;
    public Lux_Q_Hit hitScript;
    private GameObject _newQHit;

    // Collision
    private LuxController _target;

    private void Start() {
        // Store projectile start position in order to calculate remaining distance
        _initialPosition = transform.position;

        // Set the projectile hitbox transform to move along the ground
        _hitbox = gameObject.transform.Find("Hitbox").gameObject;
        _hitbox.transform.position = new Vector3(_hitbox.transform.position.x, 0.5f, _hitbox.transform.position.z);
        _hitbox.transform.localScale = new Vector3(_hitbox.transform.localScale.x, 0.1f, _hitbox.transform.localScale.z);

        // Get Orb VFX
        _orbVfx = GetComponent<VisualEffect>();
        // Set Orb VFX liftetime so the VFX stops when projectile range has been reached
        _orbVfx.SetFloat("lifetime", projectileLifetime);

        // Get Trails VFX
        _qTrails = gameObject.transform.Find("Q_Trails").gameObject;
        _qTrailsVfx = _qTrails.GetComponent<VisualEffect>();
        _qTrailsVfx.SetBool("setActive", true);
    }

    private void Update() {
        // Handle end of life
        if (remainingDistance <= 0) {
            canBeDestroyed = true;
            // Before end, set Trails VFX spawn rate block to inactive 
            if (_qTrailsVfx != null && _qTrailsVfx.GetBool("setActive")) _qTrailsVfx.SetBool("setActive", false);
            StartCoroutine(DelayBeforeDestroy(1f));
        }
        else {
            // Move object
            MoveProjectile(transform, _initialPosition);
        }
    }

    private IEnumerator DelayBeforeDestroy(float delayInSeconds) {
        yield return new WaitForSeconds(delayInSeconds);
        if(canBeDestroyed) DestroyProjectile();
    }

    // Detect projectile hitbox collision with enemy 
    private void OnCollisionEnter(Collision collision) {
        if (GlobalState.IsMultiplayer) return;
        ProcessSinglePlayerCollision(collision);
    }

    private void ProcessSinglePlayerCollision(Collision collision) {
        if (((playerType == PlayerType.Player && collision.gameObject.name == "Lux_AI") ||
             (playerType == PlayerType.Bot && collision.gameObject.name == "Lux_Player")) && !hasHit) {
            hasHit = true;
            _target = collision.gameObject.GetComponent<LuxController>();

            if (!_target.ClientBuffManager.HasBuffApplied(ability.buff)) {
                SpawnHitVfx(collision.gameObject);

                if (playerType == PlayerType.Bot) _target.ProcessPlayerDeath();
            }
            ability.buff.Apply(_target);
        }
    }
    
    // Instantiate the hit vfx prefab
    private void SpawnHitVfx(GameObject target) {
        // Spawn the prefab 
        _newQHit = Instantiate(qHit, target.transform.position, Quaternion.identity);
        projectiles.Add(_newQHit);
        hitScript = _newQHit.GetComponent<Lux_Q_Hit>();
        hitScript.target = target;
    }
    
    private void DestroyProjectile() {
        ClientObjectPool.Instance.ReturnObjectToPool(ability, AbilityPrefabType.Projectile, gameObject);
        canBeDestroyed = false;
    }

    public override void ResetVFX() {
        Start();
    }
}