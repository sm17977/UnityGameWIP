using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;
public class AttackingState : State {

    private GameObject _playerObj;
    private LuxPlayerController _player;
    private InputProcessor _input;
    private ClientAutoAttackController _autoAttackController;
    private RPCController _rpc;

    private Vector3 _direction;
    private float _currentTime;
    private float _lastAttackCount = 0f;
    private float _windupTime;
    private float _currentAttackCount;
    private GameObject _newAutoAttack;
    private float _attackSpeedDelay;
    private float _attackTimer;
    private bool _firstAttack;
    
    public AttackingState(GameObject gameObject) {
        _playerObj = gameObject;
        _player = _playerObj.GetComponent<LuxPlayerController>();
        _input = _playerObj.GetComponent<InputProcessor>();
        _rpc = _playerObj.GetComponent<RPCController>();
        _direction = _player.direction;
        _windupTime = _player.champion.windupTime;
        
        _attackSpeedDelay = 1f / (float)_player.champion.AA_attackSpeed; // Convert attacks per second to delay
        _attackTimer = 0f;
    }

    public override void Enter() {
        _firstAttack = true;
        if (_attackTimer == 0) {
            _player.animator.SetTrigger("isAttacking");
            _input.canAA = true;
        }
    }

    public override void Execute() {
        
        if (_attackTimer > 0) {
            _attackTimer -= Time.deltaTime;
            return;
        }
        
        // Get animation timings
        GetCurrentAnimationTime();
        
        if(_player.timeSinceLastAttack > 0){
            _player.timeSinceLastAttack -= Time.deltaTime;
        }
                
        // Get the direction the attack projectile should move towards
        // Vector3 attackDirection = (_input.projectileAATargetPosition - _playerObj.transform.position).normalized;
        // _player.champion.AA_direction = attackDirection;
        Vector3 attackDirection = (_player.currentAATarget.transform.position - _playerObj.transform.position).normalized;
   
        // Once the windup window has passed, fire the projectile
        if (IsWindupCompleted() && (_input.canAA || _firstAttack)) {
            if(!IsPlayingAttackAnimation()) _player.animator.SetTrigger("isAttacking");
            Debug.Log("Attacking");
        
            // Get start position and rotation
            var startPos = new Vector3(_playerObj.transform.position.x,
                1f, _playerObj.transform.position.z);
            var startRot = Quaternion.LookRotation(attackDirection, Vector3.up);

            // Spawn client side auto attack
            _newAutoAttack = ClientObjectPool.Instance.GetPooledAutoAttack();
            _newAutoAttack.SetActive(true);
            _autoAttackController = _newAutoAttack.GetComponent<ClientAutoAttackController>();
            _autoAttackController.Initialise(_playerObj, startPos, startRot, attackDirection);
            
            // Spawn server side auto attack
            var targetNetworkRef = _player.currentAATarget.GetComponent<NetworkObject>();
            _rpc.SpawnAAServerRpc(targetNetworkRef, startPos, startRot);
            
            // Prevent player spamming AAs with attack speed delay
            _attackTimer = _attackSpeedDelay; 
        }
        //
        Vector3 targetPosition = _input.GetCurrentHitboxEdge();
        if (_input.IsInAttackRange(targetPosition)) {
            _input.canAA = true;
        }
        else {
            _input.canAA = false;
        }

        _firstAttack = false;
    }

    public override void Exit() {
        _attackTimer = 0;
    }


    // Track the windup window
    private bool IsWindupCompleted() {
        return _currentTime >= _windupTime / 100f;
    }
    
    private bool IsPlayingAttackAnimation() {
        AnimatorStateInfo animState = _player.animator.GetCurrentAnimatorStateInfo(0);
        return (animState.IsName("Attack.Attack1") || animState.IsName("Attack.Attack2") ||
                animState.IsName("Attack.Variant Picker")) && animState.length >
               animState.normalizedTime;
    }

    // Get the current animation time as a percentage (start to finish)
    // Track when a new animation loop as started
    private void GetCurrentAnimationTime(){
        AnimatorStateInfo animState = _player.animator.GetCurrentAnimatorStateInfo(0);

        if(animState.IsName("Attack.Attack1") || animState.IsName("Attack.Attack2")){
            _currentTime = animState.normalizedTime % 1;
            _currentAttackCount = (int)animState.normalizedTime;
            
            // Need this check to prevent extra attacking spawning while in the variant picker state
            if (_currentTime >= 0.99f) {
                _input.canAA = false;
            }
        }
        else {
            _currentTime = 0;
            _input.canAA = false;
        }
    }
}
