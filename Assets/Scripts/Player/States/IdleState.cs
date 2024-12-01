
public class IdleState : State {

    private LuxPlayerController playerController;
    public IdleState(LuxPlayerController controller) {
        playerController = controller;
    }
    
    public override void Enter() {
        playerController.animator.SetTrigger("isIdle");
        playerController.networkAnimator.SetTrigger("isIdle");
    }

    public override void Execute() {

    }

    public override void Exit() {
        playerController.animator.ResetTrigger("isIdle");
        playerController.networkAnimator.ResetTrigger("isIdle");
    }
}
