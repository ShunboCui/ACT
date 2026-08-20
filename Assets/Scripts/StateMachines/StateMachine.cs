using UnityEngine;

public abstract class StateMachine
{
    private State currentState;

    public void SwitchState(State newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    public void Update()
    {
        currentState?.Tick(Time.deltaTime);
    }
}
