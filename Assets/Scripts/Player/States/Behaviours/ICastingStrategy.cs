using UnityEngine;

public interface ICastingStrategy {
    
    void Cast(Ability ability, Vector3 direction, Vector3 abilitySpawnPo);
    void ReCast(GameObject projectile, string abilityKey);

}
