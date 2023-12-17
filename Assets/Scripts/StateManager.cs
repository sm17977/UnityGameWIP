
public class StateManager {
    private State currentState;

    public void ChangeState(State newState) {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    public void Update() {
        currentState?.Execute();
    }

    public string GetCurrentState(){
        return currentState.ToString();
    }
}