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
    private bool isNewClick;

    // Lux Stats
    public Dictionary<string, object> luxAbilityData;
    public Dictionary<string, object> qData;
    private float moveSpeed = 3.3f;
    private float turnSpeed = 15f;
    private float stoppingDistance = 0.1f;
    public float attackRange = 0.4f;

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
    public float debugDistance;

    // Input Data
    private Controls controls;
    public Queue<InputCommand> inputQueue;
    private InputCommand previousInput = null;
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

        isCastingText.text = "isCasting: ";
        isRunningText.text = "isRunning: ";

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

        HandleInput();
       
        if(isRunning){
            MoveToCursor();
        }

        if(isCasting){
            Q_Ability();
        }

        HandleProjectiles();
    }

    public void OnRightClick (InputAction.CallbackContext context){
        AddInputToQueue(new InputCommand{type = InputCommandType.Movement, time = context.time});
    }

    public void OnQ(InputAction.CallbackContext context){
        AddInputToQueue(new InputCommand{type = InputCommandType.CastSpell, time = context.time});
    }

    public void OnA(InputAction.CallbackContext context){
        ToggleAARange();
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

    // Read the input queue to handle movement and casting input commands
    private void HandleInput(){

        // Check the queue for any unconsume input commands
        if(inputQueue.Count > 0){

            // Get the next input command
            currentInput = inputQueue.Peek();

            // Set previous input to null on first function call
            if(previousInput == null){
                previousInput = currentInput;
            }

            switch(currentInput.type){

                // Process the movement input command
                case InputCommandType.Movement:
                    lastClickPosition = getMouseClickPosition();
                    isNewClick = true;
                    break;

                // Process the casting (basically a skillshot) input command
                case InputCommandType.CastSpell:
                    if(projectiles.Count == 0 && !isCasting){
                        StartCasting();
                    }
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

            // Store the previous input for conflict resolution and remove it from the queue
            previousInput = inputQueue.Dequeue();
        }
    }

    private void AddInputToQueue(InputCommand input){
        inputQueue.Enqueue(input);
    }

    // Return the right mouse click position and check the click is valid
    private Vector3 getMouseClickPosition(){
        float worldRadius = hitboxCollider.radius * hitboxGameObj.transform.lossyScale.x;
        Plane plane = new(Vector3.up, new Vector3(0, hitboxPos.y, 0));
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;
        
        // Detect attack click
        if(Physics.Raycast(ray, out hit)){
            if(hit.collider.name == "Lux_AI"){
                isAttackClick = true;
                Vector3 diff = hit.point - hitboxPos;
                float dist = Mathf.Sqrt(diff.x * diff.x / (mainCamera.aspect * mainCamera.aspect) + diff.z * diff.z);
                lastClickPosition = hit.transform.position;
                isRunning = true;
                return lastClickPosition;
            }
        }

        // Detect move click
        if (plane.Raycast(ray, out float enter)) {
            Vector3 hitPoint = ray.GetPoint(enter);

            Vector3 diff = hitPoint - hitboxPos;
            float dist = Mathf.Sqrt(diff.x * diff.x / (mainCamera.aspect * mainCamera.aspect) + diff.z * diff.z);
            
            if (dist > worldRadius){
                // Mouse click is outside the hitbox.
                isMoveClick = true;
                lastClickPosition = new Vector3(hitPoint.x, transform.position.y, hitPoint.z);
                isRunning = true;
            }
        }



        return lastClickPosition;
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

        if(isMoveClick){
            if (Vector3.Distance(transform.position, lastClickPosition) <= stoppingDistance){
                isRunning = false;
                animator.SetBool("isRunning", isRunning);
                isMoveClick = false;
            }        
        }
        else if(isAttackClick){
            if (Vector3.Distance(transform.position, lastClickPosition) <= (attackRange/2) + hitboxCollider.radius){
                Debug.Log(Vector3.Distance(transform.position, lastClickPosition));
                isRunning = false;
                animator.SetBool("isRunning", isRunning);
                isAttackClick = false;
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

    // Set casting state false, handle any left over movement commands
    private void FinishCasting(){
        isCasting = false;
        isCastingText.text = "isCasting: " + isCasting;
        animator.SetBool("isQCast", isCasting);
                         
        // If casting interrupted moving, finish moving to target location
        // Previous input command will be casting by this point because the movement command is already consumed by the time we interrupt it (this might cause issues?)
        if(previousInput.type == InputCommandType.CastSpell){
            isRunning = true;
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

    // Initiate the casting process
    private void StartCasting (){

        hasProjectile = true;
        isCasting = true;
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

    private void RotateTowardsTarget(Vector3 direction){
        if (direction != Vector3.zero){
            Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);
            Quaternion fromRotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            transform.rotation = Quaternion.Slerp(fromRotation, toRotation, turnSpeed * Time.deltaTime);
        }
    }

    // void OnDrawGizmos(){

    //     Gizmos.color = Color.black;
    //     Gizmos.DrawSphere(projectileSpawnPos, 0.3f);
    // }

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

    public float GetAttackRange(){
        return attackRange;
    }

}
