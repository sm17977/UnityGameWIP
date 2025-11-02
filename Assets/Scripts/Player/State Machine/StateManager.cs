using UnityEngine;

public class StateManager {

    private State _currentState;
    
    /// <summary>
    /// Change player state
    /// </summary>
    /// <param name="newState">The state to transition to</param>
    public void ChangeState(State newState) {
        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter();
    }

    /// <summary>
    /// Executes the current state
    /// </summary>
    public void Update() {
        _currentState?.Execute();
    }

    /// <summary>
    /// Gets the current state
    /// </summary>
    /// <returns>The current state</returns>
    public string GetCurrentState(){
        return _currentState.ToString();
    }
}