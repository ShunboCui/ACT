using UnityEngine;

public class EnemyStateMachine : StateMachine
{
    public EnemyControl enemyControl { get; }
    public EnemyIdleState enemyIdleState;
    public EnemyMoveState enemyMoveState;
    public EnemyAttackState enemyAttackState;
    public EnemyTakeDamageState enemyTakeDamageState;
    private bool quittingAttack;
    public EnemyStateMachine(EnemyControl enemyControl)
    {
        this.enemyControl = enemyControl;

        enemyIdleState = new EnemyIdleState(this);
        enemyMoveState = new EnemyMoveState(this);
        enemyAttackState = new EnemyAttackState(this);
        enemyTakeDamageState = new EnemyTakeDamageState(this);
        //playerAttackState = new PlayerAttackState(this);

        SwitchState(enemyIdleState);
    }

    public override void SwitchState(State newState)
    {

        base.SwitchState(newState);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //SwitchState(playerIdleState);
    }

}
