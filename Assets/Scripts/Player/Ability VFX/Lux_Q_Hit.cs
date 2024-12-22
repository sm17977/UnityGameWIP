
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.VFX;

public class Lux_Q_Hit : ProjectileAbility {
    // Target hit
    public GameObject target;
 
    // Ring Radii
    private float _baseRadius = 0.4f;
    private Dictionary<string, float> _ringRadii = new Dictionary<string, float>(){
        {"ringRadius1", 0.4f},
        {"ringRadius2", 0.45f},
        {"ringRadius3", 0.5f}    
    };

    // Top Ring 
    private VisualEffect _topRingVfx;
    private GameObject _topRing;
    private float _topHeight = 0.5f;

    // Bottom Ring
    private VisualEffect _bottomRingVfx;
    private GameObject _bottomRing;
    private float _bottomHeight = 0.5f;

    // Rays
    private VisualEffect _raysVfx;
    private GameObject _rays;
    private float _middleHeight;

    // Fade Rings
    private float _fadeStartTime = 0f;
    private float _innerRingAlpha = 0.2f;
    private float _outerRingAlpha = 0.1f;
    private bool _startFadeIn = false;
    private bool _startFadeOut = false;
    
    // Enemy 
    private float _enemyHeight;
    private CapsuleCollider _enemyCollider;

    // Timing
    // Minimum VFX and stun duration is fade in time + fade out time + delay time
    private float _prewarmTime = 0.02f;
    private uint _prewarmStep = 50;
    private float _delayTime = 0.3f;
    private float _fadeDuration = 0.3f;
    private float _stunDuration;
    public float totalDuration;
    private float _timer;
    private bool _vfxPlaying = true;

    private void Start(){
        CalculateVfxDuration();
        _timer = totalDuration;
        InitVfx();
    }

    public void SetAbility(Ability abilityToSet) {
        ability = abilityToSet;
    }

    private void CalculateVfxDuration(){
        _stunDuration = ability.buff.Duration;
        
        // If the stun duration is longer than the minimum stun time, add the difference to totalDuration
        totalDuration = (_fadeDuration * 2f) + _delayTime;
        if (totalDuration < _stunDuration){
            float diff = _stunDuration - totalDuration;
            totalDuration += diff;
        }
    }

    private void Update(){

        // Start timer after rings have prewarmed
        if(_vfxPlaying && _timer > 0){
            _timer -= Time.deltaTime;
        }

        // Fade out rings
        if(_startFadeOut){
            if(_timer <= 0){
                Fade(false, _fadeDuration);
            }
        }   

        // Fade in rings
        if(_startFadeIn){
            Fade(true, _fadeDuration);
        }
    }

    private void InitVfx(){
        _vfxPlaying = true;

        // Calculate the y position to spawn the 2 rings and rays using the target's capsule collider
        _enemyCollider = target.GetComponent<CapsuleCollider>();
        _enemyHeight = _enemyCollider.height - _enemyCollider.radius;
        _bottomHeight = target.transform.position.y + 0.001f;
        _topHeight = _bottomHeight + _enemyHeight;
        _middleHeight = _topHeight / 2;

        // Retrieve the rings gameobjects
        _topRing = transform.GetChild(0).gameObject;
        _bottomRing = transform.GetChild(1).gameObject;

        // Set the y position of the rings gameobject
        _topRing.transform.position = new Vector3(_topRing.transform.position.x, _topHeight, _topRing.transform.position.z);
        _bottomRing.transform.position = new Vector3(_bottomRing.transform.position.x,  _bottomHeight, _bottomRing.transform.position.z);

        // Get the VFX components of the rings
        _topRingVfx = _topRing.GetComponent<VisualEffect>();
        _bottomRingVfx = _bottomRing.GetComponent<VisualEffect>();

        // Set radius of the rings (each vfx composed of 3 systems/rings)
        SetRingRadii(_topRingVfx, _ringRadii);
        SetRingRadii(_bottomRingVfx, _ringRadii);
        
        // Retrieve rays gameobject
        _rays = transform.GetChild(2).gameObject;

        // Set the y position of the rays' gameobject
        _rays.transform.position = new Vector3(_rays.transform.position.x,  _middleHeight, _rays.transform.position.z);

        // Get rays VFX component
        _raysVfx = _rays.GetComponent<VisualEffect>();
        _raysVfx.SetFloat("ringRadius", _baseRadius);

        // Delay spawning rings and rays
        StartCoroutine(DelayRingAndRaysVFX(_delayTime)); // Usually I'd use the delay param in the VFX Graph but this conflicts with prewarm times (which are fiddly) so it's easier to delay in script
    }

