using UnityEngine;

public class MovingState : State
{
    private Lux_Player_Controller playerController;
    private Vector3 targetLocation;
    private float stoppingDistance;
    private GameObject player;
    private bool movingToAttack;
 

    public MovingState(Lux_Player_Controller controller, Vector3 location, float distance, GameObject gameObject, bool attack){
        playerController = controller;
        targetLocation = location;
        stoppingDistance = distance;
        player = gameObject;
        movingToAttack = attack;
    }

    public override void Enter() {
        playerController.incompleteMovement = true;
        playerController.isRunning = true;
        playerController.animator.SetBool("isRunning", true);
    }

    public override void Execute() {

        // Calculate the direction the player wants to move in
        Vector3 direction = (targetLocation - player.transform.position).normalized;
        direction.y = 0f;

        // Move player towards last mouse click 
        player.transform.Translate(playerController.moveSpeed * Time.deltaTime * direction, Space.World);
        playerController.RotateTowardsTarget(direction);

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

    public override void Exit() {
        playerController.isRunning = false;
        playerController.animator.SetBool("isRunning", false);
    }
}
