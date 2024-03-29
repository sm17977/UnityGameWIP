
using UnityEngine;

public class AI_IdleState : State
{

    private Lux_AI_Controller agent;
    private GameObject player;

    public AI_IdleState(Lux_AI_Controller agentController){
        agent = agentController;
        player = agent.Lux_Player;
    }


    public override void Enter() {

    }

    public override void Execute() {

        if(IsPlayerInQRange(agent.LuxQAbility.range)){
            agent.projectileTargetPosition = player.transform.position;
            agent.SimulateQ();
        }
        else{
            
        }




    }

    public override void Exit() {

    }


    private bool IsPlayerInQRange(float abilityRange){

        Vector3 playerPos = player.transform.position;
        Vector3 agentPos = agent.transform.position;

        float distanceToPlayer = Vector3.Distance(agentPos, playerPos);

        if(distanceToPlayer <= abilityRange){
            return true;
        }

        return false;
    }
}
