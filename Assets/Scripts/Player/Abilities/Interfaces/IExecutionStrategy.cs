using UnityEngine;

public interface IExecutionStrategy {
    void Execute(Ability data, CastContext ctx, ICastBehaviour behavior);
    void Recast(Ability data, GameObject activeObject, ICastBehaviour behavior);
}
