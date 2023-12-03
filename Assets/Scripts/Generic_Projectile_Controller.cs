using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Generic_Projectile_Controller : MonoBehaviour
{

    private Dictionary<string, object> abilityData;
    private Vector3 direction;
    private float speed;
    private float range;

    public float remainingDistance = Mathf.Infinity;

    private Vector3 initialPosition;

    // Start is called before the first frame update
    void Start(){

        initialPosition = transform.position;
        
    }

    // Update is called once per frame
    void Update(){

        if(gameObject != null && gameObject.activeSelf){

            float distance = Time.deltaTime * speed;
            remainingDistance = (float)Math.Round(range - Vector3.Distance(transform.position, initialPosition), 2);
            float travelDistance = Mathf.Min(distance, remainingDistance);

            transform.Translate(direction * travelDistance, Space.World);
        
        }
        
    }

    public void setParams(Dictionary<string, object> abilityData){
        if(gameObject != null && gameObject.activeSelf){
            this.abilityData = abilityData as Dictionary<string, object>;
            direction = (Vector3)abilityData["direction"];
            speed = (float)abilityData["speed"];
            range = (float)abilityData["range"];
        }
    }

}
