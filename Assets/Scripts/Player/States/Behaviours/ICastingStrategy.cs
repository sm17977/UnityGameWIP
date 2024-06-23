using UnityEngine;

public interface ICastingStrategy {
    void Cast(Ability ability, Vector3 direction, Vector3 abilitySpawnPos, LuxPlayerController playerController);
}
