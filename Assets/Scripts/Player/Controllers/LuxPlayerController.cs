using UnityEngine;
using System.Collections.Generic;
using Multiplayer;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using UnityEngine.Serialization;
using UnityEngine.VFX;

public class LuxPlayerController : LuxController {
    
    // PLayer Scripts
    public RPCController rpc;
    private InputController _input;
    
    // Skill-shot Projectile
    public List<GameObject> projectiles;
    public List<GameObject> vfxProjectileList;
    public Vector3 projectileTargetPosition;

    // AA Projectile
    public VisualEffect projectileAA;
    public Vector3 projectileAATargetPosition;
    public double timeSinceLastAttack = 0;

    // Hitbox
    public GameObject hitboxGameObj;
    public SphereCollider hitboxCollider;
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
    
    // Client
    private ClientManager _clientManager;
    public Client Client;

    // Abilities
    public List<AbilityEntry> AbilitiesList;
    public Dictionary<string, Ability> Abilities;
    
    // Health
    public Health health;
    public GameObject healthBarAnchor;
    
    // Camera
    public Camera mainCamera;
    private Plane _cameraPlane;

    private void Awake() {
        mainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        globalState = GameObject.Find("Global State").GetComponent<GlobalState>();
        networkAnimator = GetComponent<ClientNetworkAnimator>();
        _input = GetComponent<InputController>();

        healthBarAnchor = transform.Find("HealthBarAnchor").gameObject;
        hitboxGameObj = GameObject.Find("Hitbox");
        hitboxCollider = hitboxGameObj.GetComponent<SphereCollider>();
        hitboxPos = hitboxGameObj.transform.position;
        
        champion = Instantiate(championSO);
    }

    private void OnEnable() {
        globalState.OnMultiplayerGameMode += InitAbilities;
        globalState.OnSinglePlayerGameMode += InitAbilities;
    }
    
    private void Start() {
        
        if (GlobalState.IsMultiplayer && IsOwner) {
            _clientManager = ClientManager.Instance;
            //Client = _clientManager.Client;
        }

        ClientBuffManager = new ClientBuffManager(this);
        playerType = PlayerType.Player;
        health = GetComponent<Health>();
        Health.OnPlayerDeath += (async (player) => {
            if (this != player) return;
            Debug.Log("OnPlayerDeath");
            StateManager.ChangeState(new DeathState(this));
            ProcessPlayerDeath();
        });
        
        InitStates();
        TransitionToIdle();
        
        aiHitboxCollider = aiHitboxGameObj.GetComponent<SphereCollider>();
        _aiHitboxPos = aiHitboxGameObj.transform.position;

       
        projectiles = new List<GameObject>();
        ResetCooldowns();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        InitAbilities();
    }

    // Update is called once per frame

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

        rpc = GetComponent<RPCController>();
        
        if (AbilitiesList.Count == 0) {
            AbilitiesList.Add(new AbilityEntry { key = "Q", ability = Instantiate(champion.abilityQ) });
            AbilitiesList.Add(new AbilityEntry { key = "W", ability = Instantiate(champion.abilityW) });
            AbilitiesList.Add(new AbilityEntry { key = "E", ability = Instantiate(champion.abilityE) });
            AbilitiesList.Add(new AbilityEntry { key = "R", ability = Instantiate(champion.abilityR) });
        }
        
        // Initialize the Abilities dictionary using the list
        Abilities = new Dictionary<string, Ability>();
        foreach (var entry in AbilitiesList) {
            Abilities[entry.key] = entry.ability;
            BuffEffect buffEffect = new MoveSpeedEffect();
            Abilities[entry.key].buff = new Buff(buffEffect, entry.key, 3, 0);
        }
        
        ICastingStrategy castingStrategy = new SinglePlayerCastingStrategy(this);

        if (GlobalState.IsMultiplayer) {
            castingStrategy = new MultiplayerCastingStrategy(gameObject, rpc);
        }

        foreach (var ability in Abilities.Values) {
            ability.SetCastingStrategy(castingStrategy);
        }
    }
    
    private void InitStates() {
        StateManager = StateManager.Instance;
        _idleState = new IdleState(this);
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