
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.VFX;

public class Lux_Q_Hit : ProjectileAbility
{
    // Target hit
    public GameObject target;
 
    // Ring Radii
    private float baseRadius = 0.4f;
    private Dictionary<string, float> ringRadii = new Dictionary<string, float>(){
        {"ringRadius1", 0.4f},
        {"ringRadius2", 0.45f},
        {"ringRadius3", 0.5f}    
    };

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
    private float fadeStartTime = 0f;
    private float innerRingAlpha = 0.2f;
    private float outerRingAlpha = 0.1f;
    private bool startFadeIn = false;
    private bool startFadeOut = false;
    
    // Enemy 
    private float enemyHeight;
    private CapsuleCollider enemyCollider;

    // Timing
    // Minimum VFX and stun duration is fade in time + fade out time + delay time
    private float prewarmTime = 0.02f;
    private uint prewarmStep = 50;
    private float delayTime = 0.3f;
    private float fadeDuration = 0.3f;
    private float stunDuration = 0f;
    public float totalDuration;
    private float timer;
    private bool vfxPlaying = true;

    void Start(){
        CalculateVfxDuration();
        timer = totalDuration;
        InitVfx(target);
    }

    void CalculateVfxDuration(){
        stunDuration = ability.buff.duration;
        // If the stun duration is longer than the minimum stun time, add the difference to totalDuration
        totalDuration = (fadeDuration * 2f) + delayTime;
        if (totalDuration < stunDuration){
            float diff = stunDuration - totalDuration;
            totalDuration += diff;
        }
    }

    void Update(){

        // Start timer after rings have prewarmed
        if(vfxPlaying && timer > 0){
            timer -= Time.deltaTime;
        }

        // Fade out rings
        if(startFadeOut){
            if(timer <= 0){
                Fade(false, fadeDuration);
            }
        }   

        // Fade in rings
        if(startFadeIn){
            Fade(true, fadeDuration);
        }
    }

    void InitVfx(GameObject target){
        vfxPlaying = true;

        // Calculate the y position to spawn the 2 rings and rays using the target's capsule collider
        enemyCollider = target.GetComponent<CapsuleCollider>();
        enemyHeight = enemyCollider.height - enemyCollider.radius;
        bottomHeight = target.transform.position.y + 0.001f;
        topHeight = bottomHeight + enemyHeight;
        middleHeight = topHeight / 2;

        // Retrieve the rings gameobjects
        topRing = transform.GetChild(0).gameObject;
        bottomRing = transform.GetChild(1).gameObject;

        // Set the y position of the rings gameobject
        topRing.transform.position = new Vector3(topRing.transform.position.x, topHeight, topRing.transform.position.z);
        bottomRing.transform.position = new Vector3(bottomRing.transform.position.x,  bottomHeight, bottomRing.transform.position.z);

        // Get the VFX components of the rings
        topRingVfx = topRing.GetComponent<VisualEffect>();
        bottomRingVfx = bottomRing.GetComponent<VisualEffect>();

        // Set radius of the rings (each vfx composed of 3 systems/rings)
        SetRingRadii(topRingVfx, ringRadii);
        SetRingRadii(bottomRingVfx, ringRadii);
        
        // Retrieve rays gameobject
        rays = transform.GetChild(2).gameObject;

        // Set the y position of the rays' gameobject
        rays.transform.position = new Vector3(rays.transform.position.x,  middleHeight, rays.transform.position.z);

        // Get rays VFX comppnent
        raysVfx = rays.GetComponent<VisualEffect>();
        raysVfx.SetFloat("ringRadius", baseRadius);

        // Delay spawning rings and rays
        StartCoroutine(DelayRingAndRaysVFX(delayTime)); // Usually I'd use the delay param in the VFX Graph but this conflicts with prewarm times (which are fiddly) so it's easier to delay in script
    }

    IEnumerator DelayRingAndRaysVFX(float delayInSeconds){
        yield return new WaitForSeconds(delayInSeconds); 
        
        topRingVfx.Play(); 
        bottomRingVfx.Play(); 

        topRingVfx.pause = true;
        bottomRingVfx.pause = true;

        // PreWarm rings so they've drawn a full circle when the effect plays
        topRingVfx.Simulate(prewarmTime, prewarmStep);
        bottomRingVfx.Simulate(prewarmTime, prewarmStep);
        
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
            fadeStartTime = 0f;
            if(fadeIn){
                startFadeIn = false;
                startFadeOut = true;
            }
            else{
                startFadeOut = false;
                canBeDestroyed = true;
            }
        }
    }

    void SetRingRadii(VisualEffect ringVfx, Dictionary<string, float> ringRadii){
        foreach(KeyValuePair<string, float> entry in ringRadii){
            ringVfx.SetFloat(entry.Key, entry.Value);
        }
    }
}
