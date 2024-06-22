using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.VFX;

public class LuxAIController : LuxController
{
    
   // Flags
    public bool isRunning;
    public bool isCasting;
    public bool isAttacking;
    public bool isWindingUp;
    public bool isAttackClick = false;
    public bool canCast = false;
    public bool canAA = false;
    public bool incompleteMovement = false;
    public bool inAttackRange = false;


    // Skllshot Projectile
    public List<GameObject> projectiles;
    public List<GameObject> vfxProjectileList;
    public Vector3 projectileTargetPosition;

    // AA Projectile
    public VisualEffect projectileAA;
    public Vector3 projectileAASpawnPos;
    public Vector3 projectileAATargetPosition;
    public double timeSinceLastAttack = 0;

    // Hitbox
    public GameObject hitboxGameObj;
    public SphereCollider hitboxCollider;
    private Vector3 hitboxPos;
    public Vector3 direction;

    // Movement Indicator
    public GameObject movementIndicatorPrefab;

    // Animation
    public Animator animator;
 
    // Input Data
    public Queue<InputCommand> inputQueue;
    public InputCommand previousInput;
    private InputCommand currentInput;

    // AA Range Indicator
    public GameObject AARangeIndicatorPrefab;
    private GameObject AARangeIndicator;
    private bool isAARangeIndicatorOn = false;

    // State Management
    private StateManager stateManager;
    private MovingState movingState;
    private AttackingState attackingState;
    private AI_IdleState idleState;
    private AI_CastingState castingState;
    public string currentState;

    // Player

    public GameObject Lux_Player;
    public GameObject player_hitboxGameObj;
    public SphereCollider player_hitboxCollider;
    private Vector3 player_hitboxPos;


    // Abilities
    public Ability LuxQAbilitySO;
    public Ability LuxEAbilitySO;
    public Ability LuxQAbility;
    public Ability LuxEAbility;

 
    void Awake(){
        LuxQAbility = Object.Instantiate(LuxQAbilitySO);
        LuxEAbility = Object.Instantiate(LuxEAbilitySO);
        previousInput = new InputCommand{type = InputCommandType.Init};
    }

    // Start is called before the first frame update
    void Start(){

        BuffManager = new BuffManager(this);
        playerType = PlayerType.Bot;
       
        InitStates();
        stateManager.ChangeState(idleState);
        isWindingUp = false;

        hitboxCollider = hitboxGameObj.GetComponent<SphereCollider>();
        hitboxPos = hitboxGameObj.transform.position;

        inputQueue = new Queue<InputCommand>();
        projectiles = new List<GameObject>();
        ResetCooldowns();
    }

    // Update is called once per frame
    void Update(){

        hitboxPos = hitboxGameObj.transform.position;

        HandleInput();

        stateManager.Update();

        BuffManager.Update();

        currentState = stateManager.GetCurrentState();
  
        HandleProjectiles();
    }


    public void SimulateQ(){
        AddInputToQueue(new InputCommand{type = InputCommandType.CastSpell, key = "Q"});
    }

    public void SimulateE(){
        AddInputToQueue(new InputCommand{type = InputCommandType.CastSpell, key = "E"});
    }

    private void InitStates(){
        stateManager = new StateManager();
        idleState = new AI_IdleState(this);
    }

    // Read the input queue to handle movement and casting input commands
    private void HandleInput(){

        if(inputQueue != null){

            // Check the queue for any unconsumed input commands
            if(inputQueue.Count > 0){

                // Get the next input command
                currentInput = inputQueue.Peek();

                // Set previous input to null on first function call
                if(previousInput.type == InputCommandType.Init){
                    previousInput = currentInput;
                }

                switch(currentInput.type){

                    // Process the cast spell command
                    case InputCommandType.CastSpell:

                         Ability inputAbility = LuxQAbility;

                        if(currentInput.key == "Q"){
                            inputAbility = LuxQAbility;
                        }
                        if(currentInput.key == "E"){
                            inputAbility = LuxEAbility;
                        }


                        if(!isCasting && !inputAbility.OnCooldown()){
                            stateManager.ChangeState(new AI_CastingState(this, gameObject, inputAbility));
                        }
                        inputQueue.Dequeue();
                        break;

                    case InputCommandType.None:
                        inputQueue.Dequeue();
                        break;
                } 
            }
        }
    }

    private void AddInputToQueue(InputCommand input){
        inputQueue.Enqueue(input);
    }


    public void TransitionToIdle(){
        stateManager.ChangeState(idleState);
    }

 
    public void RotateTowardsTarget(Vector3 direction){
        if (direction != Vector3.zero){
            Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);
            Quaternion fromRotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            transform.rotation = Quaternion.Slerp(fromRotation, toRotation, lux.turnSpeed * Time.deltaTime);
        }
    }

    private void HandleProjectiles(){
        if(projectiles.Count > 0){
            for(int i = 0; i < projectiles.Count; i++){
                GameObject projectile = projectiles[i];
                if(projectile != null && projectile.activeSelf){
                    ProjectileAbility missile = projectile.GetComponent<ProjectileAbility>();
                    if(missile.canBeDestroyed){
                        projectiles.RemoveAt(i);
                        Destroy(projectile);
                    }
                }
            }
        }
    }

    public float GetAttackRange(){
        return lux.AA_range;
    }

    private void ResetCooldowns(){
        LuxQAbility.currentCooldown = 0;
        LuxEAbility.currentCooldown = 0;
    }
}
