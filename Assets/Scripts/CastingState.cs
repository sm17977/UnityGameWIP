using UnityEngine;
using System.Collections.Generic;
public class CastingState : State
{

    private Lux_Player_Controller playerController;
    private GameObject player;

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
        playerController.qData = (Dictionary<string, object>)playerController.luxAbilityData["Q"];
        playerController.qData["direction"] = direction;

        playerController.RotateTowardsTarget(direction);

        if (playerController.canCast) {
            // Set the spawn position of the projectile
            float worldRadius = playerController.hitboxCollider.radius * playerController.hitboxGameObj.transform.lossyScale.x;
            playerController.projectileSpawnPos = new Vector3(player.transform.position.x, 0.51f, player.transform.position.z) + direction * worldRadius;
            
            // Set rotation
            Vector3 eulerAngles = player.transform.rotation.eulerAngles;
            eulerAngles.x = 90;
            Quaternion rotation = Quaternion.Euler(eulerAngles);

            // Create projectile
            GameObject newProjectile = Lux_Player_Controller.Instantiate(playerController.projectile, playerController.projectileSpawnPos, rotation);
            playerController.projectiles.Add(newProjectile);
            Generic_Projectile_Controller projectile_Controller = newProjectile.GetComponent<Generic_Projectile_Controller>();
            projectile_Controller.SetParams((Dictionary<string, object>)playerController.luxAbilityData["Q"]);
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
