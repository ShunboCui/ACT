using UnityEngine;

public class PlayerIdleState : PlayerBaseState
{
    public PlayerIdleState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        //Debug.Log("Entering player test state");
        playerControl.PlayerAnimator.SetFloat("RunAnimSpeed", 0f);
    }

    public override void Tick(float deltaTime)
    {
        if (playerControl.MoveInput.magnitude > 0.1f)
        {
            playerStateMachine.SwitchState(playerStateMachine.playerMoveState);
            return;
        }

        if (playerControl.AttackPressed)
        {
            playerStateMachine.SwitchState(playerStateMachine.playerAttackState);
            return;
        }
    }

    public override void Exit()
    {
        //Debug.Log("Exiting player test state");
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
