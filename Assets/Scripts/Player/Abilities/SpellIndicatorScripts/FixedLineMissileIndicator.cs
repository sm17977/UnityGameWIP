using System;
using UnityEngine;
public class FixedLineMissileIndicator : BaseSpellIndicator {

    private Transform _spriteChild;
    private SpriteRenderer _spriteRenderer;
    private Vector3 _spriteSize;
    private readonly float aboveFloorY = 0.51f;
    
    private void Awake() {
  
    }
    
    private void Update() {
        Vector3 mousePos = InputProcessor.GetMousePosition();
        
        var xDiff = mousePos.x - transform.position.x;
        var zDiff = mousePos.z - transform.position.z;
        Vector3 dir = new Vector3(xDiff, 0, zDiff).normalized;
            
        Quaternion rotation = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = rotation;
    }
    
    public override void InitializeProperties(Ability ability) {
        base.InitializeProperties(ability);
        
        _spriteChild = transform.GetChild(0); 
        _spriteRenderer = _spriteChild.GetComponent<SpriteRenderer>();
        _spriteSize = _spriteRenderer.bounds.size; 
        
        transform.localScale = new Vector3(
            transform.localScale.x,
            transform.lossyScale.y,
            ability.range / _spriteSize.z
        );
    }
}
