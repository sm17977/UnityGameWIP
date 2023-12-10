using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;
using NUnit.Framework;

public class Lux_Player_Controller : MonoBehaviour
{

    // Flags
    private bool isRunning;
    private bool isCasting;
    private bool isAttacking;
    private bool isAttackClick = false;
    private bool isMoveClick = false;
    private bool hasProjectile = false;
    private bool incompleteMovement = false;
    private bool isNewClick;

    // Lux Stats
    public Dictionary<string, object> luxAbilityData;
    public Dictionary<string, object> qData;
    private float moveSpeed = 3.3f;
    private float turnSpeed = 15f;
    private float stoppingDistance = 0.1f;
    private float attackRange = 0.6f;

    // Projectile
    public GameObject projectile;
    private List<GameObject> projectiles;
    private Vector3 projectileSpawnPos;
    private Vector3 projectileTargetPosition;

    // Hitbox
    public GameObject hitboxGameObj;
    private SphereCollider hitboxCollider;
    private Vector3 hitboxPos;

    // Movement Indicator
    public GameObject movementIndicatorPrefab;
    private GameObject movementIndicator;

    // Animator
    public Animator animator;

    // Camera
    public Camera mainCamera;
 
    // Debug Text
    public TMP_Text isCastingText;
    public TMP_Text isRunningText;
    public TMP_Text isAttackingText;
    public TMP_Text previousInputTypeText;
    public TMP_Text nextPreviousInputTypeText;
    public TMP_Text inputQueueSizeText;
    public float debugDistance;

    // Input Data
    private Controls controls;
    public Queue<InputCommand> inputQueue;
    private InputCommand previousInput = null;
    private InputCommand nextPreviousInput = null;
    private InputCommand currentInput;
    private Vector3 lastClickPosition;

    // AA Range Indicator
    public GameObject AARangeIndicatorPrefab;
    private GameObject AARangeIndicator;
    private bool isAARangeIndicatorOn = false;

    void Awake(){
        // Initiate Input Actions/Events
        controls = new Controls();
        controls.Player.Enable();
        controls.Player.RightClick.performed += OnRightClick;
        controls.Player.Q.performed += OnQ;
        controls.Player.A.performed += OnA;
    }



    // Start is called before the first frame update
    void Start()
    {

        hitboxCollider = hitboxGameObj.GetComponent<SphereCollider>();
        hitboxPos = hitboxGameObj.transform.position;
        
        isRunning = false;
        isCasting = false;
        isAttacking = false;

        isCastingText.text = "isCasting: ";
        isRunningText.text = "isRunning: ";
        isAttackingText.text = "isAttacking: ";
        previousInputTypeText.text = "previousInputType: ";
        nextPreviousInputTypeText.text = "nextPreviousInputType: ";
        inputQueueSizeText.text = "inputQueueSize: ";

        inputQueue = new Queue<InputCommand>();
        projectiles = new List<GameObject>();

        // Define Lux Ability Data
        luxAbilityData = new Dictionary<string, object>
        {
            {
                "Q", new Dictionary<string, object>
                {
                    {"range", 8f},
                    {"direction", null},
                    {"speed", 10f}
                }
            },
            {
                "W", new Dictionary<string, object>
                {
                    {"range", 5},
                    {"direction", new Vector3(1f, 2f, 5f)}
                }
            },
            {
                "E", new Dictionary<string, object>
                {
                    {"range", 5},
                    {"direction", new Vector3(1f, 2f, 5f)}
                }
            },
            {
                "R", new Dictionary<string, object>
                {
                    {"range", 5},
                    {"direction", new Vector3(1f, 2f, 5f)}
                }
            }
        };
    }

    // Update is called once per frame
    void Update(){

        isRunningText.text = "isRunning: " + isRunning;
        isAttackingText.text = "isAttacking: " + isAttacking;
        inputQueueSizeText.text = "inputQueueSize: " + inputQueue.Count.ToString();

        HandleInput();
       
        if(isRunning){
            MoveToCursor();
        }

        if(isCasting){
            Q_Ability();
        }

        if(isAttacking){
            Attack();
        }

        HandleProjectiles();
    }

    public void OnRightClick (InputAction.CallbackContext context){
        isNewClick = true;
        InputCommandType inputType = GetMouseClickPosition();
        AddInputToQueue(new InputCommand{type = inputType, time = context.time});
    }

