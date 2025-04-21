using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;
public class AttackingState : State {

    public bool IsWindingUp;

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
    private Vector3 _cachedEnemyHitboxCenter;
    
    
    public AttackingState(GameObject gameObject) {
        _playerObj = gameObject;
        _player = _playerObj.GetComponent<LuxPlayerController>();
        _input = _playerObj.GetComponent<InputProcessor>();
        _rpc = _playerObj.GetComponent<RPCController>();
        _direction = _player.direction;
        _windupTime = _player.champion.windupTime;
        
        _attackSpeedDelay = 1f / (float)_player.champion.AA_attackSpeed; 
        _attackTimer = 0f;
    }

    public override void Enter() {
        if (_attackTimer == 0) {
            // _player.animator.SetTrigger("isAttacking");
            _player.networkAnimator.SetTrigger("isAttacking");
            _input.canAA = true;
        }
        var enemyController = _player.currentAATarget.GetComponent<LuxPlayerController>();
        _cachedEnemyHitboxCenter = new Vector3(
            enemyController.hitboxGameObj.transform.position.x,
            InputProcessor.floorY,
            enemyController.hitboxGameObj.transform.position.z
        );
    }

    public override void Execute() {
        
        // Trigger the animation again for subsequent attacks
        if(!_player.animator.GetBool("isAttacking"))_player.animator.SetTrigger("isAttacking");
        
        // Get animation timings
        GetCurrentAnimationTime();
        
        if (_attackTimer > 0) {
            _attackTimer -= Time.deltaTime;
            return;
        }
        
        if(_player.timeSinceLastAttack > 0){
            _player.timeSinceLastAttack -= Time.deltaTime;
        }
        
        Vector3 attackDirection = (_player.currentAATarget.transform.position - _playerObj.transform.position).normalized;
   
        // Once the windup window has passed, fire the projectile
        if (IsWindupCompleted() && _input.canAA) {
            
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
        
        Vector3 targetPosition = _input.GetCurrentHitboxEdge();
        if (_input.IsInAttackRange(targetPosition)) {
            _input.canAA = true;
        }
        else {
            Debug.Log("Not in range!");
            _input.canAA = false;
            _player.TransitionToIdle();
        }
    }

    public override void Exit() {
        _attackTimer = 0;
        _player.animator.ResetTrigger("isAttacking");
        
    }

    // Track the windup window
    private bool IsWindupCompleted() {
        return _currentTime >= _windupTime / 100f;
    }
    
    private bool IsPlayingAttackAnimation() {
        AnimatorStateInfo animState = _player.animator.GetCurrentAnimatorStateInfo(0);
        var result = (animState.IsName("Attack.Attack1") || animState.IsName("Attack.Attack2") ||
                animState.IsName("Attack.Variant Picker")) && animState.length >
               animState.normalizedTime;
        Debug.Log("IsPlayingAttackAnimation: " + result);
        return result;
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
                IsWindingUp = false;
            }

            IsWindingUp = true;
        }
        else {
            _currentTime = 0;
            _input.canAA = false;
        }
    }
}
