using Mono.CSharp;
using QFSW.QC;
using UnityEngine;

public class MovingState : State {
    
    private GameObject _playerObj;
    private LuxPlayerController _player;
    private InputProcessor _input;
    private NetworkStateManager _networkState;
    
    private Vector3 _targetLocation;
    private float _stoppingDistance;
    private bool _movingToAttack;
    private int _frameCount = 0; // Add this to track frames

    
    public MovingState(GameObject gameObject, bool attack) {
        _playerObj = gameObject;
        _player = _playerObj.GetComponent<LuxPlayerController>();
        _input = _playerObj.GetComponent<InputProcessor>();
        _networkState = _playerObj.GetComponent<NetworkStateManager>();
        
        _targetLocation = _input.lastClickPosition;
        _stoppingDistance = _input.GetStoppingDistance();
        _movingToAttack = attack;
    }

    public override void Enter() {
        _input.incompleteMovement = true;
        _player.animator.SetBool("isRunning", true);
    }

    public override void Execute() {
        _frameCount++;
        
        if (!_player.canMove.Value) {
            _player.TransitionToIdle();
            return;
        }
        
        // Only if we're moving to attack
        if (_movingToAttack && _player.currentAATarget != null) {
            var currentEdgeDistance = _input.GetCurrentEdgeDistance();
            // Only transition to attack state if within range
            if (currentEdgeDistance < _player.champion.AA_range) {
                _player.TransitionToAttack();
                return;
            }
            
        }
        
        // Calculate the direction the player wants to move in
        Vector3 direction = (_targetLocation - _playerObj.transform.position).normalized;
        direction.y = 0f;
        
        MoveAndRotate(direction);
        
        // Check if we've reached the target location
        if(!_movingToAttack) {
            float distToTarget = Vector3.Distance(_playerObj.transform.position, _targetLocation);
            if(distToTarget <= _stoppingDistance) {
                _input.incompleteMovement = false;
                // If this wasn't an attack click, transition to idle
                _player.TransitionToIdle();
            }
        }
    }

    private void MoveAndRotate(Vector3 direction) {
        float lerpFraction = _networkState.NetworkTimer.MinTimeBetweenTicks / Time.deltaTime;
        // Move player towards last mouse click 
        _playerObj.transform.Translate(_player.movementSpeed.Value * lerpFraction * direction, Space.World);
        _player.RotateTowardsTarget(direction);
    }
    
    public override void Exit() {
        _player.animator.SetBool("isRunning", false);
    }
}
