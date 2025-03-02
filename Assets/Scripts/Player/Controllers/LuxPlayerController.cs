using UnityEngine;
using System.Collections.Generic;
using Multiplayer;
using Unity.Netcode;
using UnityEngine.Serialization;
using UnityEngine.VFX;

public class LuxPlayerController : LuxController {
    
    // PLayer Scripts
    private RPCController _rpc;
    private InputProcessor _input;
    
    // Skill-shot Projectile
    public List<GameObject> projectiles;
    public List<GameObject> vfxProjectileList;
    
    // AA Projectile
    public VisualEffect projectileAA;
    public Vector3 projectileAATargetPosition;
    public double timeSinceLastAttack = 0;
    public GameObject currentAATarget;

    // Hitbox
    public GameObject hitboxGameObj;
    public float hitboxColliderRadius;
    public Vector3 direction;
    public Vector3 hitboxPos;
    
    // Animation
    public Animator animator;
    public ClientNetworkAnimator networkAnimator;
    
    // State Management
    public StateManager StateManager;
    private MovingState _movingState;
    private AttackingState _attackingState;
    private IdleState _idleState;
    private CastingState _castingState;
    private DeathState _deathState;
    public string currentState;

    // AI
    public GameObject luxAI;
    public GameObject aiHitboxGameObj;
    public SphereCollider aiHitboxCollider;
    private Vector3 _aiHitboxPos;
    
    // Abilities
    public List<AbilityEntry> abilitiesList;
    public Dictionary<string, Ability> Abilities;
    public Dictionary<string, GameObject> ActiveAbilityPrefabs;
    
    // Health
    public Health health;
    public GameObject healthBarAnchor;
    
    // Camera
    public Camera mainCamera;
    
    private void Awake() {
        playerType = PlayerType.Player;
        
        mainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        globalState = GameObject.Find("Global State").GetComponent<GlobalState>();
        hitboxGameObj = GameObject.Find("Hitbox");
        healthBarAnchor = transform.Find("HealthBarAnchor").gameObject;
        
        networkAnimator = GetComponent<ClientNetworkAnimator>();
        _input = GetComponent<InputProcessor>();
        _rpc = GetComponent<RPCController>();
        health = GetComponent<Health>();
        
        champion = Instantiate(championSO);
    }

    private void OnEnable() {
        globalState.OnMultiplayerGameMode += InitAbilities;
        globalState.OnSinglePlayerGameMode += InitAbilities;
    }
    
    private void Start() {
        ClientBuffManager = new ClientBuffManager(this);
        
        Health.OnPlayerDeath += (async (player) => {
            if (this != player) return;
            Debug.Log("OnPlayerDeath");
            StateManager.ChangeState(new DeathState(this));
            ProcessPlayerDeath();
        });
        
        InitStateManager();
        
        aiHitboxCollider = aiHitboxGameObj.GetComponent<SphereCollider>();
        _aiHitboxPos = aiHitboxGameObj.transform.position;
        hitboxColliderRadius = hitboxGameObj.GetComponent<SphereCollider>().radius;
        hitboxPos = hitboxGameObj.transform.position;
        projectiles = new List<GameObject>();
        ActiveAbilityPrefabs = new Dictionary<string, GameObject>();
        
        ResetCooldowns();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        InitAbilities();
        if (IsServer) {
            canMove.Value = true;
            movementSpeed.Value = champion.movementSpeed;
        }
    }
    
    private void Update() {
        StateManager.Update();
        
        if (globalState.currentScene == "Multiplayer" && !IsOwner) return;
        if(hitboxGameObj != null) hitboxPos = hitboxGameObj.transform.position;
        
        ClientBuffManager.Update();
        
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

        // Initialise the abilities from the Champion SO
        Abilities = new Dictionary<string, Ability>() {
            { "Q", Instantiate(champion.abilityQ) },
            { "W", Instantiate(champion.abilityW) },
            { "E", Instantiate(champion.abilityE) },
            { "R", Instantiate(champion.abilityR) },
        };

        // Assign buffs/de-buffs
        Abilities["Q"].buff = new Buff(new RootEffect(), "Q", 3, 0);
        Abilities["E"].buff = new Buff(new SlowEffect(), "E", 2f, 0.02f);
        
        ICastingStrategy castingStrategy = new SinglePlayerCastingStrategy(this);

        if (GlobalState.IsMultiplayer) {
            castingStrategy = new MultiplayerCastingStrategy(gameObject, _rpc);
        }

        foreach (var ability in Abilities.Values) {
            ability.SetCastingStrategy(castingStrategy);
        }
    }
    
    /// <summary>
    /// Initialise the state manager
    /// </summary>
    private void InitStateManager() {
        StateManager = StateManager.Instance;
        _idleState = new IdleState(this);
        TransitionToIdle();
    }

    public void TransitionToIdle() {
        StateManager.ChangeState(_idleState);
    }

    public void TransitionToAttack() {
        StateManager.ChangeState(new AttackingState(gameObject));
    }

    public void TransitionToMove() {
        StateManager.ChangeState(new MovingState(gameObject,
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
    
    private void ResetCooldowns() {
        foreach (var ability in Abilities.Values) {
            ability.currentCooldown = 0;
        }
    }
    
    /// <summary>
    /// Apply a buff to this player
    /// </summary>
    /// <param name="buff"></param>
    public void ApplyBuff(Buff buff) {
        Debug.Log("ApplyBuff in player controller");
        ClientBuffManager.AddBuff(buff);
        buff.Effect.ApplyEffect(this, buff.EffectStrength);
    }

    /// <summary>
    /// Remove a buff from this player
    /// </summary>
    /// <param name="buff"></param>
    public void RemoveBuff(Buff buff) {
        buff.Effect.RemoveEffect(this, buff.EffectStrength);
    }
    
    private void OnDisable() {
        globalState.OnMultiplayerGameMode -= InitAbilities;
        globalState.OnSinglePlayerGameMode -= InitAbilities;
    }
    
    private void OnDestroy() {
        globalState.OnMultiplayerGameMode -= InitAbilities;
        globalState.OnSinglePlayerGameMode -= InitAbilities;
    }
}