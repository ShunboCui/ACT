using UnityEngine;

public class EnemyMoveState : EnemyBaseState
{
    public EnemyMoveState(EnemyStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        enemyControl.agent.isStopped = false;
        enemyControl.enemyAnimator.SetFloat("Speed", 1f);
        enemyControl.enemyAnimator.SetBool("Attacking", false);
    }

    public override void Tick(float deltaTime)
    {
        enemyControl.FaceToPlayer();
        enemyControl.agent.SetDestination(enemyControl.player.position);

        if (enemyControl.playerDistance > enemyControl.searchRange)
        {
            enemyStateMachine.SwitchState(enemyStateMachine.enemyIdleState);
        }
        else if (enemyControl.playerDistance <= enemyControl.attackRange)
        {
            enemyStateMachine.SwitchState(enemyStateMachine.enemyAttackState);
        }
    }

    public override void Exit()
    {
    }
}
