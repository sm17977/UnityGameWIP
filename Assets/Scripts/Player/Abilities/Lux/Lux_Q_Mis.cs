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

public class Lux_Q_Mis : ClientAbilityBehaviour {
    
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
        
        // Set the projectile hitbox transform to move along the ground
        _hitbox = gameObject.transform.Find("Hitbox").gameObject;
        _hitbox.transform.position = new Vector3(_hitbox.transform.position.x, 0.5f, _hitbox.transform.position.z);
        _hitbox.transform.localScale = new Vector3(_hitbox.transform.localScale.x, 0.1f, _hitbox.transform.localScale.z);

        // Get Orb VFX
        _orbVfx = GetComponent<VisualEffect>();
        // Set Orb VFX liftetime so the VFX stops when projectile range has been reached
        _orbVfx.SetFloat("lifetime", LifeTime);

        // Get Trails VFX
        _qTrails = gameObject.transform.Find("Q_Trails").gameObject;
        _qTrailsVfx = _qTrails.GetComponent<VisualEffect>();
        _qTrailsVfx.SetBool("setActive", true);
    }

    private void Update() {
        // Handle end of life
        if (RemainingDistance <= 0) {
            CanBeDestroyed = true;
            // Before end, set Trails VFX spawn rate block to inactive 
            if (_qTrailsVfx != null && _qTrailsVfx.GetBool("setActive")) _qTrailsVfx.SetBool("setActive", false);
            StartCoroutine(DelayBeforeDestroy(1f));
        }
        else {
            // Move object
            Move();
        }
    }

    private IEnumerator DelayBeforeDestroy(float delayInSeconds) {
        yield return new WaitForSeconds(delayInSeconds);
        if(CanBeDestroyed) DestroyAbilityPrefab(AbilityPrefabType.Projectile);
    }

    // Detect projectile hitbox collision with enemy 
    private void OnCollisionEnter(Collision collision) {
        if (GlobalState.IsMultiplayer) return;
        ProcessSinglePlayerCollision(collision);
    }

    private void ProcessSinglePlayerCollision(Collision collision) {
        if (((PlayerType == PlayerType.Player && collision.gameObject.name == "Lux_AI") ||
             (PlayerType == PlayerType.Bot && collision.gameObject.name == "Lux_Player")) && !HasHit) {
            HasHit = true;
            _target = collision.gameObject.GetComponent<LuxController>();

            if (!_target.ClientBuffManager.HasBuffApplied(Ability.buff)) {
                SpawnHitVfx(collision.gameObject);

                if (PlayerType == PlayerType.Bot) _target.ProcessPlayerDeath();
            }
            Ability.buff.Apply(_target);
        }
    }
    
    // Instantiate the hit vfx prefab
    private void SpawnHitVfx(GameObject target) {
        // Spawn the prefab 
        _newQHit = Instantiate(qHit, target.transform.position, Quaternion.identity);
        hitScript = _newQHit.GetComponent<Lux_Q_Hit>();
        hitScript.target = target;
    }
    
    public override void ResetVFX() {
        Start();
    }

    protected override void Move() {
        // The distance the projectile moves per frame
        float distance = Time.deltaTime * Ability.speed;

        // The current remaining distance the projectile must travel to reach projectile range
        RemainingDistance = (float)Math.Round(Ability.range - Vector3.Distance(transform.position, InitialPosition), 2);

        // Ensures the projectile stops moving once remaining distance is zero 
        float travelDistance = Mathf.Min(distance, RemainingDistance);

        // Move the projectile
        transform.Translate(TargetDirection * travelDistance, Space.World);
        
    }
}