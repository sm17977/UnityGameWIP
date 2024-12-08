using QFSW.QC;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class MovingState : State
{
    private LuxPlayerController playerController;
    private Vector3 targetLocation;
    private float stoppingDistance;
    private GameObject player;
    private bool movingToAttack;
 

    public MovingState(LuxPlayerController controller, Vector3 location, float distance, GameObject gameObject, bool attack){
        playerController = controller;
        targetLocation = location;
        stoppingDistance = distance;
        player = gameObject;
        movingToAttack = attack;
    }

    public override void Enter() {
        playerController.incompleteMovement = true;

        AnimatorStateInfo currentAnimatorStateInfo = playerController.animator.GetCurrentAnimatorStateInfo(0);
        if (!currentAnimatorStateInfo.IsName("isRunning")) {
            playerController.animator.SetTrigger("isRunning");
            playerController.networkAnimator.SetTrigger("isRunning");
        }
    }

    public override void Execute() {
        
        if(!playerController.canMove){
            playerController.TransitionToIdle();
        }

        // Calculate the direction the player wants to move in
        Vector3 direction = (targetLocation - player.transform.position).normalized;
        direction.y = 0f;
        
        MoveAndRotate(direction);
        
        float dist = Vector3.Distance(player.transform.position, targetLocation);
        //Debug.Log("Distance from click: " + dist);
        
        // State exits when player has reached target location
        if (Vector3.Distance(player.transform.position, targetLocation) <= stoppingDistance){
            playerController.incompleteMovement = false;

            // If we are walking in range to attack, transition to attack state
            if(movingToAttack){
                playerController.TransitionToAttack();
            }
            // Otherwise transition to idle 
            else{
                playerController.TransitionToIdle();
            }
        }   
    }

    void MoveAndRotate(Vector3 direction) {
        float lerpFraction = playerController._networkTimer.MinTimeBetweenTicks /  Time.deltaTime;
        // Move player towards last mouse click 
        player.transform.Translate(playerController.champion.movementSpeed * lerpFraction * direction, Space.World);
        playerController.RotateTowardsTarget(direction);
    }

    public override void Exit() {
        playerController.animator.ResetTrigger("isRunning");
        playerController.networkAnimator.ResetTrigger("isRunning");
        // playerController.animator.SetTrigger("isIdle");
        // playerController.networkAnimator.SetTrigger("isIdle");
    }
}
