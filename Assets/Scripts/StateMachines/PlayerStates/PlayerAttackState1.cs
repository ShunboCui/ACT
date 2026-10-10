/*using UnityEngine;

public class PlayerAttackState : PlayerBaseState
{
    private float comboTimer;
    private bool canNextCombo;
    private bool attackFinished;
    public PlayerAttackState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        //Debug.Log("Entering player attack state");
        canNextCombo = false;
        attackFinished = false;

        comboTimer = 0f;

        PlayAttack(playerControl.ComboState);
    }

    public override void Tick(float deltaTime)
    {
        comboTimer += deltaTime;

        // 播放0.3秒后允许进入下一段
        if (comboTimer >= 0.3f)
        {
            canNextCombo = true;
        }

        // 按攻击进入下一段
        if (canNextCombo && playerControl.AttackPressed)
        {
            NextCombo();
            return;
        }

        if (attackFinished)
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
        //Debug.Log("Exiting player test state");
        //this.playerControl.QuitAttackingState();
    }

    private void PlayAttack(int combo)
    {
        switch (combo)
        {
            case 0:
                this.playerControl.PlayAnimation("combo_02_1");
                break;

            case 1:
                this.playerControl.PlayAnimation("combo_02_2");
                break;

            case 2:
                this.playerControl.PlayAnimation("combo_02_3");
                break;

            case 3:
                this.playerControl.PlayAnimation("combo_02_4");
                break;
        }
    }

    private void NextCombo()
    {
        playerControl.ComboState++;
        playerStateMachine.SwitchState(playerStateMachine.playerAttackState);
    }

    public void OnAttackFinished()
    {
        this.attackFinished = true;
    }
}
*/