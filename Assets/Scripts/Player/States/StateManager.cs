public sealed class StateManager {
    
    private static StateManager _instance = null;
    private static readonly object Padlock = new object();
    private State _currentState;
    
    public static StateManager Instance {
        get {
            lock (Padlock) {
                _instance ??= new StateManager();
                return _instance;
            }
        }
    }

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