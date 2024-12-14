using System;
using System.Collections.Generic;
using Kart;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.VFX;

public class InputController : NetworkBehaviour {
    
    // Events
    public event Action<string, float>? NotifyUICooldown;
    
    // Player Scripts
    private LuxPlayerController _player;
    private RPCController _rpc;
    
    // Camera
    private Camera _mainCamera;
    private Plane _cameraPlane;
    
    // AA Projectile
    public VisualEffect projectileAA;
    public Vector3 projectileAATargetPosition;
    public double timeSinceLastAttack = 0;
    
    public Vector3 projectileTargetPosition;
    
    // Client prediction and reconciliation
    public NetworkTimer NetworkTimer;
    private CircularBuffer<InputPayload> clientInputBuffer;
    private CircularBuffer<StatePayload> clientStateBuffer;
    public Queue<InputPayload> serverInputQueue;
    private CircularBuffer<StatePayload> serverStateBuffer;
    public StatePayload lastServerState;
    private StatePayload lastProcessedState;
    private const int k_bufferSize = 1024;
    private const float reconciliationThreshold = 0.5f; // Adjust as needed
    
    // Input Data
    private Controls _controls;
    public Vector3 lastClickPosition;
    private Queue<InputCommand> _inputQueue;
    private InputCommand _previousInput;
    private InputCommand _currentInput;
    
    // Flags
    public bool isAttackClick;
    public bool canCast;
    public bool canAA;
    public bool incompleteMovement;
    private bool _isNewClick;
    
    // AA Range Indicator
    public GameObject aaRangeIndicatorPrefab;
    private GameObject _aaRangeIndicator;
    private bool _isAARangeIndicatorOn;
    
    // Movement Indicator
    public GameObject movementIndicatorPrefab;
    private GameObject _movementIndicator;
    
    private void Awake() {
        _player = GetComponent<LuxPlayerController>();
        _rpc = GetComponent<RPCController>();
        _mainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        
        _previousInput = new InputCommand { Type = InputCommandType.Init };
        
        NetworkTimer = new NetworkTimer(50);
        clientInputBuffer = new CircularBuffer<InputPayload>(k_bufferSize);
        clientStateBuffer = new CircularBuffer<StatePayload>(k_bufferSize);
        
        serverStateBuffer = new CircularBuffer<StatePayload>(k_bufferSize);
        serverInputQueue = new Queue<InputPayload>();
    }
    
    private void OnEnable() {
        _controls = new Controls();
        _controls.Player.Enable();
        _controls.Player.RightClick.performed += OnRightClick;
        _controls.Player.Q.performed += OnQ;
        _controls.Player.E.performed += OnE;
        _controls.Player.A.performed += OnA;
    }

    private void Start() {
        _inputQueue = new Queue<InputCommand>();
    }

    private void Update() {
        NetworkTimer.Update(Time.deltaTime);
    }

    private void FixedUpdate() {
        while (NetworkTimer.ShouldTick()) {
            HandleClientTick();
            HandleServerTick();
        }
    }


    /// <summary>
    /// Movement/AA input
    /// </summary>
    /// <param name="context"></param>
    public void OnRightClick(InputAction.CallbackContext context) {
        if (GlobalState.Paused) return;

        _isNewClick = true;
        var inputType = GetClickInput();
        
        AddInputToQueue(new InputCommand { Type = inputType } );
    }

    /// <summary>
    ///  Q Spell 
    /// </summary>
    /// <param name="context"></param>
    public void OnQ(InputAction.CallbackContext context) {
        if (_player.Abilities.TryGetValue("Q", out Ability ability)) {
            AddInputToQueue(new InputCommand { Type = InputCommandType.CastSpell, Key = "Q", Ability = ability });
        }
    }

    /// <summary>
    /// E Spell 
    /// </summary>
    /// <param name="context"></param>
    public void OnE(InputAction.CallbackContext context) {
        if (_player.Abilities.TryGetValue("E", out Ability ability)) {
            AddInputToQueue(new InputCommand { Type = InputCommandType.CastSpell, Key = "E", Ability = ability });
        }
    }

    /// <summary>
    /// Toggle the Auto Attack range UI indicator
    /// </summary>
    /// <param name="context"></param>
    public void OnA(InputAction.CallbackContext context) {
        ToggleAARange();
    }
    
