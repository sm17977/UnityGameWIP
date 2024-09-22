public class DeathState : State {

    private readonly LuxPlayerController _playerController;
    public DeathState(LuxPlayerController controller) {
        _playerController = controller;
    }
    
    public override void Enter() {
        _playerController.animator.SetTrigger("isDead");
    }

    public override void Execute() {
        // throw new System.NotImplementedException();
    }

    public override void Exit() {
        // throw new System.NotImplementedException();
    }
}
