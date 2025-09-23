using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class InputController : MonoBehaviour {
    public event Action<InputCommand> OnInputCommandIssued;
    
    private Controls _controls;
    
    private void OnEnable() {
        _controls = new Controls();
        _controls.Player.Enable();
        _controls.Player.RightClick.performed += OnRightClick;
        _controls.Player.Q.performed += OnQ;
        _controls.Player.E.performed += OnE;
        _controls.Player.R.performed += OnR;
        _controls.Player.A.performed += OnA;
    }

    private void OnDisable() {
        _controls.Player.RightClick.performed -= OnRightClick;
        _controls.Player.Q.performed -= OnQ;
        _controls.Player.E.performed -= OnE;
        _controls.Player.R.performed -= OnR;
        _controls.Player.A.performed -= OnA;
        _controls.Player.Disable();
    }

    // Input Events
    private void OnRightClick(InputAction.CallbackContext context) {
        if (GlobalState.Paused) return;
        OnInputCommandIssued?.Invoke(new InputCommand { Type = InputCommandType.RightClick });
    }

    private void OnQ(InputAction.CallbackContext context) {
        OnInputCommandIssued?.Invoke(new InputCommand { Type = InputCommandType.CastSpell, Key = "Q" });
    }

    private void OnE(InputAction.CallbackContext context) {
        OnInputCommandIssued?.Invoke(new InputCommand { Type = InputCommandType.CastSpell, Key = "E" });
    }
    
    private void OnR(InputAction.CallbackContext context) {
        OnInputCommandIssued?.Invoke(new InputCommand { Type = InputCommandType.CastSpell, Key = "R" });
    }

    private void OnA(InputAction.CallbackContext context) {
        OnInputCommandIssued?.Invoke(new InputCommand { Type = InputCommandType.ToggleAARange });
    }
}