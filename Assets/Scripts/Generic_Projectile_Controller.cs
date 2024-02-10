using System;
using UnityEngine;

public class Generic_Projectile_Controller : MonoBehaviour
{

    public Vector3 missile_direction;
    public float missile_speed;
    public float missile_range;
    public float remainingDistance = Mathf.Infinity;
    private GameObject hitbox;
    private Vector3 initialPosition;


    void Start(){
        initialPosition = transform.position;

        hitbox = transform.Find("Hitbox").gameObject;

        hitbox.transform.position = new Vector3(hitbox.transform.position.x, 0.5f, hitbox.transform.position.z);
        hitbox.transform.localScale = new Vector3(hitbox.transform.localScale.x, 0.1f, hitbox.transform.localScale.z);
    }

    
    void Update(){
     
        //Handle movement of projectile
        float distance = Time.deltaTime * missile_speed;
        remainingDistance = (float)Math.Round(missile_range - Vector3.Distance(transform.position, initialPosition), 2);
        float travelDistance = Mathf.Min(distance, remainingDistance);
        transform.Translate(missile_direction * travelDistance, Space.World);
        
    }

}
