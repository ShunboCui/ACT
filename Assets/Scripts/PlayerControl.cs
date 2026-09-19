using System;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerControl : MonoBehaviour
{
    private InputSystem_Actions inputSystemActions;
    public Vector2 MoveInput { get; private set; }
    //private float InputH;
    //private float InputV;
    //private Vector3 Direction;
    public float Velocity;
    private CharacterController PlayerController;
    private float TargetDirection;
    public Camera PlayerCamera;

    //Calculate rotation
    private float RotateAngle;
    private float RotateSmooth;
    private float RotateTime;

    public float LastAttackTime;
    public int ComboState = 0;
    //private string[] ComboNameList = { "combo_02_1", "combo_02_2", "combo_02_3", "combo_02_4" };

    //If attacking
    public bool AttackPressed { get; private set; } = false;
    public float AttackDurationTime { get; private set; } = 0.5f;
    public float ComboResetTime { get; private set; } = 1.5f;
    private float RunAnimSpeed;
    //public float maxHP = 100.0f;
    //private float currentHP;
    public bool isDead = false;
    private bool deadPlayed = false;

    private Vector3 NewMoveDirection;

    public Animator PlayerAnimator { get; private set;}
    public GameObject WeaponEffect;
    //Audio management component    
    public AudioSource AudioSource;
    //Attack sound
    public float AttackVolume = .8f;
    public AudioClip WeaponAttackSound;
    public AudioClip[] CharaAttackSounds;
    private bool OnGround = true;
    private bool dodge = false;
    public GameObject DealDamageArea;
    private Transform DamagePosition;
    public GameObject TakeDamageEffect;
    private Transform TakeDamageEffectPosition;
    private GameObject[] enemyList;
    public float playerDamage;
    //private bool underAttack = false;
    //private Slider playerHealthBar;
    private Text playerCurrentHealth;
    private Text playerMaxHealth;
    private PlayerHealth playerHealth;

    //public GameObject deadUI;
    private PlayerStateMachine playerStateMachine;
    public WeaponController weaponController;
    public int initialWeaponId = 2;

    private void Awake()
    {
        PlayerAnimator = this.gameObject.GetComponent<Animator>();
        playerStateMachine = new PlayerStateMachine(this);
        inputSystemActions = new InputSystem_Actions();
        WeaponEffect.SetActive(false);
        DamagePosition = this.transform.Find("CharacterDamagePosition");
        TakeDamageEffectPosition = this.transform.Find("CharacterGetHitEffectPosition");
        //playerHealthBar = this.transform.Find("Canvas/Slider").gameObject.GetComponent<Slider>();
        //playerCurrentHealth = this.transform.Find("Canvas/CurrentHP").gameObject.GetComponent<Text>();
        //playerMaxHealth = this.transform.Find("Canvas/MaxHP").gameObject.GetComponent<Text>();
        playerHealth = GetComponent<PlayerHealth>();
        LastAttackTime = Time.time;
    }

    private void OnEnable()
    {
        this.inputSystemActions.Player.Enable();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Get game controller component
        this.PlayerController = this.gameObject.GetComponent<CharacterController>();
        this.Velocity = 10f;
        //PlayerCamera = Camera.main;
        this.RotateTime = 0.1f;

        //Instantiate(DealDamageArea, DamagePosition.position, DamagePosition.rotation);
        this.DealDamageArea.SetActive(false);
        enemyList = GameManager.Instance.enemyList;
        //LockMouse();

        //WeaponController.Instance.EquipWeapon(Freedom_Sworn_Data);
        PackageTableItem packageTableItem = GameManager.Instance.GetPackageTableItemById(initialWeaponId);
        WeaponController.Instance.EquipWeapon(packageTableItem);
    }

    private void OnDisable()
    {
        inputSystemActions.Player.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        MoveInput = inputSystemActions.Player.Move.ReadValue<Vector2>();


        DoAttack();
            //Jump();

        this.playerStateMachine.Update();
        Dodge();
        Dead();
        //MouseControl();
        //PlayerUIControl();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isDead && !dodge && other.gameObject.tag == "EnemyAttackBox")
        {
            playerHealth.TakeDamage(10);
            Debug.Log("Player health " + playerHealth.CurrentHP);
            this.playerStateMachine.SwitchState(this.playerStateMachine.playerTakeDamageState);
            Instantiate(TakeDamageEffect, TakeDamageEffectPosition.position, TakeDamageEffectPosition.rotation);

            //underAttack = true;
            //this.EndAttackingAction();
        }
    }

    public void Move(Vector3 direction)
    {

        //PlayerController.Move(Direction * velocity * Time.deltaTime);

        //if (direction.magnitude >= 0.1f && !AttackingAction)
        //{
            //Animation
            //PlayerAnimator.SetBool("Run", true);
            PlayerAnimator.SetFloat("RunAnimSpeed", direction.magnitude, 0.1f, Time.deltaTime);

            //Calculate target angle
            TargetDirection = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + PlayerCamera.transform.eulerAngles.y;
            RotateAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, TargetDirection, ref RotateSmooth, RotateTime);

            transform.rotation = Quaternion.Euler(0, RotateAngle, 0);

            NewMoveDirection = Quaternion.Euler(0, TargetDirection, 0) * Vector3.forward;

            PlayerController.Move(NewMoveDirection * Velocity * Time.deltaTime);

        //}
        //else
        //{
            //PlayerAnimator.SetBool("Run", false);
        //    PlayerAnimator.SetFloat("RunAnimSpeed", 0f, 0.1f, Time.deltaTime);
        //}
    }

    public void SetMoveSpeed(float speed)
    {
        this.PlayerAnimator.SetFloat("RunAnimSpeed", speed);
    }

    private void DoAttack()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            AttackPressed = true;
            PlayerViewAngle();
            WeaponEffect.SetActive(true);
            /*
            if (ComboState < 3)
            {
                PlayerAnimator.CrossFade(ComboNameList[ComboState], 0.2f);
                ComboState += 1;
            }
            else
            {
                PlayerAnimator.CrossFade("combo_02_4", 0.2f);
                ComboState = 0;
            }*/
            /*
            switch (ComboState)
            {
                case 0:
                    ComboState += 1;
                    PlayerAnimator.CrossFade("combo_02_1", 0.1f);
                    break;
                case 1:
                    ComboState += 1;
                    PlayerAnimator.CrossFade("combo_02_2", 0.1f);
                    break;
                case 2:
                    ComboState += 1;
                    PlayerAnimator.CrossFade("combo_02_3", 0.1f);
                    break;
                case 3:
                    ComboState = 0;
                    PlayerAnimator.CrossFade("combo_02_4", 0.1f);
                    break;
            }*/
            /*
            CancelInvoke("QuitAttacking");
            Invoke("EndAttackingAction", AttackDurationTime);

            Invoke("QuitAttacking", AttackQuitTime);*/
        }
        else
        {
            if (this.AttackPressed)
            {
                this.AttackPressed = false;
            }
        }
    }

    public void PlayAnimation(string animationName, float transitionTime = 0f)
    {
        this.PlayerAnimator.CrossFade(animationName, transitionTime);
    }

    private void DealDamage()
    {
        DealDamageArea.SetActive(true);
    }

    private void DisableDamageArea()
    {
        DealDamageArea.SetActive(false);
    }

    public void AttackAnimFinished()
    {
        this.playerStateMachine.playerAttackState.OnAttackFinished();
    }

    public void QuitAttackingState()
    {
        DisableDamageArea();
        WeaponEffect.SetActive(false);
        ComboState = 0;
    }

    private void PlayAttackSound(AnimationEvent attackSoundEvent)
    {
        AudioSource.PlayOneShot(WeaponAttackSound, AttackVolume);
        AudioSource.PlayOneShot(CharaAttackSounds[attackSoundEvent.intParameter], AttackVolume);
    }

    /*
    private void Jump()
    {
        float jumping = inputSystemActions.Player.Jump.ReadValue<float>();
        if (OnGround && jumping > 0.1f && !AttackingAction)
        {
            PlayerAnimator.SetBool("IsGrounded", false);
            PlayerAnimator.CrossFade("jump", 0.1f);
            OnGround = false;
            Invoke("JumpEnd", 1.0f);
        }
    }*/

    private void Dodge()
    {
        //float jumping = inputSystemActions.Player.Jump.ReadValue<float>();
        if (inputSystemActions.Player.Jump.WasPressedThisFrame() && !dodge)
        {
            this.playerStateMachine.SwitchState(this.playerStateMachine.playerDodgeState);
            //PlayerAnimator.CrossFade("avoid_back", 0.1f);
            //this.StartDodge();
            //EndAttackingAction();
            //QuitAttackingState();
            //EndTakingDamage();
            Invoke("EndDodge", 0.5f);
        }
    }

    public void StartDodge()
    {
        dodge = true;
    }

    public void EndDodge()
    {
        if (dodge)
        {
            dodge = false;
        }
    }

    private void JumpEnd()
    {
        OnGround = true;
        PlayerAnimator.SetBool("IsGrounded", true);
    }

    /*
    private void TakeDamage()
    {
        currentHP -= 10;
        Instantiate(TakeDamageEffect, TakeDamageEffectPosition.position, TakeDamageEffectPosition.rotation);

        underAttack = true;
        this.PlayerAnimator.CrossFade("get_hit", 0.1f);
        this.PlayerAnimator.SetFloat("Speed", 0);
        Invoke("EndTakingDamage", 0.5f);
    }*/

    /*
    private void EndTakingDamage()
    {
        underAttack = false;
    }*/

    private void PlayerViewAngle()
    {
        enemyList = GameManager.Instance.enemyList;
        if (enemyList == null || enemyList.Count() == 0)
        {
            return;
        }
        Vector3 currentPosition = this.transform.position;
        Vector3 enemyPosition;
        float minDistance = 10000f;
        float enemyDistance;
        foreach (var enemy in enemyList)
        {
            if (enemy.GetComponent<EnemyControl>().IsDead == false)
            {
                enemyDistance = Vector3.Distance(currentPosition, enemy.transform.position);
                if (enemyDistance < minDistance)
                {
                    minDistance = enemyDistance;
                    enemyPosition = enemy.transform.position;
                    enemyPosition.y = currentPosition.y;
                    this.transform.LookAt(enemyPosition);
                }
            }
        }
    }

    private void Dead()
    {
        if (playerHealth.CurrentHP <= 0 && !isDead)
        {
            isDead = true;
            PlayerAnimator.CrossFade("DieA", 0.1f);
            Invoke(nameof(OnDeath), 2.0f);
        }
    }

    private void OnDeath()
    {
        //deadUI.SetActive(true);
        //backgroundUI.SetActive(true);

        //UnlockMouse();
        //UIManager.Instance.OpenPanel("Background");
        //UIManager.Instance.OpenPanel("DeadMenu");

        //Time.timeScale = 0f;
        GameManager.Instance.GameOver();
    }

    /*
    private void PlayerUIControl()
    {
        playerHealthBar.value = currentHP / maxHP;
        playerCurrentHealth.text = currentHP.ToString();
        playerMaxHealth.text = " / " + maxHP.ToString();
    }*/
}