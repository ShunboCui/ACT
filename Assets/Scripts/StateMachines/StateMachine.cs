using UnityEngine;

public abstract class StateMachine
{
    protected State currentState;

    public virtual void SwitchState(State newState)
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
