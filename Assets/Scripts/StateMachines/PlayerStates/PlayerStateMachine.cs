using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class PlayerStateMachine : StateMachine
{
    public PlayerControl playerControl { get; }
    public PlayerTestState playerTestState;
    public PlayerTestState2 playerTestState2;
    public PlayerIdleState playerIdleState;
    public PlayerMoveState playerMoveState;
    public PlayerAttackState playerAttackState;
    public PlayerTakeDamageState playerTakeDamageState;
    public PlayerDodgeState playerDodgeState;
    public PlayerStateMachine(PlayerControl playerControl)
    {
        this.playerControl = playerControl;

        playerIdleState = new PlayerIdleState(this);
        playerMoveState = new PlayerMoveState(this);
        playerAttackState = new PlayerAttackState(this);
        playerTakeDamageState = new PlayerTakeDamageState(this);
        playerDodgeState = new PlayerDodgeState(this);
        //playerAttackState = new PlayerAttackState(this);

        SwitchState(playerIdleState);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //SwitchState(playerIdleState);
    }

}
