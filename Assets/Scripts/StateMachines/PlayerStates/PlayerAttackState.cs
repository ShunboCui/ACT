using UnityEngine;

public class PlayerAttackState : PlayerBaseState
{
    private ComboManager comboManager;
    public PlayerAttackState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
        this.comboManager = playerControl.ComboManager;
    }

    public override void Enter()
    {
        Debug.Log("Entering player attack state");
        comboManager.StartCombo();
    }

    public override void Tick(float deltaTime)
    {
        comboManager.UpdateCombo();

        // Press attack to enter next combo segment
        if (playerControl.AttackPressed)
        {
            comboManager.NextSegment();
        }

        if (!comboManager.isPlaying)
        {
            //playerControl.ComboState = 0;
            if (playerControl.MoveInput.magnitude > 0.1f)
            {
                playerStateMachine.SwitchState(playerStateMachine.playerMoveState);
                return;
            }
            else
            {
                playerStateMachine.SwitchState(playerStateMachine.playerIdleState);
                return;
            }
        }
    }

    public override void Exit()
    {
        comboManager.EndCombo();
    }
}
