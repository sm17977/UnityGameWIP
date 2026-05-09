using UnityEngine;

// Defines an ability's casting behaviour (specifically how it spawns and moves)

public interface ICastBehaviour {
    void Cast(Ability data, CastContext context);
    void Recast(GameObject prefab);
}