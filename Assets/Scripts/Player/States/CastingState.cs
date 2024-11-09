using Unity.Netcode;
using UnityEngine;

public class CastingState : State
{

    public GameObject player;
    private LuxPlayerController playerController;
    public Ability ability;
    public bool castingFinished = false; 

    public CastingState (LuxPlayerController controller,  GameObject gameObject, Ability ability){
        playerController = controller;
        player = gameObject;
        this.ability = ability;
        Debug.Log("Casting State Ability - " + this.ability.buff.id);
    }

    public override void Enter() {
        ability.PutOnCooldown();
        playerController.canCast = true;
        playerController.animator.SetTrigger(ability.animationTrigger);
        
        if (GlobalState.IsMultiplayer) {
            ability.PutOnCooldown_Net(player);
            playerController.networkAnimator.SetTrigger(ability.animationTrigger);
        }
    }

    public override void Execute() {

        // Track when animation ends
        if(playerController.animator.GetCurrentAnimatorClipInfo(0).Length > 0){
            castingFinished = playerController.animator.GetCurrentAnimatorClipInfo(0)[0].clip.name == ability.animationClip.name && 
                playerController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.95; // This is set to 0.95 because the normalized time doesn't always reach 100%
        }
       
        // Get the direction the abliity should move towards
        Vector3 direction = (playerController.projectileTargetPosition - player.transform.position).normalized;
  
        // Rotate the player in the direction the spell was cast
        playerController.RotateTowardsTarget(direction);
        
        
        if (playerController.canCast) {
            
            // Set the spawn position of the projectile
            float worldRadius = playerController.hitboxCollider.radius * playerController.hitboxGameObj.transform.lossyScale.x;
            Vector3 abilitySpawnPos = new Vector3(player.transform.position.x, ability.spawnHeight, player.transform.position.z) + direction * worldRadius;
            
            ability.Cast(direction, abilitySpawnPos);
          
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
    }
}
