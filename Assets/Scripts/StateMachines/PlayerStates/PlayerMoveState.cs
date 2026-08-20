using UnityEngine;
using UnityEngine.Windows;

public class PlayerMoveState : PlayerBaseState
{
    private float InputH;
    private float InputV;
    private Vector3 Direction;

    public PlayerMoveState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        //Debug.Log("Entering player test state");
        playerControl.PlayerAnimator.SetFloat("RunAnimSpeed", 1f);
    }

    public override void Tick(float deltaTime)
    {
        Vector2 input = playerControl.MoveInput;
        InputH = input.x;
        InputV = input.y;

        Vector3 direction = new Vector3(input.x, 0, input.y).normalized;

        if (direction.magnitude < 0.1f)
        {
            playerStateMachine.SwitchState(playerStateMachine.playerIdleState);
            return;
        }

        playerControl.Move(direction);
        if (this.playerControl.AttackPressed)
        {
            this.playerStateMachine.SwitchState(playerStateMachine.playerAttackState);
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
