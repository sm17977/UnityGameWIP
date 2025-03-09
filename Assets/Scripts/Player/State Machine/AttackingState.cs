using UnityEngine;
using UnityEngine.VFX;
public class AttackingState : State {

    private GameObject _playerObj;
    private LuxPlayerController _player;
    private InputProcessor _input;
    private ClientAutoAttackController _clientAutoAttackController;

    private Vector3 _direction;
    private float _currentTime;
    private float _lastAttackCount = 0f;
    private float _windupTime;
    private float _currentAttackCount;
    private GameObject _newProjectile;
    
    public AttackingState(GameObject gameObject) {
        _playerObj = gameObject;
        _player = _playerObj.GetComponent<LuxPlayerController>();
        _input = _playerObj.GetComponent<InputProcessor>();
        _direction = _player.direction;
        _windupTime = _player.champion.windupTime;
    }

    public override void Enter() {
        _input.canAA = false;
        _player.animator.SetTrigger("isAttacking");
    }

    public override void Execute() {
      
        if(_player.timeSinceLastAttack > 0){
            _player.timeSinceLastAttack -= Time.deltaTime;
        }
    
        // Get animation timings
        GetCurrentAnimationTime();
                
        // Get the direction the attack projectile should move towards
        Vector3 attackDirection = (_input.projectileAATargetPosition - _playerObj.transform.position).normalized;
        _player.champion.AA_direction = attackDirection;

   
        // Once the windup window has passed, fire the projectile
        if (IsWindupCompleted() && _input.canAA) {
        
            // Create Visual Effect instance
            var startPos = new Vector3(_playerObj.transform.position.x,
                1f, _playerObj.transform.position.z);
            var startRot = Quaternion.LookRotation(_direction, Vector3.up);

            _newProjectile = ClientObjectPool.Instance.GetPooledAutoAttack();
            _newProjectile.SetActive(true);
            _clientAutoAttackController = _newProjectile.GetComponent<ClientAutoAttackController>();

            // Set the player and controller references in the VFX Controller 
            _clientAutoAttackController.player = _playerObj;
            _clientAutoAttackController.playerController = _player;
            _clientAutoAttackController.Initialise(startPos, startRot);
            
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

    // Get the current animation time as a percentage (start to finish)
    // Track when a new animation loop as started
    private void GetCurrentAnimationTime(){
        AnimatorStateInfo animState = _player.animator.GetCurrentAnimatorStateInfo(0);

        if(animState.IsName("Attack.Attack1") || animState.IsName("Attack.Attack2")){
            _currentTime = animState.normalizedTime % 1;
            _currentAttackCount = (int)animState.normalizedTime;
        }
    }

}
