using System;
using UnityEngine;
using UnityEngine.VFX;
using System.Collections;
using System.Collections.Generic;

public class Generic_Projectile_Controller : MonoBehaviour
{

    public Vector3 missile_direction;
    public float missile_speed;
    public float missile_range;


    public float remainingDistance = Mathf.Infinity;
    public bool die = false;
    private GameObject hitbox;
    private Vector3 initialPosition;
    private float lifetime;


    private VisualEffect orbVfx;
    private GameObject qTrails;
    private VisualEffect qTrailsVfx;

    private VFXSpawnerState qTrailsState;
    private List<string> spawnSystemNames;
    

    void Start(){
        orbVfx = GetComponent<VisualEffect>();
        qTrails = orbVfx.transform.GetChild(0).gameObject;
        qTrailsVfx = qTrails.GetComponent<VisualEffect>();
        qTrailsVfx.SetBool("setActive", true);

        spawnSystemNames = new List<string>();
        qTrailsVfx.GetSpawnSystemNames(spawnSystemNames);
    
        initialPosition = transform.position;
        hitbox = transform.Find("Hitbox").gameObject;
        hitbox.transform.position = new Vector3(hitbox.transform.position.x, 0.5f, hitbox.transform.position.z);
        hitbox.transform.localScale = new Vector3(hitbox.transform.localScale.x, 0.1f, hitbox.transform.localScale.z);
    }

    
    void Update(){
     
        //Handle movement of projectile
        float distance = Time.deltaTime * missile_speed;
        lifetime = missile_range / missile_speed;
        orbVfx.SetFloat("lifetime", lifetime);
        remainingDistance = (float)Math.Round(missile_range - Vector3.Distance(transform.position, initialPosition), 2);
        float travelDistance = Mathf.Min(distance, remainingDistance);
        transform.Translate(missile_direction * travelDistance, Space.World);
   
        if(qTrailsVfx != null){
            if(remainingDistance <= 0 && qTrailsVfx.GetBool("setActive")){
                qTrailsVfx.SetBool("setActive", false);
            }
        }
    }
}
