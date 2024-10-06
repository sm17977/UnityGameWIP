using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.VFX;
public class AttackingState : State
{
    public LuxPlayerController playerController;
    public VFXController vfxController;
    public GameObject player;
    private Vector3 direction;
    private float currentTime;
    private float lastAttackCount = 0f;
    private float windupTime;
    private float currentAttackCount;
    private VisualEffect newProjectile;
   

    public AttackingState(LuxPlayerController controller, Vector3 dir, GameObject gameObject, float time){
        playerController = controller;
        direction = dir;
        player = gameObject;
        windupTime = time;
    }

    public override void Enter() {
        playerController.canAA = false;
        playerController.animator.SetTrigger("isAttacking");
        playerController.networkAnimator.SetTrigger("isAttacking");
    }

    public override void Execute() {
      
        if(playerController.timeSinceLastAttack > 0){
            playerController.timeSinceLastAttack -= Time.deltaTime;
        }
    
        // Get animation timings
        GetCurrentAnimationTime();
                
        // Get the direction the abliity should move towards
        Vector3 attackDirection = (playerController.projectileAATargetPosition - player.transform.position).normalized;
        playerController.lux.AA_direction = attackDirection;

   
        // Once the windup window has passed, fire the projectile
        if (IsWindupCompleted() && playerController.canAA) {
        
            // Create Visual Effect instance
            newProjectile = LuxPlayerController.Instantiate(playerController.projectileAA, new Vector3(player.transform.position.x, 1f, player.transform.position.z), Quaternion.LookRotation(direction, Vector3.up));

            vfxController = newProjectile.GetComponent<VFXController>();

            // Set the player and controller references in the VFX Controller 
            vfxController.player = player;
            vfxController.playerController = playerController;

            // Add the VFX projectile to a list so they can be destroyed after VFX is complete
            playerController.vfxProjectileList.Add(newProjectile.gameObject);

            // Prevent player spamming AAs
            playerController.canAA = false;
        }
    }

    public override void Exit() {
    }


    // Track the windup window
    private bool IsWindupCompleted() {
        return currentTime >= windupTime / 100f;
    }

    // Get the current animation time as a perecentage (start to finish)
    // Track when a new animation loop as started
    private void GetCurrentAnimationTime(){
        AnimatorStateInfo animState = playerController.animator.GetCurrentAnimatorStateInfo(0);

        if(animState.IsName("Attack.Attack1") || animState.IsName("Attack.Attack2")){
            currentTime = animState.normalizedTime % 1;
            currentAttackCount = (int)animState.normalizedTime;
        }
    }

}
