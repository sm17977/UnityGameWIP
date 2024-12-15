using UnityEngine;
using UnityEngine.VFX;
public class AttackingState : State {

    private GameObject _playerObj;
    private LuxPlayerController _player;
    private InputProcessor _input;
    private VFXController _vfxController;

    private Vector3 _direction;
    private float _currentTime;
    private float _lastAttackCount = 0f;
    private float _windupTime;
    private float _currentAttackCount;
    private VisualEffect _newProjectile;
    
    public AttackingState(GameObject gameObject) {
        _playerObj = gameObject;
        _player = _playerObj.GetComponent<LuxPlayerController>();
        _input = _playerObj.GetComponent<InputProcessor>();
        _direction = _player.direction;
        _playerObj = gameObject;
        _windupTime = _player.champion.windupTime;
    }

    public override void Enter() {
        _input.canAA = false;
        _player.animator.SetTrigger("isAttacking");
        _player.networkAnimator.SetTrigger("isAttacking");
    }

    public override void Execute() {
      
        if(_player.timeSinceLastAttack > 0){
            _player.timeSinceLastAttack -= Time.deltaTime;
        }
    
        // Get animation timings
        GetCurrentAnimationTime();
                
        // Get the direction the abliity should move towards
        Vector3 attackDirection = (_input.projectileAATargetPosition - _playerObj.transform.position).normalized;
        _player.champion.AA_direction = attackDirection;

   
        // Once the windup window has passed, fire the projectile
        if (IsWindupCompleted() && _input.canAA) {
        
            // Create Visual Effect instance
            _newProjectile = LuxPlayerController.Instantiate(_player.projectileAA, new Vector3(_playerObj.transform.position.x, 1f, _playerObj.transform.position.z), Quaternion.LookRotation(_direction, Vector3.up));

            _vfxController = _newProjectile.GetComponent<VFXController>();

            // Set the player and controller references in the VFX Controller 
            _vfxController.player = _playerObj;
            _vfxController.playerController = _player;

            // Add the VFX projectile to a list so they can be destroyed after VFX is complete
            _player.vfxProjectileList.Add(_newProjectile.gameObject);

            // Prevent player spamming AAs
            _input.canAA = false;
        }
    }

    public override void Exit() {
    }


    // Track the windup window
    private bool IsWindupCompleted() {
        return _currentTime >= _windupTime / 100f;
    }

    // Get the current animation time as a perecentage (start to finish)
    // Track when a new animation loop as started
    private void GetCurrentAnimationTime(){
        AnimatorStateInfo animState = _player.animator.GetCurrentAnimatorStateInfo(0);

        if(animState.IsName("Attack.Attack1") || animState.IsName("Attack.Attack2")){
            _currentTime = animState.normalizedTime % 1;
            _currentAttackCount = (int)animState.normalizedTime;
        }
    }

}
