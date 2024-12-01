using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using Multiplayer;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.VFX;

public class LuxPlayerController : LuxController {

    // Events
    public event Action<string, float>? NotifyUICooldown;
    
    // Flags
    public bool isAttackClick = false;
    public bool canCast = false;
    public bool canAA = false;
    public bool incompleteMovement = false;
    private bool _isNewClick;

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
    private Vector3 _hitboxPos;

    // Movement Indicator
    public GameObject movementIndicatorPrefab;
    private GameObject _movementIndicator;

    // Animation
    public Animator animator;
    public ClientNetworkAnimator networkAnimator;
    
    // Camera
    public Camera mainCamera;
    private Plane _cameraPlane;

    // Input Data
    private Controls _controls;
    private Queue<InputCommand> _inputQueue;
    private InputCommand _previousInput;
    private InputCommand _currentInput;
    private Vector3 _lastClickPosition;

    // AA Range Indicator
    public GameObject aaRangeIndicatorPrefab;
    private GameObject _aaRangeIndicator;
    private bool _isAARangeIndicatorOn;

    // State Management
    private StateManager _stateManager;
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

    // Debugging stuff
    public float attackStartTime;
    public bool attackStartTimeLogged = false;
    
    // RPC Manager
    public RPCController rpcController;
    
    // Client
    private ClientManager _clientManager;
    public Client Client;

    // Abilities
    public List<AbilityEntry> AbilitiesList;
    public Dictionary<string, Ability> Abilities;
    
    //Buffs
    
    // Health
    public Health health;
    public GameObject healthBarAnchor;

    private void Awake() {
        mainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        globalState = GameObject.Find("Global State").GetComponent<GlobalState>();
        networkAnimator = GetComponent<ClientNetworkAnimator>();

        healthBarAnchor = transform.Find("HealthBarAnchor").gameObject;
        hitboxGameObj = GameObject.Find("Hitbox");
        hitboxCollider = hitboxGameObj.GetComponent<SphereCollider>();
        _hitboxPos = hitboxGameObj.transform.position;
        
        _previousInput = new InputCommand { Type = InputCommandType.Init };
        champion = Instantiate(championSO);
    }

    private void OnEnable() {
        _controls = new Controls();
        _controls.Player.Enable();
        _controls.Player.RightClick.performed += OnRightClick;
        _controls.Player.Q.performed += OnQ;
        _controls.Player.E.performed += OnE;
        _controls.Player.A.performed += OnA;
        globalState.OnMultiplayerGameMode += InitAbilities;
        globalState.OnSinglePlayerGameMode += InitAbilities;
    }

    private void OnDisable() {
        // Disable Input Actions/Events
        _controls.Player.RightClick.performed -= OnRightClick;
        _controls.Player.Q.performed -= OnQ;
        _controls.Player.E.performed -= OnE;
        _controls.Player.A.performed -= OnA;
        _controls.Player.Disable();
        globalState.OnMultiplayerGameMode -= InitAbilities;
        globalState.OnSinglePlayerGameMode -= InitAbilities;
    }
    
    // Start is called before the first frame update
    private void Start() {
        if (GlobalState.IsMultiplayer && IsOwner) {
            _clientManager = ClientManager.Instance;
            Client = _clientManager.Client;
        }

        BuffManager = new BuffManager(this);
        playerType = PlayerType.Player;
        health = GetComponent<Health>();
        Health.OnPlayerDeath += (async (player) => {
            if (this != player) return;
            Debug.Log("OnPlayerDeath");
            _stateManager.ChangeState(new DeathState(this));
            ProcessPlayerDeath();
        });
        
        InitStates();
        TransitionToIdle();
        
        aiHitboxCollider = aiHitboxGameObj.GetComponent<SphereCollider>();
        _aiHitboxPos = aiHitboxGameObj.transform.position;

        _inputQueue = new Queue<InputCommand>();
        projectiles = new List<GameObject>();
        ResetCooldowns();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        InitAbilities();
    }

    // Update is called once per frame

    private void Update() {
        if (globalState.currentScene == "Multiplayer" && !IsOwner) return;

        if(hitboxGameObj != null) _hitboxPos = hitboxGameObj.transform.position;
        
        HandleInput();

        _stateManager.Update();

        BuffManager.Update();

        currentState = _stateManager.GetCurrentState();

        if (_stateManager.GetCurrentState() != "AttackingState")
            if (timeSinceLastAttack > 0)
                timeSinceLastAttack -= Time.deltaTime;

        HandleProjectiles();
        HandleVFX();
    }
    
    /// <summary>
    /// Store the players abilities in a list
    /// Set the casting strategy for the ability depending on singleplayer/multiplayer
    /// </summary>
    private void InitAbilities() {

        rpcController = GetComponent<RPCController>();
        
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
            castingStrategy = new MultiplayerCastingStrategy(gameObject, rpcController);
        }

