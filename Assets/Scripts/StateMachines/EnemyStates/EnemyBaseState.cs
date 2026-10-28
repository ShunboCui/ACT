using UnityEngine;

public abstract class EnemyBaseState : State
{
    protected EnemyControl enemyControl;
    protected EnemyStateMachine enemyStateMachine;

    public EnemyBaseState(EnemyStateMachine stateMachine)
    {
        this.enemyStateMachine = stateMachine;
        this.enemyControl = stateMachine.enemyControl;
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
