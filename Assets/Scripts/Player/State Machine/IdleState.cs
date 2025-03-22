
using UnityEngine;

public class IdleState : State {

    private LuxPlayerController _playerController;
    public IdleState(LuxPlayerController controller) {
        _playerController = controller;
    }

    public IdleState() {
        
    }
    
    public override void Enter() {
        Debug.Log("Idle State - entering");
        _playerController.animator.SetTrigger("isIdle");
    }

    public override void Execute() {

    }

    public override void Exit() {
 
    }
}