    public void OnQ(InputAction.CallbackContext context){
        AddInputToQueue(new InputCommand{type = InputCommandType.CastSpell, time = context.time});
    }

    public void OnA(InputAction.CallbackContext context){
        ToggleAARange();
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
                previousInputTypeText.text = "previousInputType: " +  previousInput.type.ToString();
            }

            switch(currentInput.type){

                // Process the movement command
                case InputCommandType.Movement:
                    isMoveClick = true;
                    isRunning = true;
                    break;

                // Process the attack command
                case InputCommandType.Attack:
                    isAttackClick = true;
                    break;

                // Process the cast spell command
                case InputCommandType.CastSpell:
                    if(projectiles.Count == 0 && !isCasting){
                        isCasting = true;
                        StartCasting();
                    }
                    break;

                case InputCommandType.None:
                    break;

            } 

            // Go through a sort of "conflict resolution" for when a new input command occurrs while an animation is currently playing
            if(isCasting && isRunning){
                          
                // Casting Interrupt     
                // If we input a movement command while casting, interrupt the cast animation so we can transition to moving/idle
                if(previousInput.type == InputCommandType.CastSpell){
                    isCasting = false;
                    animator.SetBool("isQCast", isCasting);
                }

                // Moving Interrupt
                // If we input a casting command while moving, interrupt the movement animation so we can transition to casting
                // If the interrupted movement command did not complete (a.k.a target pos was not reached), we have specific handling to continue moving in FinishCasting()
                else if(previousInput.type == InputCommandType.Movement){
                    isRunning = false;
                    animator.SetBool("isRunning", isRunning);
                }
            }

            if(isAttacking && isRunning){

                // Attacking Interrupt
                // If we input a movement command while attacking, interrupt the attack animation so we can transition to moving/idle
                if(previousInput.type == InputCommandType.Attack){
                    isAttacking = false;
                    animator.SetBool("isAttacking", isAttacking);
                }

            }


            // Store penultimate input, used when we interrupt a move command with a cast command so we can still finish the move command
            nextPreviousInput = previousInput;
            nextPreviousInputTypeText.text = "nextPreviousInputType: " +  nextPreviousInput.type.ToString();

