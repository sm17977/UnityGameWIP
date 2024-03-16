using UnityEngine;
using System;

public class Lux_E_Mis : ProjectileAbility
{
    private Vector3 initialPosition;
   
    void Start(){

        // Store projectile start position in order to calculate remaining distance
        initialPosition = transform.position;
    }

    void Update(){
    
        // Move object
        MoveProjectile(transform, initialPosition);
    }
}
