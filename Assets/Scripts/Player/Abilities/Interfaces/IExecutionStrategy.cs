using UnityEngine;

public interface IExecutionStrategy {
    void Execute(Ability data, CastContext ctx, ICastBehaviour behavior);
    void Recast(string abilityKey, GameObject activeObject, ICastBehaviour behavior);
}
