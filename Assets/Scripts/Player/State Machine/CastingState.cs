using UnityEngine;

public class CastingState : State {

    private readonly GameObject _playerObj;
    private readonly LuxPlayerController _playerController;
    private readonly InputProcessor _input;
    private readonly Ability _ability;
    
    private readonly bool _isRecast;
    private bool _castingFinished;

    public CastingState (GameObject gameObject, Ability ability, bool isRecast = false){
        _playerObj = gameObject;
        _playerController = _playerObj.GetComponent<LuxPlayerController>();
        _input = _playerObj.GetComponent<InputProcessor>();
        _ability = ability;
        _isRecast = isRecast;
    }

    public override void Enter() {

        if (_isRecast) {
            if (_playerController.ActiveAbilityPrefabs.TryGetValue(_ability.key, out var projectile)) {
                _ability.Recast(projectile);
            }
            _playerController.TransitionToIdle();
            return;
        }
        
        _ability.PutOnCooldown();
        _input.canCast = true;
        //_player.animator.SetTrigger(_ability.animationTrigger);
        
        if (GlobalState.IsMultiplayer) {
            _ability.PutOnCooldown_Net(_playerObj);
            Debug.Log("SetAnimTrigger");
            _playerController.SetAnimTrigger(_ability.animationTrigger);
        }
    }

    public override void Execute() {

        // Track when animation ends
        if(_playerController.animator.GetCurrentAnimatorClipInfo(0).Length > 0){
            _castingFinished = _playerController.animator.GetCurrentAnimatorClipInfo(0)[0].clip.name == _ability.animationClip.name && 
                _playerController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.95; // This is set to 0.95 because the normalized time doesn't always reach 100%
        }
       
        // Get the direction the ability should move towards
        Vector3 direction = (_input.abilityTargetPosition - _playerObj.transform.position).normalized;
  
        // Rotate the player in the direction the spell was cast
        _playerController.RotateTowardsTarget(direction);
        
        
        if (_input.canCast) {
            
            // Set the spawn position of the projectile
            float worldRadius = _playerController.hitboxColliderRadius * _playerController.hitboxGameObj.transform.lossyScale.x;
            Vector3 abilitySpawnPos = new Vector3(_playerObj.transform.position.x, _ability.spawnHeight, _playerObj.transform.position.z) + direction * worldRadius;
            
            var ctx = new CastContext {
                Direction = direction,
                TargetPos = _input.abilityTargetPosition,
                SpawnPos = abilitySpawnPos,
                SourcePlayer = null,
                PlayerController = _playerController
            };

            _ability.Cast(ctx);
            _input.canCast = false;
        }
        
        if(_castingFinished){
            
            _playerController.networkAnimator.ResetTrigger(_ability.animationTrigger);
            
            // Finish any incomplete movement commands
            if(_input.incompleteMovement){
                _playerController.TransitionToMove();
            }
            // Return to attacking
            else if(_input.isAttackClick){
                _playerController.TransitionToAttack();
            }
            // Default to idle
            else{
                _playerController.TransitionToIdle();
            }
        }
    }
    
    public override void Exit() {
        _playerController.networkAnimator.ResetTrigger(_ability.animationTrigger);
    }
}
