using UnityEngine;

public class AI_CastingState : State
{

    public GameObject player;
    private LuxAIController agent;
    public Ability ability;
    public bool castingFinished = false; 

    public AI_CastingState (LuxAIController controller,  GameObject gameObject, Ability ability){
        agent = controller;
        player = gameObject;
        this.ability = ability;
        ability.maxCooldown = Random.Range(1, 8);
    }

    public override void Enter() {
        ability.PutOnCooldown();
        agent.isCasting = true; // This flag was used for the animator, can probs remove if triggers are working
        agent.canCast = true;
        agent.animator.SetTrigger(ability.animationTrigger);
    }

    public override void Execute() {

        // Track when animation ends
        if(agent.animator.GetCurrentAnimatorClipInfo(0).Length > 0){
            castingFinished = agent.animator.GetCurrentAnimatorClipInfo(0)[0].clip.name == ability.animationClip.name && 
                agent.animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.95; // This is set to 0.95 because the normalized time doesn't always reach 100%
        }
       

        // Get the direction the abliity should move towards
        Vector3 direction = (agent.projectileTargetPosition - player.transform.position).normalized;
  
        // Rotate the player in the direction the spell was cast
        agent.RotateTowardsTarget(direction);
        

        if (agent.canCast) {
            // Set the spawn position of the projectile
            float worldRadius = agent.hitboxCollider.radius * agent.hitboxGameObj.transform.lossyScale.x;
            Vector3 abilitySpawnPos = new Vector3(player.transform.position.x, ability.spawnHeight, player.transform.position.z) + direction * worldRadius;
            
            // Create projectile
            GameObject newProjectile = LuxAIController.Instantiate(ability.missilePrefab, abilitySpawnPos,  Quaternion.LookRotation(direction, Vector3.up));

            // Store projectile in list
            agent.projectiles.Add(newProjectile);

            // Get script on prefab to initialize propreties
            ProjectileAbility projectileScript = newProjectile.GetComponent<ProjectileAbility>();
            //projectileScript?.InitProjectileProperties(direction, ability, agent.projectiles, agent.playerType);
          
            agent.canCast = false;
        }
        
        if(castingFinished){
            agent.TransitionToIdle();
        }
    }

    public override void Exit() {
        agent.isCasting = false;
    }
}
