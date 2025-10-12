using System;
using QFSW.QC;
using UnityEngine;
using UnityEngine.VFX;

public class Lux_R_Aoe : ClientAbilityBehaviour {

    private GameObject _aoePrefab;
    private VisualEffect _aoeVfx;
    private bool _hasPlayed;
    
    private void Start() {
        _aoePrefab = transform.GetChild(0).gameObject;
        _aoeVfx = _aoePrefab.GetComponent<VisualEffect>();
        _aoeVfx.Play();
    }
    
    private void Update() {
        if (_hasPlayed && _aoeVfx.aliveParticleCount == 0) {
            CanBeDestroyed = true;
            _hasPlayed = false;
            Debug.Log("Destroying Lux R AOE (Client)");
            DestroyAbilityPrefab(AbilityPrefabType.Spawn);
        }

        if(_aoeVfx.aliveParticleCount > 0) _hasPlayed = true;
    }

    public override void ResetVFX() {
        if (_aoePrefab != null) {
            _aoeVfx.Stop();
            _aoeVfx.Reinit();
        }
    }
}
