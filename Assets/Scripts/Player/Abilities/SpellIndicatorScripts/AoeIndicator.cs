using UnityEngine;

public class AoeIndicator : BaseSpellIndicator{
    
    public override void InitializeProperties(Ability ability) {
        base.InitializeProperties(ability);
        
        transform.localScale = new Vector3(
            ability.range * 2,
            ability.range * 2,
            1
        );
    }
}
