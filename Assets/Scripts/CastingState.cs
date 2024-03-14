using UnityEngine;
using System.Collections.Generic;
using System.ComponentModel;
public class CastingState : State
{

    public GameObject player;
    private Lux_Player_Controller playerController;
    public Ability ability;

    public CastingState (Lux_Player_Controller controller,  GameObject gameObject, Ability ability){
        playerController = controller;
        player = gameObject;
        this.ability = ability;
    }

    public override void Enter() {
        ability.PutOnCooldown();
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
  
        // Rotate the player in the direction the spell was cast
        playerController.RotateTowardsTarget(direction);
        

        if (playerController.canCast) {

            // Set the spawn position of the projectile
            float worldRadius = playerController.hitboxCollider.radius * playerController.hitboxGameObj.transform.lossyScale.x;
            Vector3 abilitySpawnPos = new Vector3(player.transform.position.x, 1f, player.transform.position.z) + direction * worldRadius;
            
            // Create projectile
            GameObject newProjectile = Lux_Player_Controller.Instantiate(ability.missile, abilitySpawnPos,  Quaternion.LookRotation(direction, Vector3.up));

            // Store projectile in list
            playerController.projectiles.Add(newProjectile);

            // Assign properties using interface
            IProjectileAbility projectileScript = newProjectile.GetComponent<IProjectileAbility>();
            projectileScript?.InitializeProjectile(direction, ability.speed, ability.range);
          
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
