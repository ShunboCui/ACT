using UnityEngine;

public abstract class PlayerBaseState : State
{
    protected PlayerControl playerControl;
    protected PlayerStateMachine playerStateMachine;

    public PlayerBaseState(PlayerStateMachine stateMachine)
    {
        this.playerStateMachine = stateMachine;
        this.playerControl = stateMachine.playerControl;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
