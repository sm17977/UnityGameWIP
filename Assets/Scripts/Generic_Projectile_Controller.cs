using System;
using UnityEngine;

public class Generic_Projectile_Controller : MonoBehaviour
{

    private Vector3 missile_direction;
    private float missile_speed;
    private float missile_range;
    public float remainingDistance = Mathf.Infinity;
    private Vector3 initialPosition;

    void Start(){
        initialPosition = transform.position;
    }

    // Handles movement of projectile
    void Update(){

        if(gameObject != null && gameObject.activeSelf){
            float distance = Time.deltaTime * missile_speed;
            remainingDistance = (float)Math.Round(missile_range - Vector3.Distance(transform.position, initialPosition), 2);
            float travelDistance = Mathf.Min(distance, remainingDistance);
            transform.Translate(missile_direction * travelDistance, Space.World);
        }
    }

    public void SetParams(float speed, float range, Vector3 dir){
        if(gameObject != null && gameObject.activeSelf){
            missile_speed = speed;
            missile_range = range;
            missile_direction = dir;    
        }
    }

}
