using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

public class InputProcessor : NetworkBehaviour {
    
    public event Action<string, float>? NotifyUICooldown;
    
    [SerializeField] private LuxPlayerController _player;
    [SerializeField] private NetworkStateManager _networkStateManager;
    [SerializeField] private GameObject movementIndicatorPrefab;
    [SerializeField] private GameObject spellIndicatorPrefab;
    [SerializeField] private GameObject aaRangeIndicatorPrefab;

    private InputController _input;
    
    private bool _isAARangeIndicatorOn;
    private GameObject _aaRangeIndicator;
    private GameObject _movementIndicator;
    private GameObject _spellIndicator;

    private Queue<InputCommand> _inputQueue = new Queue<InputCommand>();

    private Camera _mainCamera;
    public Vector3 projectileTargetPosition;
    public Vector3 lastClickPosition;
    public Vector3 projectileAATargetPosition;
    
    public bool isAttackClick;
    public bool canCast;
    public bool incompleteMovement;
    public bool canAA;

    public bool isTyping;
    
    private bool _isNewClick;
    private bool _showSpellIndicator;
    private GameObject _lastSelectedEnemy;

    public static readonly float floorY = 0.5f;

    private void Awake() {
        _mainCamera = Camera.main;
        _input = GetComponent<InputController>();
    }

    private void OnEnable() {
        _input.OnInputCommandIssued += EnqueueInputCommand;
    }

    private void OnDisable() {
        _input.OnInputCommandIssued -= EnqueueInputCommand;
    }

    private void FixedUpdate() {
        if (IsOwner) {
            ProcessCommands();
        }
    }

    private void Update() {
        DetectEnemySelection();
    }

    private void EnqueueInputCommand(InputCommand input) {
        if(isTyping) return;
        _inputQueue.Enqueue(input);
    }

    private void ProcessCommands() {
        while (_inputQueue.Count > 0) {
            var command = _inputQueue.Dequeue();
            switch (command.Type) {
                case InputCommandType.RightClick:
                    HandleRightClick();
                    break;
                case InputCommandType.Movement:
                    HandleMovementCommand();
                    break;
                case InputCommandType.Attack:
                    HandleAttackCommand();
                    break;
                case InputCommandType.CastSpell:
                    HandleSpellCommand(command.Key);
                    break;
                case InputCommandType.ToggleAARange:
                    ToggleAARange();
                    break;
                case InputCommandType.None:
                default:
                    break;
            }
        }
    }

    private void HandleRightClick() {
        _isNewClick = true;
        _showSpellIndicator = false;
        HideSpellIndicator();
        var inputType = GetClickInput();
        if (inputType != InputCommandType.None) {
            _inputQueue.Enqueue(new InputCommand { Type = inputType });
        }
    }

    /// <summary>
    ///  Detect if the player has clicked on the ground (movement command) or an enemy (attack command)
    ///  Store the position of the click in lastClickPosition
    ///  Return the input command type
    /// </summary>
    private InputCommandType GetClickInput() {
        if (_player.hitboxColliderRadius == 0 || _player.hitboxGameObj == null) 
            return InputCommandType.None;

        if (IsAttackClick()) 
            return InputCommandType.Attack;
        
        // Get world space radius of the hitbox + account for scaling
        var worldRadius = _player.hitboxColliderRadius * _player.hitboxGameObj.transform.lossyScale.x;
        var mousePos = GetMousePosition();
            
        // Get vector from players hitbox to the mouse
        var diff = mousePos - _player.hitboxPos;
        var dist = Mathf.Sqrt(diff.x * diff.x / (_mainCamera.aspect * _mainCamera.aspect) + diff.z * diff.z);
        
        if (dist > worldRadius) {
            // Move click is valid (outside the players hitbox)
            lastClickPosition = new Vector3(mousePos.x, floorY, mousePos.z);
            isAttackClick = false;
            _player.currentAATarget = null;
            return InputCommandType.Movement;
        }
        return InputCommandType.None;
    }