    // Detect if the player has clicked on the ground (movement command) or an enemy (attack command)
    // Store the position of the click in lastClickPosition
    // Return the input command type
    private InputCommandType GetClickInput() {
        if (_player.hitboxColliderRadius == 0 || _player.hitboxGameObj == null) return InputCommandType.None;
        
        var worldRadius = _player.hitboxColliderRadius * _player.hitboxGameObj.transform.lossyScale.x;
        Plane plane = new(Vector3.up, new Vector3(0, _player.hitboxPos.y, 0));
        var ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        // Detect attack click
        if (Physics.Raycast(ray, out var hit))
            if (hit.collider.name == "Lux_AI") {
                var luxAI = hit.rigidbody.gameObject;
                ToggleOutlineShader(luxAI, "Default");
                isAttackClick = true;
                lastClickPosition = hit.transform.position;
                projectileAATargetPosition = hit.transform.position;
                return InputCommandType.Attack;
            }

        // Detect move click
        if (plane.Raycast(ray, out var enter)) {
            //ToggleOutlineShader(luxAI, "Selection");
            var hitPoint = ray.GetPoint(enter);
            var diff = hitPoint - _player.hitboxPos;
            var dist = Mathf.Sqrt(diff.x * diff.x / (_mainCamera.aspect * _mainCamera.aspect) + diff.z * diff.z);

            if (dist > worldRadius) {
                // Move click is valid (outside the players hitbox)
                lastClickPosition = new Vector3(hitPoint.x, transform.position.y, hitPoint.z);
                isAttackClick = false;
                return InputCommandType.Movement;
            }
        }

        // Default to None type
        return InputCommandType.None;
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

                    ShowMovementIndicator(lastClickPosition);
                    
                    if (_player.canMove) {
                        _player.StateManager.ChangeState(new MovingState(gameObject,
                            false));
                    }

                    _inputQueue.Dequeue();
                    break;

                //Process the attack command
                case InputCommandType.Attack:
                    _player.direction = (lastClickPosition - transform.position).normalized;
                    // If the player is not within attack range, move until they are
                    if (!IsInAttackRange(lastClickPosition, GetStoppingDistance()))
                        _player.StateManager.ChangeState(new MovingState(gameObject, true));
                    // If they are in attack range, transition to attack state
                    // Attacking state persists until we input a new command
                    else
                        _player.StateManager.ChangeState(new AttackingState(gameObject));
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
                                _player.StateManager.ChangeState(new CastingState(gameObject, inputAbility));
                            }
                        });
                    }
                    else {
                        GetCastingTargetPosition();
                        _player.StateManager.ChangeState(new CastingState(gameObject, inputAbility));
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
    
    /// <summary>
    /// Get mouse position
    /// </summary>
    private void GetCastingTargetPosition() {
        var plane = new Plane(Vector3.up, new Vector3(0, _player.hitboxPos.y, 0));
        var ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        projectileTargetPosition = transform.position;

        if (plane.Raycast(ray, out var enter)) {
            var hitPoint = ray.GetPoint(enter);
            projectileTargetPosition = new Vector3(hitPoint.x, transform.position.y, hitPoint.z);
        }
    }
    
    /// <summary>
    /// Get the minimum distance the player must move to reach the target location
    /// </summary>
    /// <returns></returns>
    public float GetStoppingDistance() {
        var distance = _player.champion.stoppingDistance;

        // If we inputted an attack click, the distance is calculated from the edge of the players attack range and the center of the enemy
        if (isAttackClick) {
            var calculatedAttackRange = _player.champion.AA_range * 10 / 2;
            distance = calculatedAttackRange + _player.hitboxColliderRadius;
        }

        return distance;
    }
    
    public void HandleClientTick() {
        if (!IsClient || !IsOwner) return;

        var currentTick = NetworkTimer.CurrentTick;
        var bufferIndex = currentTick % k_bufferSize;
            
        InputPayload inputPayload = new InputPayload() {
            Tick = currentTick,
            TargetPosition = lastClickPosition
        };
            
        clientInputBuffer.Add(inputPayload, bufferIndex);
        _rpc.SendInputRpc(inputPayload);
            
        HandleInput();
        clientStateBuffer.Add(new StatePayload() {
            Position = transform.position,
            TargetPosition = lastClickPosition
        }, bufferIndex);
    }
    
    public void HandleServerTick() {
        if (!IsServer) return;
             
        var bufferIndex = -1;
        InputPayload inputPayload = default;
        
        while (serverInputQueue.Count > 0) {
            inputPayload = serverInputQueue.Dequeue();
            bufferIndex = inputPayload.Tick % k_bufferSize;

            lastClickPosition = inputPayload.TargetPosition;
            AddInputToQueue(new InputCommand { Type = InputCommandType.Movement });
            HandleInput();
            StatePayload statePayload = new StatePayload() {
                Position = transform.position,
                TargetPosition = lastClickPosition
            };
            serverStateBuffer.Add(statePayload, bufferIndex);
        }
            
        if (bufferIndex == -1) return;
        _rpc.SendStateRpc(serverStateBuffer.Get(bufferIndex));
        
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
    
    private bool IsInAttackRange(Vector3 targetLocation, float stoppingDistance) {
        if (Vector3.Distance(transform.position, targetLocation) <= stoppingDistance) return true;
        return false;
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
    
    // Renders a quick animation on the terrain whenever the right mouse is clicked 
    public void ShowMovementIndicator(Vector3 position) {
        if (_isNewClick) {
            if (_movementIndicator != null) Destroy(_movementIndicator);

            _movementIndicator = Instantiate(movementIndicatorPrefab, new Vector3(position.x, 0.51f, position.z),
                Quaternion.Euler(90, 0, 0));
            _isNewClick = false;
        }
    }
    
    private void OnDisable() {
        _controls.Player.RightClick.performed -= OnRightClick;
        _controls.Player.Q.performed -= OnQ;
        _controls.Player.E.performed -= OnE;
        _controls.Player.A.performed -= OnA;
        _controls.Player.Disable();
    }

    private void OnDestroy() {
        if (_controls != null) {
            _controls.Player.RightClick.performed -= OnRightClick;
            _controls.Player.Q.performed -= OnQ;
            _controls.Player.E.performed -= OnE;
            _controls.Player.A.performed -= OnA;
            _controls.Player.Disable();
        }
    }
}
