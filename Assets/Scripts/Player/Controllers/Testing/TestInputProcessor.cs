using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

public class TestInputProcessor : NetworkBehaviour {
    
    [SerializeField] private BarebonesPlayerController _player;
    [SerializeField] private GameObject movementIndicatorPrefab;
    [SerializeField] private GameObject spellIndicatorPrefab;
    [SerializeField] private GameObject aaRangeIndicatorPrefab;

    private InputController _input;

    public bool normalCast;
    private bool _isAARangeIndicatorOn;
    private GameObject _aaRangeIndicator;
    private GameObject _movementIndicator;
    private GameObject _spellIndicator;

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
    private bool _showSpellIndicator;

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

    private void Update() {
        if (_showSpellIndicator && _spellIndicator != null) {
            Vector3 mousePos = GetMousePosition();
            
            var xDiff = mousePos.x - transform.position.x;
            var zDiff = mousePos.z - transform.position.z;
            Vector3 dir = new Vector3(xDiff, 0, zDiff).normalized;
            
            Quaternion rotation = Quaternion.LookRotation(dir, Vector3.up);
            _spellIndicator.transform.rotation = rotation;
            
            _spellIndicator.transform.position = new Vector3(
                transform.position.x,
                0.51f, 
                transform.position.z
            );
        }
    }

    private void FixedUpdate() {
        ProcessCommands();
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
        _showSpellIndicator = false;
        HideSpellIndicator();
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

        var mousePos = GetMousePosition();
            
        // Get vector from players hitbox to the mouse
        var diff = mousePos - _player.hitboxPos;
        
        var dist = Mathf.Sqrt(diff.x * diff.x / (_mainCamera.aspect * _mainCamera.aspect) + diff.z * diff.z);
        
        if (dist > worldRadius) {
            // Move click is valid (outside the players hitbox)
            lastClickPosition = new Vector3(mousePos.x, transform.position.y, mousePos.z);
            isAttackClick = false;
            return InputCommandType.Movement;
        }
        return InputCommandType.None;
    }

    private Vector3 GetMousePosition() {
        // Define a horizontal plane at the player's hitbox height
        Plane plane = new(Vector3.up, new Vector3(0, _player.hitboxPos.y, 0));
        
        // Get a ray from the camera to the mouse 
        var ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        // Detect move click
        return plane.Raycast(ray, out var enter) ? ray.GetPoint(enter) : new Vector3();
    }

    private void HandleMovementCommand() {
        ShowMovementIndicator(lastClickPosition);
        _player.StateManager.ChangeState(new TestMovingState(gameObject, false));
    }

    private void HandleAttackCommand() {
        _player.direction = (lastClickPosition - transform.position).normalized;
        if (!IsInAttackRange(lastClickPosition, GetStoppingDistance()))
            _player.StateManager.ChangeState(new TestMovingState(gameObject, true));
        else
            _player.StateManager.ChangeState(new AttackingState(gameObject));

        // Notify network about an attack command if needed
       // _networkStateManager.SendAttackCommand(lastClickPosition);
    }

    private void HandleSpellCommand(string key) {
        if (!_showSpellIndicator) {
            _showSpellIndicator = true;
            ShowSpellIndicator(Quaternion.identity); 
        
        } else {
            _showSpellIndicator = false;
            HideSpellIndicator();
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

    private void ShowSpellIndicator(Quaternion rotation) {
        _spellIndicator  = Instantiate(
            spellIndicatorPrefab,
            new Vector3(transform.position.x, 0.51f, transform.position.z),
            rotation
        );
    }

    private void HideSpellIndicator() {
        if (_spellIndicator != null) Destroy(_spellIndicator);
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
