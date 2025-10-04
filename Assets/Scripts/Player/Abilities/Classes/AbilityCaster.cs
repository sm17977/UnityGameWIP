using UnityEngine;


public class AbilityCaster {
    private readonly ICastBehaviour _behavior;
    private readonly IExecutionStrategy _execution;
    private readonly LuxPlayerController _playerController;

    public AbilityCaster(ICastBehaviour behavior, IExecutionStrategy execution, LuxPlayerController playerController) {
        _behavior = behavior;
        _execution = execution;
        _playerController = playerController;
    }

    public void Cast(Ability ability, Vector3 direction, Vector3 targetPos, Vector3 abilitySpawnPos) {
        var ctx = new CastContext {
            Direction = direction,
            TargetPos = targetPos,
            SpawnPos = abilitySpawnPos,
            SourcePlayer = null,
            PlayerController = _playerController
        };
        _execution.Execute(ability, ctx, _behavior);
    }

    public void Recast(GameObject activeObject, string key) {
        _execution.Recast(null, activeObject, _behavior);
    }
}
