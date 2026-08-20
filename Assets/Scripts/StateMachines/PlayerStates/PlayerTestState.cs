using UnityEngine;

public class PlayerTestState : PlayerBaseState
{
    public PlayerTestState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        //Debug.Log("Entering player test state");
    }
    public override void Tick(float deltaTime)
    {
        //Debug.Log("Tick player test state");
        if (Input.GetKeyDown(KeyCode.Space))
        {
            playerStateMachine.SwitchState(playerStateMachine.playerTestState2);
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
