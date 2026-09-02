using UnityEngine;

public class PlayerDodgeState : PlayerBaseState
{
    private float dodgeDuration = 0.5f;
    private float timer;

    public PlayerDodgeState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        //Debug.Log("Entering player taking damage state");

        timer = 0f;

        this.playerControl.SetMoveSpeed(0f);
        this.playerControl.PlayAnimation("avoid_back");
        //this.playerControl.QuitAttackingState();

        this.playerControl.StartDodge();
    }

    public override void Tick(float deltaTime)
    {
        timer += deltaTime;

        if (timer >= dodgeDuration)
        {
            this.playerControl.EndDodge();
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
        this.playerControl.EndDodge();
    }
}
