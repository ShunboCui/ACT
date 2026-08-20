using UnityEngine;

public class PlayerTakeDamageState : PlayerBaseState
{
    private float hitDuration = 0.5f;
    private float timer;

    public PlayerTakeDamageState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        //Debug.Log("Entering player taking damage state");

        timer = 0f;

        this.playerControl.SetMoveSpeed(0f);
        this.playerControl.PlayAnimation("get_hit");
        this.playerControl.EndAttackingAction();
    }

    public override void Tick(float deltaTime)
    {
        timer += deltaTime;

        if (timer >= hitDuration)
        {
            if (this.playerControl.MoveInput.magnitude > 0)
            {
                this.playerStateMachine.SwitchState(this.playerStateMachine.playerMoveState);
            }
            else
            {
                this.playerStateMachine.SwitchState(this.playerStateMachine.playerIdleState);
            }
        }
    }

    public override void Exit()
    {
        //Debug.Log("Exiting player take damage state");
    }
}
