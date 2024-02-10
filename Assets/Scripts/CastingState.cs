using UnityEngine;
using System.Collections.Generic;
public class CastingState : State
{

    private Lux_Player_Controller playerController;
    public GameObject player;

    public CastingState (Lux_Player_Controller controller,  GameObject gameObject){
        playerController = controller;
        player = gameObject;
    }

    public override void Enter() {
        playerController.isCasting = true;
        playerController.canCast = true;
        playerController.animator.SetBool("isQCast", playerController.isCasting);
    }

    public override void Execute() {

        // Track when animation ends
        bool castingFinished = playerController.animator.GetCurrentAnimatorStateInfo(0).IsName("Q_Ability") && 
            playerController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1;

        // Get the direction the abliity should move towards
        Vector3 direction = (playerController.projectileTargetPosition - player.transform.position).normalized;
        playerController.lux.Q_direction = direction;

        // Rotate the player in the direction the spell was cast
        playerController.RotateTowardsTarget(direction);

        if (playerController.canCast) {
            // Set the spawn position of the projectile
            float worldRadius = playerController.hitboxCollider.radius * playerController.hitboxGameObj.transform.lossyScale.x;
            playerController.projectileSpawnPos = new Vector3(player.transform.position.x, 1f, player.transform.position.z) + direction * worldRadius;
            
            // Create projectile
            GameObject newProjectile = Lux_Player_Controller.Instantiate(playerController.projectile, playerController.projectileSpawnPos,  Quaternion.LookRotation(direction, Vector3.up));

            // Store projectile in list
            playerController.projectiles.Add(newProjectile);

            Generic_Projectile_Controller projectileController = newProjectile.GetComponent<Generic_Projectile_Controller>();
            projectileController.missile_direction = direction;
            projectileController.missile_speed = playerController.lux.Q_speed;
            projectileController.missile_range = playerController.lux.Q_range;

            playerController.canCast = false;
        }
        
        if(castingFinished){
            // Finish any incomplete movement commands
            if(playerController.incompleteMovement){
                playerController.TransitionToMove();
            }
            // Return to attacking
            else if(playerController.isAttackClick){
                playerController.TransitionToAttack();
            }
            // Deafult to idle
            else{
                playerController.TransitionToIdle();
            }
        }
    }

    public override void Exit() {
        playerController.isCasting = false;
        playerController.animator.SetBool("isQCast", playerController.isCasting);
    }
}
