
using UnityEngine;

public class IdleState : State {

    private LuxPlayerController _playerController;
    public IdleState(LuxPlayerController controller) {
        _playerController = controller;
    }

    public IdleState() {
        
    }
    
    public override void Enter() {
        _playerController.SetAnimTrigger("isIdle");
    }

    public override void Execute() {

    }

    public override void Exit() {
        _playerController.ResetAnimTrigger("isIdle");
    }
}
