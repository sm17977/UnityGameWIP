using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackAnimationPicker : StateMachineBehaviour
{

    private LuxPlayerController playerController;

    //OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex){

        if (playerController == null){
            playerController = animator.GetComponent<LuxPlayerController>();
        }

        animator.SetInteger("attackVariant", 2);        
    }


    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex){
        if(playerController.timeSinceLastAttack <= 0){
            int random = Random.Range(0, 2);
            animator.SetInteger("attackVariant", random);   
            playerController.timeSinceLastAttack = 1.15;
        }     
    }


    //OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    // override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex){
    // }

    // OnStateMove is called right after Animator.OnAnimatorMove()
    // override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    // {
    //    // Implement code that processes and affects root motion
    // }

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
