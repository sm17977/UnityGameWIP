
using System.Collections;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;

public class AttackingState : State
{
    private Lux_Player_Controller playerController;
    private GameObject player;
    private Vector3 direction;
    private float currentTime;
    private float lastAttackCount = 0;
    private float currentAttackCount;
    private float windupTime;
    private string lastAnimationState = "";

    public AttackingState(Lux_Player_Controller controller, Vector3 dir, GameObject gameObject, float time){
        playerController = controller;
        direction = dir;
        player = gameObject;
        windupTime = time;
    }

    public override void Enter() {
        playerController.isAttacking = true;
        playerController.animator.SetBool("isAttacking", true);
        playerController.canAA = true;
    }

    public override void Execute() {
        
        // Get animation timings
        GetCurrentAnimationTime();

        // Rotate to enemy 
        playerController.RotateTowardsTarget(direction);
        playerController.isWindingUpText.text = "isWindingUp: " + playerController.isWindingUp;
                
        // Get the direction the abliity should move towards
        Vector3 attackDirection = (playerController.projectileAATargetPosition - player.transform.position).normalized;
        playerController.qData = (Dictionary<string, object>)playerController.luxAbilityData["Q"];
        playerController.qData["direction"] = attackDirection;

        playerController.RotateTowardsTarget(direction);

        // Once the windup window has passed, fire the projectile
        if (IsWindupCompleted() && playerController.canAA) {
            // Set the spawn position of the projectile
            float worldRadius = playerController.hitboxCollider.radius * playerController.hitboxGameObj.transform.lossyScale.x;
            playerController.projectileAASpawnPos = new Vector3(player.transform.position.x, 1f, player.transform.position.z) + attackDirection * worldRadius;
            
            // Set rotation
            Vector3 eulerAngles = player.transform.rotation.eulerAngles;
            eulerAngles.x = 90;
            Quaternion rotation = Quaternion.Euler(eulerAngles);

            // Create projectile
            GameObject newProjectile = Lux_Player_Controller.Instantiate(playerController.projectileAA, playerController.projectileAASpawnPos, rotation);

            playerController.projectiles.Add(newProjectile);
            Generic_Projectile_Controller projectile_Controller = newProjectile.GetComponent<Generic_Projectile_Controller>();
            projectile_Controller.SetParams((Dictionary<string, object>)playerController.luxAbilityData["Q"]);
            playerController.canAA = false;
        }
    }

    public override void Exit() {
        playerController.isAttacking = false;
        playerController.animator.SetBool("isAttacking", false);
    }

    // Track the windup window
    private bool IsWindupCompleted() {
        return currentTime >= windupTime / 100f;
    }

    // Get the current animation time as a perecentage (start to finish)
    // Track when a new animation loop as started
    private void GetCurrentAnimationTime(){
        AnimatorStateInfo animState = playerController.animator.GetCurrentAnimatorStateInfo(0);

        if(animState.IsName("Attack.Variant Picker")){
            playerController.canAA = true;
        }

        if(animState.IsName("Attack.Attack1") || animState.IsName("Attack.Attack2")){
            currentTime = animState.normalizedTime % 1;
            currentAttackCount = (int)animState.normalizedTime;
        }
    }

}
