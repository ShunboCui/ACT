using UnityEngine;

public class EnemyAttackState : EnemyBaseState
{
    public EnemyAttackState(EnemyStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        enemyControl.agent.isStopped = true;
    }

    public override void Tick(float deltaTime)
    {

        enemyControl.EnemyAttack();

        if (enemyControl.playerDistance > enemyControl.attackRange)
        {
            enemyStateMachine.SwitchState(enemyStateMachine.enemyMoveState);
        }
        else if (enemyControl.playerDistance > enemyControl.searchRange)
        {
            enemyStateMachine.SwitchState(enemyStateMachine.enemyIdleState);
        }
        else
        {
            enemyStateMachine.SwitchState(enemyStateMachine.enemyAttackState);
        }
    }

    public override void Exit()
    {
    }

}
