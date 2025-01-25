using UnityEngine;
public class BaseSpellIndicator : MonoBehaviour {
    
    protected Ability Ability;
    
    public virtual void InitializeProperties(Ability ability) {
        Ability = ability;
    }
    
    
    
}
