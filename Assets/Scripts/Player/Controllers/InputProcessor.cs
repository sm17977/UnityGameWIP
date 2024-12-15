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
    [SerializeField] private GameObject aaRangeIndicatorPrefab;

    private InputController _input;

    private bool _isAARangeIndicatorOn;
    private GameObject _aaRangeIndicator;
    private GameObject _movementIndicator;

    private Queue<InputCommand> _inputQueue = new Queue<InputCommand>();

    private Camera _mainCamera;
    public Vector3 projectileTargetPosition;
    public Vector3 lastClickPosition;
    public Vector3 projectileAATargetPosition;
    
    
    private bool _isNewClick;
    public bool isAttackClick;
    public bool canCast;
    public bool incompleteMovement;
    public bool canAA;

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

    private void EnqueueInputCommand(InputCommand input) {
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
        var inputType = GetClickInput();
        if (inputType != InputCommandType.None) {
            // Enqueue a resolved input command (movement or attack)
            _inputQueue.Enqueue(new InputCommand { Type = inputType });
        }
    }

   /// <summary>
    ///  Detect if the player has clicked on the ground (movement command) or an enemy (attack command)
    ///  Store the position of the click in lastClickPosition
    ///  Return the input command type
    /// </summary>
    private InputCommandType GetClickInput() {
        if (_player.hitboxColliderRadius == 0 || _player.hitboxGameObj == null) return InputCommandType.None;
        
        // Get world space radius of the hitbox + account for scaling
        var worldRadius = _player.hitboxColliderRadius * _player.hitboxGameObj.transform.lossyScale.x;
        
        // Define a horizontal plane at the player's hitbox height
        Plane plane = new(Vector3.up, new Vector3(0, _player.hitboxPos.y, 0));
        
        // Get a ray from the camera to the mouse 
        var ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        // Detect attack click
        if (Physics.Raycast(ray, out var hit)) {
            if (hit.collider.name == "Lux_AI") {
                var luxAI = hit.rigidbody.gameObject;
                //ToggleOutlineShader(luxAI, "Default");
                isAttackClick = true;
                lastClickPosition = hit.transform.position;
                projectileAATargetPosition = hit.transform.position;
                return InputCommandType.Attack;
            }
        }

        // Detect move click
        if (plane.Raycast(ray, out var enter)) {
            //ToggleOutlineShader(luxAI, "Selection");
            var hitPoint = ray.GetPoint(enter);
            
            // Get vector from players hitbox to the mouse
            var diff = hitPoint - _player.hitboxPos;
            
            var dist = Mathf.Sqrt(diff.x * diff.x / (_mainCamera.aspect * _mainCamera.aspect) + diff.z * diff.z);
            //var dist = diff.magnitude;

            if (dist > worldRadius) {
                // Move click is valid (outside the players hitbox)
                lastClickPosition = new Vector3(hitPoint.x, transform.position.y, hitPoint.z);
                isAttackClick = false;
                return InputCommandType.Movement;
            }
        }
        
        return InputCommandType.None;
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
        if (!IsInAttackRange(lastClickPosition, GetStoppingDistance()))
            _player.StateManager.ChangeState(new MovingState(gameObject, true));
        else
            _player.StateManager.ChangeState(new AttackingState(gameObject));

        // Notify network about an attack command if needed
       // _networkStateManager.SendAttackCommand(lastClickPosition);
    }

    private void HandleSpellCommand(string key) {
        if (_player.Abilities.TryGetValue(key, out Ability ability)) {
            ability.OnCooldown_Net(gameObject, (bool networkCooldown) => {
                if (!networkCooldown) {
                    NotifyUICooldown?.Invoke(key, ability.maxCooldown);
                    GetCastingTargetPosition();
                    _player.StateManager.ChangeState(new CastingState(gameObject, ability));
                    _networkStateManager.SendSpellCommand(key, ability);
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

    private bool IsInAttackRange(Vector3 targetLocation, float stoppingDistance) {
        return Vector3.Distance(transform.position, targetLocation) <= stoppingDistance;
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
}
