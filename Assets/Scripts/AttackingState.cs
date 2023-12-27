
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

    public AttackingState(Lux_Player_Controller controller, Vector3 dir, GameObject gameObject){
        playerController = controller;
        direction = dir;
        player = gameObject;
    }

    public override void Enter() {
        playerController.isAttacking = true;
        playerController.animator.SetBool("isAttacking", true);
        playerController.canAA = true;
    }

    public override void Execute() {

        // Rotate to enemy 
        playerController.RotateTowardsTarget(direction);
        playerController.isWindingUpText.text = "isWindingUp: " + playerController.isWindingUp;
        
        // Get animation timings
        GetCurrentAnimaionTime();
        CalculateWindUp();

        // Check if a new attack has started
        if (currentAttackCount > lastAttackCount){
            Debug.Log("Animation looped");
            playerController.canAA = true;
            lastAttackCount = currentAttackCount;
        }
        
        // Get the direction the abliity should move towards
        Vector3 attackDirection = (playerController.projectileAATargetPosition - player.transform.position).normalized;
        playerController.qData = (Dictionary<string, object>)playerController.luxAbilityData["Q"];
        playerController.qData["direction"] = attackDirection;

        playerController.RotateTowardsTarget(direction);

        if (!playerController.isWindingUp && playerController.canAA) {
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

    // Get the current animation time as a perecentage (start to finish)
    private void GetCurrentAnimaionTime(){
        AnimatorStateInfo animState = playerController.animator.GetCurrentAnimatorStateInfo(0);
        if(animState.IsName(playerController.attackAnimState)){
            currentTime = animState.normalizedTime % 1;
            currentAttackCount = (int)animState.normalizedTime;

        }
    }

    // Define the period of the animation that is the attack windup
    private void CalculateWindUp(){
        if(currentTime * 100 > playerController.windupTime){
            playerController.isWindingUp = false;
        }
        else{
            playerController.isWindingUp = true;
        }
    }
}