    IEnumerator DelayRingAndRaysVFX(float delayInSeconds){
        yield return new WaitForSeconds(delayInSeconds);
        
        _topRingVfx.Play(); 
        _bottomRingVfx.Play(); 

        _topRingVfx.pause = true;
        _bottomRingVfx.pause = true;

        // PreWarm rings so they've drawn a full circle when the effect plays
        _topRingVfx.Simulate(_prewarmTime, _prewarmStep);
        _bottomRingVfx.Simulate(_prewarmTime, _prewarmStep);
        
        _topRingVfx.pause = false;
        _bottomRingVfx.pause = false;

        _startFadeIn = true;
   
        // Play rays
        _raysVfx.Play();
    }

    private void Fade(bool fadeIn, float fadeDuration){
        // Set start time for fading out
        if(_fadeStartTime == 0){
            _fadeStartTime = Time.time;
        }

         // Get time since fade out started
        float elapsedTime = Time.time - _fadeStartTime;
        // Clamp between 0 and 1
        float normalizedTime = Mathf.Clamp01(elapsedTime / fadeDuration);

        // Set fade to true to allow updating the Set Alpha node
        _topRingVfx.SetBool("fade", true);
        _bottomRingVfx.SetBool("fade", true);
        
        // Fade out 
        if(!fadeIn){
            _topRingVfx.SetFloat("fadeFromInner", _innerRingAlpha);
            _topRingVfx.SetFloat("fadeFromOuter", _outerRingAlpha);
            _topRingVfx.SetFloat("fadeToInner", 0f);
            _topRingVfx.SetFloat("fadeToOuter", 0f);
            _bottomRingVfx.SetFloat("fadeFromInner", _innerRingAlpha);
            _bottomRingVfx.SetFloat("fadeFromOuter", _outerRingAlpha);
            _bottomRingVfx.SetFloat("fadeToInner", 0f);
            _bottomRingVfx.SetFloat("fadeToOuter", 0f);
        }
        // Fade in
        else{
            _topRingVfx.SetFloat("fadeFromInner", 0f);
            _topRingVfx.SetFloat("fadeFromOuter", 0f);
            _topRingVfx.SetFloat("fadeToInner", _innerRingAlpha);
            _topRingVfx.SetFloat("fadeToOuter", _outerRingAlpha);
            _bottomRingVfx.SetFloat("fadeFromInner", 0f);
            _bottomRingVfx.SetFloat("fadeFromOuter", 0f);
            _bottomRingVfx.SetFloat("fadeToInner", _innerRingAlpha);
            _bottomRingVfx.SetFloat("fadeToOuter", _outerRingAlpha);
        }

        // Set the Lerp node amount using the normalized time
        if(elapsedTime <= fadeDuration){
            _topRingVfx.SetFloat("fadeTime", normalizedTime);
            _bottomRingVfx.SetFloat("fadeTime", normalizedTime);
        }
        // Reset start time when fade has finished
        else{
            _fadeStartTime = 0f;
            if(fadeIn){
                _startFadeIn = false;
                _startFadeOut = true;
            }
            else{
                _startFadeOut = false;
                canBeDestroyed = true;
                if (GlobalState.IsMultiplayer) {
                    ClientObjectPool.Instance.ReturnObjectToPool(ability, AbilityPrefabType.Hit, gameObject);
                }
            }
        }
    }

    private void SetRingRadii(VisualEffect ringVfx, Dictionary<string, float> ringRadii){
        foreach(KeyValuePair<string, float> entry in ringRadii){
            ringVfx.SetFloat(entry.Key, entry.Value);
        }
    }

    public override void ResetVFX() {
        Start();
    }
}
