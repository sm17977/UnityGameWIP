
using UnityEngine;

public class AttackingState : State
{
    private Lux_Player_Controller playerController;
    private Vector3 direction;
    public AttackingState(Lux_Player_Controller controller, Vector3 dir){
        playerController = controller;
        direction = dir;
    }

    public override void Enter() {
        
        playerController.isAttacking = true;
        playerController.animator.SetBool("isAttacking", true);
    }

    public override void Execute() {
        playerController.RotateTowardsTarget(direction);

    }

    public override void Exit() {
        playerController.isAttacking = false;
        playerController.animator.SetBool("isAttacking", false);
    }
}