    private bool IsAttackClick() {
        var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out var hit)) {
            var obj = hit.transform.gameObject;
            if (obj.CompareTag("Player") && obj != gameObject) {
                Debug.Log("Clicked on: " + obj.name);
                var enemyHitbox = obj.GetComponent<LuxPlayerController>().hitboxGameObj;
                projectileAATargetPosition = enemyHitbox.transform.position;
                lastClickPosition = GetHitboxEdge(hit.point, enemyHitbox);
                isAttackClick = true;
                _player.currentAATarget = obj;
                return true;
            }
        }
        
        return false;
    }

    private Vector3 GetHitboxEdge(Vector3 clickPos, GameObject hitbox) {
        var hitboxCollider = hitbox.GetComponent<SphereCollider>();
        var directionToHitboxEdge = (clickPos - hitbox.transform.position).normalized;
        var edge = hitbox.transform.position + directionToHitboxEdge * hitboxCollider.radius;
        edge.y = floorY;
        return edge;
    }
    
    public Vector3 GetCurrentHitboxEdge() {
        var enemyHitbox = _player.currentAATarget.GetComponent<LuxPlayerController>().hitboxGameObj;
        var directionToHitboxEdge = (transform.position - enemyHitbox.transform.position).normalized;
        var edge = enemyHitbox.transform.position + directionToHitboxEdge * enemyHitbox.GetComponent<SphereCollider>().radius;
        edge.y = floorY;
        return edge;
    }
    
    public static Vector3 GetMousePosition() {
        // Define a horizontal plane at the floor height
        Plane plane = new(Vector3.up, new Vector3(0, floorY, 0));
        
        // Get a ray from the camera to the mouse 
        var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        // Detect move click
        return plane.Raycast(ray, out var enter) ? ray.GetPoint(enter) : new Vector3();
    }

    private void HandleMovementCommand() {
        ShowMovementIndicator(lastClickPosition);
        if (_player.canMove.Value) {
            _player.StateManager.ChangeState(new MovingState(gameObject, false));
            _networkStateManager.SendMoveCommand(lastClickPosition);
        }
    }

    private void HandleAttackCommand() {
        _player.direction = (lastClickPosition - transform.position).normalized;
        if (!IsInAttackRange(lastClickPosition))
            _player.StateManager.ChangeState(new MovingState(gameObject, true));
        else
            _player.StateManager.ChangeState(new AttackingState(gameObject));
        
        // _networkStateManager.SendAttackCommand(lastClickPosition);
    }

    private void HandleSpellCommand(string key) {
        
        if (_player.Abilities.TryGetValue(key, out Ability ability)) {
            
            spellIndicatorPrefab = ability.spellIndicatorPrefab;
            
            ability.OnCooldown_Net(gameObject, (bool networkCooldown) => {
                // Check cooldown before casting
                if (!networkCooldown) {
                    
                    // Spell indicator isn't already showing, show it and don't cast
                    if (!_showSpellIndicator) {
                        _showSpellIndicator = true;
                        ShowSpellIndicator(ability); 
                    }
                    // Otherwise remove it and cast
                    else {
                        _showSpellIndicator = false;
                        HideSpellIndicator();
                        NotifyUICooldown?.Invoke(key, ability.maxCooldown);
                        GetCastingTargetPosition();
                        _player.StateManager.ChangeState(new CastingState(gameObject, ability));
                        _networkStateManager.SendSpellCommand(key, ability);
                        ability.canRecast = ability.hasRecast;
                    }
                }
                // If ability is on CD and it's re-castable
                else if (ability.hasRecast && ability.canRecast) {
                    _player.StateManager.ChangeState(new CastingState(gameObject, ability, true));
                    ability.canRecast = false;
                }
            });
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

    private void ShowMovementIndicator(Vector3 position) {
        if (_isNewClick) {
            if (_movementIndicator != null) Destroy(_movementIndicator);

            _movementIndicator = Instantiate(movementIndicatorPrefab, new Vector3(position.x, 0.51f, position.z),
                Quaternion.Euler(90, 0, 0));
            _isNewClick = false;
        }
    }
    
    private void ShowSpellIndicator(Ability ability) {
        _spellIndicator = Instantiate(
            spellIndicatorPrefab,
            new Vector3(transform.position.x, 0.51f, transform.position.z), 
            spellIndicatorPrefab.transform.rotation
        );
        var spellIndicatorScript = _spellIndicator.GetComponent<BaseSpellIndicator>();
        spellIndicatorScript.InitializeProperties(ability);
    }

    private void HideSpellIndicator() {
        if (_spellIndicator != null) Destroy(_spellIndicator);
    }
    
    public bool IsInAttackRange(Vector3 targetLocation) {
        
        if (isAttackClick && _player.currentAATarget != null) {
            float currentEdgeDistance = GetCurrentEdgeDistance();
            Debug.Log("Is in attack range: " + currentEdgeDistance + " | AA Range: " + _player.champion.AA_range + 1);
            return currentEdgeDistance <= _player.champion.AA_range + 1;
        }
        
        float stoppingDistance = GetStoppingDistance();
        return Vector3.Distance(transform.position, targetLocation) <= stoppingDistance;
    }

    /// <summary>
    /// Get the minimum distance the player must move to reach the target location
    /// </summary>
    /// <returns></returns>
    public float GetStoppingDistance() {
        return _player.champion.stoppingDistance;
    }
    
    /// <summary>
    /// Get mouse position
    /// </summary>
    private void GetCastingTargetPosition() {
        // Define a horizontal plane at the player's hitbox y position
        var plane = new Plane(Vector3.up, new Vector3(0, _player.hitboxPos.y, 0));
        // Get a ray from the camera to the mouse
        var ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        projectileTargetPosition = transform.position; // Setting a default, not sure if we still need this

        // Check if the ray intersects with the plane, if it intersects we can find the projectile target position
        if (plane.Raycast(ray, out var enter)) {
            var hitPoint = ray.GetPoint(enter);
            projectileTargetPosition = new Vector3(hitPoint.x, transform.position.y, hitPoint.z);
        }
    }
    
    public float GetCurrentEdgeDistance() {
        // Recalculate the distance.
        var playerCenter = new Vector3(
            _player.hitboxGameObj.transform.position.x,
            InputProcessor.floorY,
            _player.hitboxGameObj.transform.position.z);

        var enemyController = _player.currentAATarget.GetComponent<LuxPlayerController>();
        var enemyCenter = new Vector3(
            enemyController.hitboxGameObj.transform.position.x,
            InputProcessor.floorY,
            enemyController.hitboxGameObj.transform.position.z);

        float centerDistance = Vector3.Distance(playerCenter, enemyCenter);
        float currentEdgeDistance = Mathf.Max(0,
            centerDistance - (_player.hitboxColliderRadius + enemyController.hitboxColliderRadius));
        //
        // Debug.Log("Current edge-to-edge distance: " + currentEdgeDistance + " | AA Range: " +
        //           _player.champion.AA_range);

        return currentEdgeDistance;
    }

    private void DetectEnemySelection() {
        if (!IsServer) {
            var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out var hit)) {
                var hitObj = hit.transform.gameObject;
                if (hitObj.CompareTag("Player") && hitObj.name != "Local Player") {
                    _lastSelectedEnemy = hitObj;
                    _lastSelectedEnemy.layer = LayerMask.NameToLayer("Selection");
                    foreach (Transform child in _lastSelectedEnemy.transform) {
                        child.gameObject.layer = LayerMask.NameToLayer("Selection");
                    }
                }
                else if (_lastSelectedEnemy != null) {
                    _lastSelectedEnemy.layer = LayerMask.NameToLayer("Default");
                    foreach (Transform child in _lastSelectedEnemy.transform) {
                        child.gameObject.layer = LayerMask.NameToLayer("Default");
                    }
                }
            }
        }
    }
}
