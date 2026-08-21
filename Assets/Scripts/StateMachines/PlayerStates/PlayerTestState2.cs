using UnityEngine;

public class PlayerTestState2 : PlayerBaseState
{
    public PlayerTestState2(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        //Debug.Log("Entering player test state 2");
    }
    public override void Tick(float deltaTime)
    {
        //Debug.Log("Tick player test state 2");
        if (Input.GetKeyDown(KeyCode.Space))
        {
            playerStateMachine.SwitchState(playerStateMachine.playerTestState);
        }
    }
    public override void Exit()
    {
        //Debug.Log("Exiting player test state 2");
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
