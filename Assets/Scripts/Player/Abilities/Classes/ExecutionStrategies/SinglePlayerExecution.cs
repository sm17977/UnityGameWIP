using UnityEngine;

public class SinglePlayerExecution : IExecutionStrategy {
    
    private readonly LuxPlayerController _playerController;
    
    public void Execute(Ability data, CastContext ctx, ICastBehaviour behavior)
        => behavior.Cast(data, ctx, _playerController);

    public void Recast(Ability data, GameObject activeObject, ICastBehaviour behavior)
        => behavior.Recast(data, activeObject);
}