        foreach (var ability in Abilities.Values) {
            ability.SetCastingStrategy(castingStrategy);
        }
    }


    public void OnRightClick(InputAction.CallbackContext context) {
        if (GlobalState.Paused) return;

        _isNewClick = true;
        var inputType = GetClickInput();
        
        AddInputToQueue(new InputCommand { Type = inputType } );
    }

    public void OnQ(InputAction.CallbackContext context) {
        if (Abilities.TryGetValue("Q", out Ability ability)) {
            AddInputToQueue(new InputCommand { Type = InputCommandType.CastSpell, Key = "Q", Ability = ability });
        }
    }

    public void OnE(InputAction.CallbackContext context) {
        if (Abilities.TryGetValue("E", out Ability ability)) {
            AddInputToQueue(new InputCommand { Type = InputCommandType.CastSpell, Key = "E", Ability = ability });
        }
    }

    public void OnA(InputAction.CallbackContext context) {
        ToggleAARange();
    }

    private void InitStates() {
        _stateManager = StateManager.Instance;
        _idleState = new IdleState(this);
    }

    // Read the input queue to handle movement and casting input commands
    private void HandleInput() {
        // Check the queue for any unconsumed input commands
        if (_inputQueue.Count > 0) {
            // Get the next input command
            _currentInput = _inputQueue.Peek();

            // Set previous input to null on first function call
            if (_previousInput.Type == InputCommandType.Init) _previousInput = _currentInput;

            switch (_currentInput.Type) {
                // Process the movement command
                case InputCommandType.Movement:

                    ShowMovementIndicator(_lastClickPosition);
                    if (canMove) {
                        _movingState = new MovingState(this, _lastClickPosition, GetStoppingDistance(), gameObject,
                            false);
                        _stateManager.ChangeState(_movingState);
                    }

                    _inputQueue.Dequeue();
                    break;

                //Process the attack command
                case InputCommandType.Attack:
                    direction = (_lastClickPosition - transform.position).normalized;
                    // If the player is not within attack range, move until they are
                    if (!IsInAttackRange(_lastClickPosition, GetStoppingDistance()))
                        _stateManager.ChangeState(new MovingState(this, _lastClickPosition, GetStoppingDistance(),
                            gameObject, true));
                    // If they are in attack range, transition to attack state
                    // Attacking state persists until we input a new command
                    else
                        _stateManager.ChangeState(new AttackingState(this, direction, gameObject, champion.windupTime));
                    _inputQueue.Dequeue();
                    break;

                // Process the cast spell command
                case InputCommandType.CastSpell:

                    var inputAbility = _currentInput.Ability;
                    var abilityOnCooldown = inputAbility.OnCooldown();
                    if (abilityOnCooldown) {
                        _inputQueue.Dequeue();
                        break;
                    }
                    
                    if (GlobalState.IsMultiplayer) {
                        inputAbility.OnCooldown_Net(gameObject, (bool networkCooldown) => {
                            if (!networkCooldown) {
                                // Invoke UI HUD ability animation;
                                NotifyUICooldown?.Invoke(_currentInput.Key, inputAbility.maxCooldown);
                                GetCastingTargetPosition();
                                _stateManager.ChangeState(new CastingState(this, gameObject, inputAbility));
                            }
                        });
                    }
                    else {
                        GetCastingTargetPosition();
                        _stateManager.ChangeState(new CastingState(this, gameObject, inputAbility));
                    }
                    
                    _inputQueue.Dequeue(); 
                    break;

                case InputCommandType.None:
                    _inputQueue.Dequeue();
                    break;
            }
        }
    }

    private void AddInputToQueue(InputCommand input) {
        _inputQueue.Enqueue(input);
    }

    // Detect if the player has clicked on the ground (movement command) or an enemy (attack command)
    // Store the position of the click in lastClickPosition
    // Return the input command type
    private InputCommandType GetClickInput() {
        if (hitboxCollider == null || hitboxGameObj == null) return InputCommandType.None;
        
        var worldRadius = hitboxCollider.radius * hitboxGameObj.transform.lossyScale.x;
        Plane plane = new(Vector3.up, new Vector3(0, _hitboxPos.y, 0));
        var ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        // Detect attack click
        if (Physics.Raycast(ray, out var hit))
            if (hit.collider.name == "Lux_AI") {
                var Lux_AI = hit.rigidbody.gameObject;
                ToggleOutlineShader(Lux_AI, "Default");
                isAttackClick = true;
                _lastClickPosition = hit.transform.position;
                projectileAATargetPosition = hit.transform.position;
                return InputCommandType.Attack;
            }

        // Detect move click
        if (plane.Raycast(ray, out var enter)) {
            ToggleOutlineShader(luxAI, "Selection");
            var hitPoint = ray.GetPoint(enter);
            var diff = hitPoint - _hitboxPos;
            var dist = Mathf.Sqrt(diff.x * diff.x / (mainCamera.aspect * mainCamera.aspect) + diff.z * diff.z);

            if (dist > worldRadius) {
                // Move click is valid (outside the players hitbox)
                _lastClickPosition = new Vector3(hitPoint.x, transform.position.y, hitPoint.z);
                isAttackClick = false;
                return InputCommandType.Movement;
            }
        }

        // Default to None type
        return InputCommandType.None;
    }

    // Set the AI game object layer to default so the outline renders ('Selection' layer = outline off)
    private void ToggleOutlineShader(GameObject Lux_AI, string mask) {
        ChangeLayerRecursively(Lux_AI, LayerMask.NameToLayer(mask));
    }

    private void ChangeLayerRecursively(GameObject obj, int newLayer) {
        if (obj == null) return;
        obj.layer = newLayer;

        foreach (Transform child in obj.transform) ChangeLayerRecursively(child.gameObject, newLayer);
    }

    private void GetCastingTargetPosition() {
        var plane = new Plane(Vector3.up, new Vector3(0, _hitboxPos.y, 0));
        var ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        projectileTargetPosition = transform.position;

        if (plane.Raycast(ray, out var enter)) {
            var hitPoint = ray.GetPoint(enter);
            projectileTargetPosition = new Vector3(hitPoint.x, transform.position.y, hitPoint.z);
        }
    }

    // Get the minimum distance the player must move to reach the target location
    private float GetStoppingDistance() {
        var distance = champion.stoppingDistance;

        // If we inputted an attack click, the distance is calculated from the edge of the players attack range and the center of the enemy
        if (isAttackClick) {
            var calculatedAttackRange = champion.AA_range * 10 / 2;
            distance = calculatedAttackRange + hitboxCollider.radius;
        }

        return distance;
    }

    private bool IsInAttackRange(Vector3 targetLocation, float stoppingDistance) {
        if (Vector3.Distance(transform.position, targetLocation) <= stoppingDistance) return true;
        return false;
    }

    // Renders a quick animation on the terrain whenever the right mouse is clicked 
    public void ShowMovementIndicator(Vector3 position) {
        if (_isNewClick) {
            if (_movementIndicator != null) Destroy(_movementIndicator);

            _movementIndicator = Instantiate(movementIndicatorPrefab, new Vector3(position.x, 0.51f, position.z),
                Quaternion.Euler(90, 0, 0));
            _isNewClick = false;
        }
    }

    public void TransitionToIdle() {
        _stateManager.ChangeState(_idleState);
    }

    public void TransitionToAttack() {
        _stateManager.ChangeState(new AttackingState(this, direction, gameObject, champion.windupTime));
    }

    public void TransitionToMove() {
        _stateManager.ChangeState(new MovingState(this, _lastClickPosition, GetStoppingDistance(), gameObject,
            isAttackClick));
    }

    public void RotateTowardsTarget(Vector3 direction) {
        if (direction != Vector3.zero) {
            var toRotation = Quaternion.LookRotation(direction, Vector3.up);
            var fromRotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            transform.rotation = Quaternion.Slerp(fromRotation, toRotation, champion.turnSpeed * Time.deltaTime);
        }
    }

    private void HandleProjectiles() {
        if (projectiles.Count > 0)
            for (var i = 0; i < projectiles.Count; i++) {
                var projectile = projectiles[i];
                if (projectile != null && projectile.activeSelf) {
                    var missile = projectile.GetComponent<ProjectileAbility>();
                    if (missile.canBeDestroyed) {
                        projectiles.RemoveAt(i);
                        Destroy(projectile);
                    }
                }
            }
    }

    private void HandleVFX() {
        if (vfxProjectileList.Count > 0)
            for (var i = 0; i < vfxProjectileList.Count; i++) {
                var projectile = vfxProjectileList[i];
                if (projectile != null && projectile.activeSelf) {
                    var controller = projectile.GetComponent<VFXController>();
                    if (controller.die) {
                        vfxProjectileList.RemoveAt(i);
                        Destroy(projectile.gameObject);
                    }
                }
            }
    }

    private void ToggleAARange() {
        if (!_isAARangeIndicatorOn) {
            _isAARangeIndicatorOn = true;
            _aaRangeIndicator = Instantiate(aaRangeIndicatorPrefab, transform);
        }
        else {
            Destroy(_aaRangeIndicator);
            _isAARangeIndicatorOn = false;
        }
    }
    
    private void ResetCooldowns() {
        foreach (var ability in Abilities.Values) {
            ability.currentCooldown = 0;
        }
    }
    
    private void OnDestroy() {
        if (_controls != null) {
            _controls.Player.RightClick.performed -= OnRightClick;
            _controls.Player.Q.performed -= OnQ;
            _controls.Player.E.performed -= OnE;
            _controls.Player.A.performed -= OnA;
            _controls.Player.Disable();
        }

        globalState.OnMultiplayerGameMode -= InitAbilities;
        globalState.OnSinglePlayerGameMode -= InitAbilities;
    }
    
    /// <summary>
    /// Apply a buff to this player
    /// </summary>
    /// <param name="buff"></param>
    public void ApplyBuff(Buff buff) {
        BuffManager.AddBuff(buff);
        buff.Effect.ApplyEffect(this, buff.EffectStrength);
    }

    /// <summary>
    /// Remove a buff from this player
    /// </summary>
    /// <param name="buff"></param>
    public void RemoveBuff(Buff buff) {
        buff.Effect.RemoveEffect(this, buff.EffectStrength);
    }
}