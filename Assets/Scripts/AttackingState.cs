
using System.Collections;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;

public class AttackingState : State
{
    private Lux_Player_Controller playerController;
    private Vector3 direction;
    private float currentTime;

    public AttackingState(Lux_Player_Controller controller, Vector3 dir){
        playerController = controller;
        direction = dir;
    }

    public override void Enter() {
        playerController.isAttacking = true;
        playerController.animator.SetBool("isAttacking", true);
    }

    public override void Execute() {
        playerController.isWindingUpText.text = "isWindingUp: " + playerController.isWindingUp;
        playerController.RotateTowardsTarget(direction);
        GetCurrentAnimaionTime();
        CalculateWindUp();
    }

    public override void Exit() {
        playerController.isAttacking = false;
        playerController.animator.SetBool("isAttacking", false);
    }

    // Get the current animation time as a perecentage (start to finish)
    private void GetCurrentAnimaionTime(){
        AnimatorStateInfo animState = playerController.animator.GetCurrentAnimatorStateInfo(0);
        if(animState.IsName(playerController.attackAnimState)){
            currentTime = animState.normalizedTime % 1;
            Debug.Log("Current Time: " + currentTime );
        }
    }

    // Define the period of the animation that is the attack windup
    private void CalculateWindUp(){
        if(currentTime * 100 > playerController.windupTime){
            playerController.isWindingUp = false;
        }
        else{
            playerController.isWindingUp = true;
        }
    }
}
