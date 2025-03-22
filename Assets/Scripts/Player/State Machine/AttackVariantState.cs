using UnityEngine;

public class AnimTest : StateMachineBehaviour {
    private LuxPlayerController _player;
    private InputProcessor _input;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex){
        if (_input == null) _input = animator.GetComponent<InputProcessor>();
        if (_player == null) _player = animator.GetComponent<LuxPlayerController>();
        // _input.canAA = true;
    }
    
    //OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex){
        _player.RotateTowardsTarget(_player.direction);
    }
}
