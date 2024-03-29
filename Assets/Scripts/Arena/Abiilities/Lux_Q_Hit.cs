using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class Lux_Q_Hit : MonoBehaviour
{

    private Lux_Player_Controller playerController;
    private GameObject target;
    public GameObject hitEffectPrefab;
    private GameObject hitEffect;
    private bool hasHit = false;

    // Rings radius
    private float ringRadius1 = 0.4f;
    private float ringRadius2 = 0.45f;
    private float ringRadius3 = 0.5f;

    // Top Ring 
    private VisualEffect topRingVfx;
    private GameObject topRing;
    private float topHeight = 0.5f;

    // Bottom Ring
    private VisualEffect bottomRingVfx;
    private GameObject bottomRing;
    private float bottomHeight = 0.5f;

    // Rays
    private VisualEffect raysVfx;
    private GameObject rays;
    private float middleHeight;

    // Fade Rings
    private float fadeStartTime = 0;
    private float innerRingAlpha = 0.2f;
    private float outerRingAlpha = 0.1f;
    private bool startFadeIn = false;
    private bool startFadeOut = false;
    
    // Ring Spawner State 
    private VFXSpawnerState ringState;
    private List<string> spawnSystemNames;

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
            target = collision.gameObject;
            SpawnHit(target);
            hasHit = true;
        }
    }

    void SpawnHit(GameObject target){

        // Calculate which y position to spawn the 2 rings and rays
        enemyCollider = target.GetComponent<CapsuleCollider>();
        enemyHeight = enemyCollider.height - enemyCollider.radius;
        bottomHeight = target.transform.position.y + 0.001f;
        topHeight = bottomHeight + enemyHeight;
        middleHeight = topHeight / 2;

        // Spawn the prefab 
        hitEffect = Instantiate(hitEffectPrefab, target.transform.position, Quaternion.identity);

        // Retrieve the rings gameobjects
        topRing = hitEffect.transform.GetChild(0).gameObject;
        bottomRing = hitEffect.transform.GetChild(1).gameObject;

        // Set the y position of the rings gameobject
        topRing.transform.position = new Vector3(topRing.transform.position.x, topHeight, topRing.transform.position.z);
        bottomRing.transform.position = new Vector3(bottomRing.transform.position.x,  bottomHeight, bottomRing.transform.position.z);

        // Get the VFX components of the rings
        topRingVfx = topRing.GetComponent<VisualEffect>();
        bottomRingVfx = bottomRing.GetComponent<VisualEffect>();

        // Set radius of the rings (each vfx composed of 3 systems/rings)
        topRingVfx.SetFloat("ringRadius1", ringRadius1);
        topRingVfx.SetFloat("ringRadius2", ringRadius2);
        topRingVfx.SetFloat("ringRadius3", ringRadius3);
        bottomRingVfx.SetFloat("ringRadius1", ringRadius1);
        bottomRingVfx.SetFloat("ringRadius2", ringRadius2);
        bottomRingVfx.SetFloat("ringRadius3", ringRadius3);
        
        // Get list of spawn system names in order to access the current loop count used to trigger the fade out (top and bottom ring are in sync so can just get this from the topRingVfx)
        topRingVfx.GetSpawnSystemNames(spawnSystemNames);

        // Retrieve rays gameobject
        rays = hitEffect.transform.GetChild(2).gameObject;

        // Set the y position of the rays' gameobject
        rays.transform.position = new Vector3(rays.transform.position.x,  middleHeight, rays.transform.position.z);

        // Get rays VFX comppnent
        raysVfx = rays.GetComponent<VisualEffect>();
        raysVfx.SetFloat("ringRadius", ringRadius1);

        // Delay spawning rings and rays
        StartCoroutine(DelayRingAndRaysVFX(0.3f));
    }

    IEnumerator DelayRingAndRaysVFX(float delayInSeconds){
        yield return new WaitForSeconds(delayInSeconds); 
        
        topRingVfx.Play(); 
        bottomRingVfx.Play(); 

        topRingVfx.pause = true;
        bottomRingVfx.pause = true;

        // PreWarm rings so they've drawn a full circle when the effect plays
        topRingVfx.Simulate(0.02f, 50);
        bottomRingVfx.Simulate(0.02f, 50);
        
        topRingVfx.pause = false;
        bottomRingVfx.pause = false;

        startFadeIn = true;
   
        // Play rays
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

        // Set fade to true to allow updating the Set Alpha node
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
        // Reset start time when fade has finished
        else{
            fadeStartTime = 0;
            if(fadeIn){
                startFadeIn = false;
                startFadeOut = true;
            }
            else{
                startFadeOut = false;
                Lux_Q_Mis missile = transform.parent.gameObject.GetComponent<Lux_Q_Mis>();
                missile.canBeDestroyed = true;
                Destroy(hitEffect);
            }
        }
    }
}
