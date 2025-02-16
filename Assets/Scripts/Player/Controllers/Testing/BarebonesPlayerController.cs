using UnityEngine;
using System.Collections.Generic;
using Multiplayer;
using Unity.Netcode;
using UnityEngine.Serialization;
using UnityEngine.VFX;

public class BarebonesPlayerController : LuxController {
    
    // PLayer Scripts
    private TestInputProcessor _input;
    
    // Skill-shot Projectile
    public List<GameObject> projectiles;
    public List<GameObject> vfxProjectileList;
    
    // AA Projectile
    public VisualEffect projectileAA;
    public Vector3 projectileAATargetPosition;
    public double timeSinceLastAttack = 0;

    // Hitbox
    public GameObject hitboxGameObj;
    public float hitboxColliderRadius;
    public Vector3 direction;
    public Vector3 hitboxPos;
    
    // Animation
    public Animator animator;
    
    // State Management
    public StateManager StateManager;
    private TestMovingState _movingState;
    private AttackingState _attackingState;
    private IdleState _idleState;
    private CastingState _castingState;
    private DeathState _deathState;
    public string currentState;
    
    
    // Camera
    public Camera mainCamera;
    
    private void Awake() {
        playerType = PlayerType.Player;
        
        _input = GetComponent<TestInputProcessor>();
        mainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        globalState = GameObject.Find("Global State").GetComponent<GlobalState>();
        hitboxGameObj = GameObject.Find("Hitbox");
        champion = Instantiate(championSO);
    }
    
    private void Start() {
        
        InitStateManager();
        
        hitboxColliderRadius = hitboxGameObj.GetComponent<SphereCollider>().radius;
        hitboxPos = hitboxGameObj.transform.position;
        projectiles = new List<GameObject>();
    }

    
    private void Update() {
        StateManager.Update();
        if(hitboxGameObj != null) hitboxPos = hitboxGameObj.transform.position;
        currentState = StateManager.GetCurrentState();

        if (StateManager.GetCurrentState() != "AttackingState")
            if (timeSinceLastAttack > 0)
                timeSinceLastAttack -= Time.deltaTime;
    }
    

    /// <summary>
    /// Store the players abilities in a list
    /// Set the casting strategy for the ability depending on singleplayer/multiplayer
    /// </summary>
    private void InitAbilities() {


    }
    
    /// <summary>
    /// Initialise the state manager
    /// </summary>
    private void InitStateManager() {
        StateManager = StateManager.Instance;
        _idleState = new IdleState();
        TransitionToIdle();
    }

    public void TransitionToIdle() {
        StateManager.ChangeState(_idleState);
    }

    public void TransitionToAttack() {
        StateManager.ChangeState(new AttackingState(gameObject));
    }

    public void TransitionToMove() {
        StateManager.ChangeState(new TestMovingState(gameObject,
            _input.isAttackClick));
    }

    /// <summary>
    /// Rotate player towards a target direction
    /// </summary>
    /// <param name="toDirection"></param>
    public void RotateTowardsTarget(Vector3 toDirection) {
        if (toDirection != Vector3.zero) {
            var toRotation = Quaternion.LookRotation(toDirection, Vector3.up);
            var fromRotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            transform.rotation = Quaternion.Slerp(fromRotation, toRotation, champion.turnSpeed * Time.deltaTime);
        }
    }
}