using QFSW.QC;
using Unity.Netcode;
using Unity.VisualScripting;
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

     
        if(!playerController.canMove){
            playerController.TransitionToIdle();
        }

        // Calculate the direction the player wants to move in
        Vector3 direction = (targetLocation - player.transform.position).normalized;
        direction.y = 0f;

        // if(playerController.globalState.currentScene == "Multiplayer" && playerController.IsOwner){
        //    playerController.SendCommandToServerRpc(direction);
        // }
       
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

    void MoveAndRotate(Vector3 direction){
        // Move player towards last mouse click 
        player.transform.Translate(playerController.lux.movementSpeed * Time.deltaTime * direction, Space.World);
        playerController.RotateTowardsTarget(direction);
    }

    public override void Exit() {
        playerController.isRunning = false;
        playerController.animator.SetBool("isRunning", false);
    }
}
