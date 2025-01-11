using UnityEngine;

public interface ICastingStrategy {
    
    void Cast(Ability ability, Vector3 targetDir, Vector3 targetPos, Vector3 abilitySpawnPo);
    void ReCast(GameObject projectile, string abilityKey);

}
