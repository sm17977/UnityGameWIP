using UnityEngine;
using System.Timers;
using UnityEngine.VFX;
using UnityEditor;
using System;

public class AttackingState : State
{
    public Lux_Player_Controller playerController;
    public VFX_Controller vfxController;
    public GameObject player;
    private Vector3 direction;
    private float currentTime;
    private float lastAttackCount = 0f;
    private float windupTime;
    private float currentAttackCount;
    private VisualEffect newProjectile;
    private float effectTimer;
   

    public AttackingState(Lux_Player_Controller controller, Vector3 dir, GameObject gameObject, float time){
        playerController = controller;
        direction = dir;
        player = gameObject;
        windupTime = time;
    }

    public override void Enter() {
        playerController.canAA = false;
        playerController.isAttacking = true;
        playerController.animator.SetBool("isAttacking", playerController.isAttacking);
            
    }

    public override void Execute() {
      
        if(playerController.timeSinceLastAttack > 0){
            playerController.timeSinceLastAttack -= Time.deltaTime;
        }
    
        // Get animation timings
        GetCurrentAnimationTime();
        
        playerController.isWindingUpText.text = "isWindingUp: " + playerController.isWindingUp;
                
        // Get the direction the abliity should move towards
        Vector3 attackDirection = (playerController.projectileAATargetPosition - player.transform.position).normalized;
        playerController.lux.AA_direction = attackDirection;

   
        // Once the windup window has passed, fire the projectile
        if (IsWindupCompleted() && playerController.canAA) {
        
            // Create projectile
            newProjectile = Lux_Player_Controller.Instantiate(playerController.projectileAA, new Vector3(player.transform.position.x, 1f, player.transform.position.z), Quaternion.LookRotation(direction, Vector3.up));
            vfxController = newProjectile.GetComponent<VFX_Controller>();
            vfxController.player = player;
            vfxController.playerController = playerController;
            playerController.vfxProjectileList.Add(newProjectile.gameObject);
            playerController.canAA = false;
        }
    }

    public override void Exit() {
        playerController.isAttacking = false;
        playerController.animator.SetBool("isAttacking", playerController.isAttacking);
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
