using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;
using NUnit.Framework;
using UnityEngine.VFX;

public class Lux_Player_Controller : MonoBehaviour
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
    private bool isNewClick;
    public bool inAttackRange = false;

    // Lux Stats
    public Lux lux;

    // Skllshot Projectile
    public GameObject projectile;
    public List<GameObject> projectiles;
    public Vector3 projectileSpawnPos;
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
    private GameObject movementIndicator;

    // Animation
    public Animator animator;

    public string attackAnimName = "model|lux_attack1_model";
    public string attackAnimState = "Attack";

    // Camera
    public Camera mainCamera;
 
    // Debug Text
    public TMP_Text currentStateText;
    public TMP_Text isWindingUpText;
    public TMP_Text autoCDText;

    // Input Data
    private Controls controls;
    public Queue<InputCommand> inputQueue;
    public InputCommand previousInput = null;
    public InputCommand nextPreviousInput = null;
    private InputCommand currentInput;
    private Vector3 lastClickPosition;

    // AA Range Indicator
    public GameObject AARangeIndicatorPrefab;
    private GameObject AARangeIndicator;
    private bool isAARangeIndicatorOn = false;

    // State Management
    private StateManager stateManager;
    private MovingState movingState;
    private AttackingState attackingState;
    private IdleState idleState;
    private CastingState castingState;

    // AI
    public GameObject Lux_AI;
    public GameObject ai_hitboxGameObj;
    public SphereCollider ai_hitboxCollider;
    private Vector3 ai_hitboxPos;

    // Debugging stuff
    public float attackStartTime;
    public bool attackStartTimeLogged = false;

    void Awake(){
        // Initiate Input Actions/Events
        controls = new Controls();
        controls.Player.Enable();
        controls.Player.RightClick.performed += OnRightClick;
        controls.Player.Q.performed += OnQ;
        controls.Player.A.performed += OnA;
    }

    // Start is called before the first frame update
    void Start(){

        InitStates();
        stateManager.ChangeState(idleState);
        isWindingUp = false;

        hitboxCollider = hitboxGameObj.GetComponent<SphereCollider>();
        hitboxPos = hitboxGameObj.transform.position;

        ai_hitboxCollider = ai_hitboxGameObj.GetComponent<SphereCollider>();
        ai_hitboxPos = ai_hitboxGameObj.transform.position;
        
        currentStateText.text = "currentState: ";
        isWindingUpText.text = "isWindingUp: " + isWindingUp;
        autoCDText.text = "autoCD: " + timeSinceLastAttack;
        
        lux = new Lux();
        inputQueue = new Queue<InputCommand>();
        projectiles = new List<GameObject>();
    }

    // Update is called once per frame
    void Update(){

        hitboxPos = hitboxGameObj.transform.position;

        HandleInput();

        stateManager.Update();

        currentStateText.text = "currentState: " + stateManager.GetCurrentState();

        if(stateManager.GetCurrentState() != "AttackingState"){
            if(timeSinceLastAttack > 0){
                timeSinceLastAttack -= Time.deltaTime;
            }
        }

        autoCDText.text = "autoCD: " + timeSinceLastAttack;
  
        HandleProjectiles();
    }

    public void OnRightClick (InputAction.CallbackContext context){
        isNewClick = true;
        InputCommandType inputType = GetClickInput();
        AddInputToQueue(new InputCommand{type = inputType, time = context.time});
    }

    public void OnQ(InputAction.CallbackContext context){
        AddInputToQueue(new InputCommand{type = InputCommandType.CastSpell, time = context.time});
    }

    public void OnA(InputAction.CallbackContext context){
        ToggleAARange();
    }


    private void InitStates(){
        stateManager = new StateManager();
        idleState = new IdleState();
    }


    // Read the input queue to handle movement and casting input commands
    private void HandleInput(){

        // Check the queue for any unconsumed input commands
        if(inputQueue.Count > 0){

            // Get the next input command
            currentInput = inputQueue.Peek();

            // Set previous input to null on first function call
            if(previousInput == null){
                previousInput = currentInput;
            }

            switch(currentInput.type){

                // Process the movement command
                case InputCommandType.Movement:

                    ShowMovementIndicator(lastClickPosition);
                    movingState = new MovingState(this, lastClickPosition, GetStoppingDistance(), gameObject, false);
                    stateManager.ChangeState(movingState);
                    
                    inputQueue.Dequeue();
                    break;

                //Process the attack command
                case InputCommandType.Attack:
                    direction = (lastClickPosition - transform.position).normalized;
                    // If the player is not within attack range, move until they are
                    if(!IsInAttackRange(lastClickPosition, GetStoppingDistance())){
                        stateManager.ChangeState(new MovingState(this, lastClickPosition, GetStoppingDistance(), gameObject, true));
                    }
                    // If they are in attack range, transition to attack state
                    // Attacking state persists until we input a new command
                    else{
                        stateManager.ChangeState(new AttackingState(this, direction, gameObject, lux.windup_time));
                    }
                    inputQueue.Dequeue();
                    break;

                // Process the cast spell command
                case InputCommandType.CastSpell:
                    if(projectiles.Count == 0 && !isCasting){
                        GetCastingTargetPosition();
                        stateManager.ChangeState(new CastingState(this, gameObject));
                    }
                    inputQueue.Dequeue();
                    break;

                case InputCommandType.None:
                    inputQueue.Dequeue();
                    break;
            } 
        }
    }

    private void AddInputToQueue(InputCommand input){
        inputQueue.Enqueue(input);
    }

    // Detect if the player has clicked on the ground (movement commmand) or an enemy (attack command)
    // Store the position of the click in lastClickPosition
    // Return the input command type
    private InputCommandType GetClickInput(){
        float worldRadius = hitboxCollider.radius * hitboxGameObj.transform.lossyScale.x;
        Plane plane = new(Vector3.up, new Vector3(0, hitboxPos.y, 0));
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        // Detect attack click
        if (Physics.Raycast(ray, out RaycastHit hit)){
            if (hit.collider.name == "Lux_AI"){
                GameObject Lux_AI = hit.rigidbody.gameObject;
                ToggleOutlineShader(Lux_AI, "Default");
                isAttackClick = true;
                lastClickPosition = hit.transform.position;
                projectileAATargetPosition = hit.transform.position;
                return InputCommandType.Attack;
            }
        }

        // Detect move click
        if (plane.Raycast(ray, out float enter)) {
            ToggleOutlineShader(Lux_AI, "Selection");
            Vector3 hitPoint = ray.GetPoint(enter);
            Vector3 diff = hitPoint - hitboxPos;
            float dist = Mathf.Sqrt(diff.x * diff.x / (mainCamera.aspect * mainCamera.aspect) + diff.z * diff.z);
            
            if (dist > worldRadius){
                // Move click is valid (outside the players hitbox)
                lastClickPosition = new Vector3(hitPoint.x, transform.position.y, hitPoint.z);
                isAttackClick = false;
                return InputCommandType.Movement;
            }
        }

        // Default to None type
        return InputCommandType.None;
    }

    // Set the AI gameobject layet to default so the outline renders ('Selection' layer = outline off)
    void ToggleOutlineShader(GameObject Lux_AI, string mask){
        ChangeLayerRecursively(Lux_AI, LayerMask.NameToLayer(mask));
    }

    void ChangeLayerRecursively(GameObject obj, int newLayer) {
        if (obj == null) return;
        obj.layer = newLayer;

        foreach (Transform child in obj.transform) {
            ChangeLayerRecursively(child.gameObject, newLayer);
        }
    }

    private void GetCastingTargetPosition(){
        Plane plane = new Plane(Vector3.up, new Vector3(0, hitboxPos.y, 0));
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        projectileTargetPosition = transform.position;

        if (plane.Raycast(ray, out float enter)) {
            Vector3 hitPoint = ray.GetPoint(enter);
            projectileTargetPosition = new Vector3(hitPoint.x, transform.position.y, hitPoint.z);
        }
    }

   // Get the minimum distance the player must move to reach the target location
    private float GetStoppingDistance(){

        float distance = lux.stopping_distance;

        // If we inputted an attack click, the distance is calculated from the edge of the players attack range and the center of the enemy
        if(isAttackClick){
            float calculatedAttackRange = ((lux.AA_range * 10) / 2);
            distance = calculatedAttackRange + hitboxCollider.radius;
        }

        return distance;
    }

    private bool IsInAttackRange(Vector3 targetLocation, float stoppingDistance){

        if (Vector3.Distance(transform.position, targetLocation) <= stoppingDistance){
            return true;
        }      
        return false;
    }

    // Renders a quick animation on the terrain whenever the right mouse is clicked 
    public void ShowMovementIndicator(Vector3 position){

        if(isNewClick){
            if(movementIndicator != null){
                Destroy(movementIndicator);
            }

            movementIndicator = Instantiate(movementIndicatorPrefab, new Vector3(position.x, 0.51f, position.z), Quaternion.Euler(90, 0, 0));
            isNewClick = false;
        }
    }

    public void TransitionToIdle(){
        stateManager.ChangeState(idleState);
    }

    public void TransitionToAttack(){
        stateManager.ChangeState(new AttackingState(this, direction, gameObject, lux.windup_time));
    }

    public void TransitionToMove(){
        stateManager.ChangeState(new MovingState(this, lastClickPosition, GetStoppingDistance(), gameObject, isAttackClick));
    }

    public void RotateTowardsTarget(Vector3 direction){
        if (direction != Vector3.zero){
            Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);
            Quaternion fromRotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            transform.rotation = Quaternion.Slerp(fromRotation, toRotation, lux.turn_speed * Time.deltaTime);
        }
    }

    private void HandleProjectiles(){
        if(projectiles.Count >= 1){
            for(int i = 0; i < projectiles.Count; i++){
                GameObject projectile = projectiles[i];
                if(projectile != null && projectile.activeSelf){
                    Generic_Projectile_Controller controller = projectile.GetComponent<Generic_Projectile_Controller>();
                    if(controller.remainingDistance <= 0){
                        projectiles.RemoveAt(i);
                        Destroy(projectile);
                    }
                }
            }
        }
    }

    private void ToggleAARange(){
        if(!isAARangeIndicatorOn){
            isAARangeIndicatorOn = true;
            AARangeIndicator = Instantiate(AARangeIndicatorPrefab, transform);
        }
        else{
            Destroy(AARangeIndicator);
            isAARangeIndicatorOn = false;
        }
    }

    public void PrintQueue(){
        string output = "Queue: [";

        foreach(InputCommand input in inputQueue){
            output += input.type + ", ";
        }

        output += "]";
        Debug.Log(output);
    }

    public float GetAttackRange(){
        return lux.AA_range;
    }
}