using UnityEngine;

public class TestMovingState : State {
    
    private GameObject _playerObj;
    private BarebonesPlayerController _player;
    private TestInputProcessor _input;
    private NetworkStateManager _networkState;
    
    private Vector3 _targetLocation;
    private float _stoppingDistance;
    private bool _movingToAttack;
    
    public TestMovingState(GameObject gameObject, bool attack) {
        _playerObj = gameObject;
        _player = _playerObj.GetComponent<BarebonesPlayerController>();
        _input = _playerObj.GetComponent<TestInputProcessor>();
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
        
        
        // Calculate the direction the player wants to move in
        Vector3 direction = (_targetLocation - _playerObj.transform.position).normalized;
        direction.y = 0f;
        
        MoveAndRotate(direction);
        
        float dist = Vector3.Distance(_playerObj.transform.position, _targetLocation);
        //Debug.Log("Distance from click: " + dist);
        
        // State exits when player has reached target location
        if (Vector3.Distance(_playerObj.transform.position, _targetLocation) <= _stoppingDistance){
            _input.incompleteMovement = false;

            // If we are walking in range to attack, transition to attack state
            if(_movingToAttack){
                _player.TransitionToAttack();
            }
            // Otherwise transition to idle 
            else{
                _player.TransitionToIdle();
            }
        }   
    }

    void MoveAndRotate(Vector3 direction) {
        // Move player towards last mouse click 
        _playerObj.transform.Translate( 3.8f * Time.deltaTime * direction, Space.World);
        _player.RotateTowardsTarget(direction);
    }

    public override void Exit() {
        _player.animator.SetBool("isRunning", false);
    }
}
