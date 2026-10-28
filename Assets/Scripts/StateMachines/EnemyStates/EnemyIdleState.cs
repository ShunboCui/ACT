using UnityEngine;

public class EnemyIdleState : EnemyBaseState
{
    public EnemyIdleState(EnemyStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        enemyControl.agent.isStopped = true;
        enemyControl.enemyAnimator.Play("MoveTree");
        enemyControl.enemyAnimator.SetFloat("Speed", 0f);
        enemyControl.enemyAnimator.SetBool("Attacking", false);
    }
    public override void Tick(float deltaTime)
    {

        if (enemyControl.playerDistance <= enemyControl.searchRange && enemyControl.playerDistance > enemyControl.attackRange)
        {
            enemyStateMachine.SwitchState(enemyStateMachine.enemyMoveState);
            return;
        }
    }

    public override void Exit()
    {
    }

}
