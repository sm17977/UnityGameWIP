using UnityEngine;

public class CastingState : State {

    private GameObject _playerObj;
    private LuxPlayerController _player;
    private InputProcessor _input;
    
    private Ability _ability;
    private bool _castingFinished;
    private bool _isRecast;

    public CastingState (GameObject gameObject, Ability ability, bool isRecast = false){
        _playerObj = gameObject;
        _player = _playerObj.GetComponent<LuxPlayerController>();
        _input = _playerObj.GetComponent<InputProcessor>();
        _ability = ability;
        _isRecast = isRecast;
    }

    public override void Enter() {

        if (_isRecast) {
            if (_player.ActiveAbilityPrefabs.TryGetValue(_ability.key, out var projectile)) {
                _ability.Recast(projectile);
            }
            _player.TransitionToIdle();
            return;
        }
        
        _ability.PutOnCooldown();
        _input.canCast = true;
        _player.animator.SetTrigger(_ability.animationTrigger);
        
        if (GlobalState.IsMultiplayer) {
            _ability.PutOnCooldown_Net(_playerObj);
            _player.networkAnimator.SetTrigger(_ability.animationTrigger);
        }
    }

    public override void Execute() {

        // Track when animation ends
        if(_player.animator.GetCurrentAnimatorClipInfo(0).Length > 0){
            _castingFinished = _player.animator.GetCurrentAnimatorClipInfo(0)[0].clip.name == _ability.animationClip.name && 
                _player.animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.95; // This is set to 0.95 because the normalized time doesn't always reach 100%
        }
       
        // Get the direction the ability should move towards
        Vector3 direction = (_input.abilityTargetPosition - _playerObj.transform.position).normalized;
  
        // Rotate the player in the direction the spell was cast
        _player.RotateTowardsTarget(direction);
        
        
        if (_input.canCast) {
            
            // Set the spawn position of the projectile
            float worldRadius = _player.hitboxColliderRadius * _player.hitboxGameObj.transform.lossyScale.x;
            Vector3 abilitySpawnPos = new Vector3(_playerObj.transform.position.x, _ability.spawnHeight, _playerObj.transform.position.z) + direction * worldRadius;

            _ability.Cast(direction, _input.abilityTargetPosition, abilitySpawnPos);
          
            _input.canCast = false;
        }
        
        if(_castingFinished){
            // Finish any incomplete movement commands
            if(_input.incompleteMovement){
                _player.TransitionToMove();
            }
            // Return to attacking
            else if(_input.isAttackClick){
                _player.TransitionToAttack();
            }
            // Default to idle
            else{
                _player.TransitionToIdle();
            }
        }
    }
    
    public override void Exit() {
    }
}
