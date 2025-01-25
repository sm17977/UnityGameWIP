using System;
using UnityEngine;

public class AoeIndicator : BaseSpellIndicator {
    
    private Transform _cursorIndicator;

    private void Update() {
        var mousePos = InputProcessor.GetMousePosition();
        var centerPos = transform.position; 
        
        mousePos.y = centerPos.y;

        var direction = mousePos - centerPos;
        float distanceFromCenter = direction.magnitude;
        
        if (distanceFromCenter > Ability.range) {
            direction = direction.normalized * Ability.range;
            mousePos = centerPos + direction;
        }
        
        _cursorIndicator.transform.position = new Vector3(
            mousePos.x,
            0.51f,        
            mousePos.z
        );
    }

    public override void InitializeProperties(Ability ability) {
        base.InitializeProperties(ability);
        
        _cursorIndicator = transform.GetChild(0); 
        
        transform.localScale = new Vector3(
            ability.range * 2,
            ability.range * 2,
            1
        );

        _cursorIndicator.localScale = new Vector3(
            (ability.hitboxRadius * 2 / transform.localScale.x),
            (ability.hitboxRadius * 2 / transform.lossyScale.y),
            1
        );
    }
}