            previousInput = inputQueue.Dequeue();
            previousInputTypeText.text = "previousInputType: " +  previousInput.type.ToString();
        }
    }

    private void AddInputToQueue(InputCommand input){
        inputQueue.Enqueue(input);
    }

    // Process the right mouse click, are we attacking or moving, then return the click position
    private InputCommandType GetMouseClickPosition(){
        float worldRadius = hitboxCollider.radius * hitboxGameObj.transform.lossyScale.x;
        Plane plane = new(Vector3.up, new Vector3(0, hitboxPos.y, 0));
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;
        
        // Detect attack click
        if(Physics.Raycast(ray, out hit)){
            if(hit.collider.name == "Lux_AI"){
                lastClickPosition = hit.transform.position;
                isRunning = true;
                return InputCommandType.Attack;
            }
        }

        // Detect move click
        if (plane.Raycast(ray, out float enter)) {
            Vector3 hitPoint = ray.GetPoint(enter);
            Vector3 diff = hitPoint - hitboxPos;
            float dist = Mathf.Sqrt(diff.x * diff.x / (mainCamera.aspect * mainCamera.aspect) + diff.z * diff.z);
            
            if (dist > worldRadius){
                // Mouse click is outside the hitbox.
                lastClickPosition = new Vector3(hitPoint.x, transform.position.y, hitPoint.z);
                return InputCommandType.Movement;
            }
        }

        // Default to None type
        return InputCommandType.None;
    }

   // Show the movement indicator and move our player towards the last mouse click position
    private void MoveToCursor(){

        isCastingText.text = "isCasting: " + isCasting;
        hitboxPos = hitboxGameObj.transform.position;
        animator.SetBool("isRunning", isRunning);
  
        Vector3 direction = (lastClickPosition - transform.position).normalized;
        direction.y = 0f;

        ShowMovementIndicator(lastClickPosition);

        // Move player towards last mouse click 
        transform.Translate(direction * moveSpeed * Time.deltaTime, Space.World);
        RotateTowardsTarget(direction);
        debugDistance = Vector3.Distance(transform.position, lastClickPosition);

        // Process attack click
        if(isAttackClick){
            float calculatedAttackRange = ((attackRange * 10) / 2);
            if (Vector3.Distance(transform.position, lastClickPosition) <= calculatedAttackRange + hitboxCollider.radius){
                isAttacking = true;
                isAttackClick = false;
                isRunning = false;
                animator.SetBool("isRunning", isRunning);
            }    
        }

        // Process move click
        else if(isMoveClick){
            incompleteMovement = true;
            if (Vector3.Distance(transform.position, lastClickPosition) <= stoppingDistance){
                isMoveClick = false;
                isRunning = false;
                incompleteMovement = false;
                animator.SetBool("isRunning", isRunning);
            }        
        }
    }

    // Renders a quick animation on the terrain whenever the right mouse is clicked 
    private void ShowMovementIndicator(Vector3 position){

        if(isNewClick){
            if(movementIndicator != null){
                Destroy(movementIndicator);
            }

            movementIndicator = Instantiate(movementIndicatorPrefab, new Vector3(position.x, 0.51f, position.z), Quaternion.Euler(90, 0, 0));
            isNewClick = false;
        }
    }

    // Casts the Q skillshot projectile 
    private void Q_Ability(){

        // Check if the casting animation has finished
        bool castingFinished = animator.GetCurrentAnimatorStateInfo(0).IsName("Q_Ability") && 
            animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1;

        if(isCasting){
            UpdateCasting();
            if(castingFinished){
                FinishCasting();
            }
        }
    }

    // Initiate the casting process
    private void StartCasting (){

        hasProjectile = true;
        animator.SetBool("isQCast", isCasting);
        isCastingText.text = "isCasting: " + isCasting;
        projectileTargetPosition = transform.position;

        Plane plane = new Plane(Vector3.up, new Vector3(0, hitboxPos.y, 0));
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (plane.Raycast(ray, out float enter)) {
            Vector3 hitPoint = ray.GetPoint(enter);
            projectileTargetPosition = new Vector3(hitPoint.x, transform.position.y, hitPoint.z);
        }
    }

    // Instantiates the projectile, rotates the player towards its target (Generic_Projectile_Controller handles the movement)
    private void UpdateCasting(){

        Vector3 direction = (projectileTargetPosition - transform.position).normalized;
        qData = (Dictionary<string, object>)luxAbilityData["Q"];
        qData["direction"] = direction;
        RotateTowardsTarget(direction);

        if (hasProjectile) {
            float worldRadius = hitboxCollider.radius * hitboxGameObj.transform.lossyScale.x;
            projectileSpawnPos = new Vector3(transform.position.x, 0.51f, transform.position.z) + direction * worldRadius;
            
            Vector3 eulerAngles = transform.rotation.eulerAngles;
            eulerAngles.x = 90;
            Quaternion rotation = Quaternion.Euler(eulerAngles);

            GameObject newProjectile = Instantiate(projectile, projectileSpawnPos, rotation);
            projectiles.Add(newProjectile);
            Generic_Projectile_Controller projectile_Controller = newProjectile.GetComponent<Generic_Projectile_Controller>();
            projectile_Controller.setParams((Dictionary<string, object>)luxAbilityData["Q"]);
            hasProjectile = false;
        }
    }

    // Set casting state false, handle any left over movement commands
    private void FinishCasting(){
        isCasting = false;
        isCastingText.text = "isCasting: " + isCasting;
        animator.SetBool("isQCast", isCasting);
                         
        // If casting interrupted moving, finish moving to target location
        if(nextPreviousInput.type == InputCommandType.Movement && incompleteMovement){
            isRunning = true;
        }
    }

    private void Attack(){

        Vector3 direction = (lastClickPosition - transform.position).normalized;
        animator.SetBool("isAttacking", isAttacking);
        RotateTowardsTarget(direction);
        











    }


    private void RotateTowardsTarget(Vector3 direction){
        if (direction != Vector3.zero){
            Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);
            Quaternion fromRotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            transform.rotation = Quaternion.Slerp(fromRotation, toRotation, turnSpeed * Time.deltaTime);
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
        return attackRange;
    }

}
