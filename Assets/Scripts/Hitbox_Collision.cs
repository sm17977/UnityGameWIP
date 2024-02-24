using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

public class Hitbox_Collision : MonoBehaviour
{

    private Lux_Player_Controller playerController;
    private GameObject target;
    public GameObject HitEffect;
    public float radius;
    private bool hasHit = false;

    // Top Ring 
    private VisualEffect topRingVfx;
    private GameObject topRing;
    private float topHeight = 0.5f;

    // Bottom Ring
    private VisualEffect bottomRingVfx;
    private GameObject bottomRing;
    private float bottomHeight = 0.5f;

    // Fade Rings
    private float fadeStartTime = 0;
    private float innerRingAlpha = 0.2f;
    private float outerRingAlpha = 0.1f;
    private bool startFadeIn = false;
    private bool startFadeOut = false;
    
    // Ring Spawner State 
    private VFXSpawnerState ringState;
    private List<string> spawnSystemNames;

    // Rays
    private VisualEffect raysVfx;
    private GameObject rays;
    private float middleHeight;


    // Enemy 
    private float enemyHeight;
    private CapsuleCollider enemyCollider;


    // Start is called before the first frame update
    void Start(){
        GameObject player = GameObject.Find("Lux_Player");
        playerController = player.GetComponent<Lux_Player_Controller>();
        spawnSystemNames = new List<string>();
    }

    void Update(){

        if(startFadeOut){
            ringState = topRingVfx.GetSpawnSystemInfo(spawnSystemNames[0]);
            // Start fading out after 2nd loop
            if(ringState.loopIndex == 2){
                Fade(false, 0.3f);
            }
        }   

        if(startFadeIn){
            Fade(true, 0.3f);
        }
    }

    void OnCollisionEnter(Collision collision){
        if(collision.gameObject.name == "Lux_AI" && !hasHit){
            Debug.Log("Q Hit!");
            target = collision.gameObject;
            SpawnHit(target);
            playerController.projectiles.Remove(transform.parent.gameObject);
            //Destroy(transform.parent.gameObject);
            hasHit = true;
        }
    }

    void SpawnHit(GameObject target){

        enemyCollider = target.GetComponent<CapsuleCollider>();
        enemyHeight = enemyCollider.height - enemyCollider.radius;
        bottomHeight = target.transform.position.y + 0.001f;
        topHeight = bottomHeight + enemyHeight;
        middleHeight = topHeight / 2;

        Instantiate(HitEffect, target.transform.position, Quaternion.identity);

        topRing = GameObject.Find("Top Ring");
        bottomRing = GameObject.Find("Bottom Ring");

        topRingVfx = topRing.GetComponent<VisualEffect>();
        //topRingVfx.SetFloat("ringRadius", radius);
        topRing.transform.position = new Vector3(topRing.transform.position.x, topHeight, topRing.transform.position.z);

        bottomRingVfx = bottomRing.GetComponent<VisualEffect>();
        //bottomRingVfx.SetFloat("ringRadius", radius);
        bottomRing.transform.position = new Vector3(bottomRing.transform.position.x,  bottomHeight, bottomRing.transform.position.z);

        topRingVfx.GetSpawnSystemNames(spawnSystemNames);

        rays = GameObject.Find("Rays");
        raysVfx = rays.GetComponent<VisualEffect>();
        raysVfx.SetFloat("ringRadius", radius);
        rays.transform.position = new Vector3(rays.transform.position.x,  middleHeight, rays.transform.position.z);

        StartCoroutine(DelayRingVFX(0.3f));

    }

    IEnumerator DelayRingVFX(float delayInSeconds){
        yield return new WaitForSeconds(delayInSeconds); 

        // PreWarm Rings
        topRingVfx.Play(); 

        topRingVfx.pause = true;
        topRingVfx.Simulate(0.02f, 50);
        topRingVfx.pause = false;

        bottomRingVfx.Play(); 
        bottomRingVfx.pause = true;
        bottomRingVfx.Simulate(0.02f, 50);
        bottomRingVfx.pause = false;

        startFadeIn = true;
   
        raysVfx.Play();
    }

    void Fade(bool fadeIn, float fadeDuration){
        // Set start time for fading out
        if(fadeStartTime == 0){
            fadeStartTime = Time.time;
        }
        // Get time since fade out started
        float elapsedTime = Time.time - fadeStartTime;
        // Clamp between 0 and 1
        float normalizedTime = Mathf.Clamp01(elapsedTime / fadeDuration);

        topRingVfx.SetBool("fade", true);
        bottomRingVfx.SetBool("fade", true);
        
        // Fade out 
        if(!fadeIn){
            topRingVfx.SetFloat("fadeFromInner", innerRingAlpha);
            topRingVfx.SetFloat("fadeFromOuter", outerRingAlpha);
            topRingVfx.SetFloat("fadeToInner", 0f);
            topRingVfx.SetFloat("fadeToOuter", 0f);
            bottomRingVfx.SetFloat("fadeFromInner", innerRingAlpha);
            bottomRingVfx.SetFloat("fadeFromOuter", outerRingAlpha);
            bottomRingVfx.SetFloat("fadeToInner", 0f);
            bottomRingVfx.SetFloat("fadeToOuter", 0f);
        }
        // Fade in
        else{
            topRingVfx.SetFloat("fadeFromInner", 0f);
            topRingVfx.SetFloat("fadeFromOuter", 0f);
            topRingVfx.SetFloat("fadeToInner", innerRingAlpha);
            topRingVfx.SetFloat("fadeToOuter", outerRingAlpha);
            bottomRingVfx.SetFloat("fadeFromInner", 0f);
            bottomRingVfx.SetFloat("fadeFromOuter", 0f);
            bottomRingVfx.SetFloat("fadeToInner", innerRingAlpha);
            bottomRingVfx.SetFloat("fadeToOuter", outerRingAlpha);
        }

        // Set the Lerp node amount using the normalized time
        if(elapsedTime <= fadeDuration){
            topRingVfx.SetFloat("fadeTime", normalizedTime);
            bottomRingVfx.SetFloat("fadeTime", normalizedTime);
        }
        else{
            fadeStartTime = 0;
            if(fadeIn){
                startFadeIn = false;
                startFadeOut = true;
            }
            else{
                startFadeOut = false;
            }
        }
    }
}
