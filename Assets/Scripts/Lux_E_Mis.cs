using UnityEngine;
using System;

public class Lux_E_Mis : MonoBehaviour, IProjectileAbility
{

    // Projectile properties are set in CastingState
    private Vector3 _projectileDirection;
    private float _projectileSpeed;
    private float _projectileRange;
    private float remainingDistance = Mathf.Infinity;
    private Vector3 initialPosition;

    public Vector3 ProjectileDirection { get => _projectileDirection; set => _projectileDirection = value; }
    public float ProjectileSpeed { get => _projectileSpeed; set => _projectileSpeed = value; }
    public float ProjectileRange { get => _projectileRange; set => _projectileRange = value; }

    public void InitializeProjectile(Vector3 direction, float speed, float range){
        _projectileDirection = direction;
        _projectileSpeed = speed;
        _projectileRange = range;
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

    void Start(){

        // Store projectile start position in order to calculate remaining distance
        initialPosition = transform.position;
        
    }

    void Update(){
    
        // Move object
        MoveProjectile();
    }
}
