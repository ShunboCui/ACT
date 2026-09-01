using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public enum EnemyState
{
    idle,
    attack,
    move
}

public class EnemyControl : MonoBehaviour
{
    private EnemyState currentState = EnemyState.idle;
    private Animator enemyAnimator;
    private NavMeshAgent agent;
    private float currentHP = 100.0f;
    private bool isDead = false;
    private float playerDistance;

    public Transform player;
    public AudioSource AudioSource;
    //Attack sound
    public float AttackVolume = .8f;
    public AudioClip EnemyAttackSound;
    public float searchRange = 50.0f;
    public float attackRange = 3.0f;
    public float maxHP = 100.0f;
    public GameObject DealDamageArea;
    private Transform DealDamagePosition;
    public GameObject TakeDamageEffect;
    private Transform TakeDamageEffectPosition;
    private Slider enemyHealthBar;
    private float playerDamage;
    public bool IsDead
    {
        get { return isDead; }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.currentHP = maxHP;
        this.enemyAnimator = this.GetComponent<Animator>();
        this.agent = this.GetComponent<NavMeshAgent>();
        this.playerDamage = GameObject.FindWithTag("Player").GetComponent<PlayerControl>().playerDamage;
        this.player = GameObject.FindWithTag("Player").transform;
        this.TakeDamageEffectPosition = this.transform.Find("EnemyGetHitEffectPosition");
        this.DealDamagePosition = this.transform.Find("EnemyDamagePosition");
        this.enemyHealthBar = this.transform.Find("EnemyCanvas/Slider").gameObject.GetComponent<Slider>();

        this.DealDamageArea.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        EnemyStateController();
        Dead();
        UIControl();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isDead && other.gameObject.tag == "CharacterAttackBox")
        {
            TakeDamage();
            Debug.Log("Current health " + currentHP);
        }
    }

    private void FaceToPlayer()
    {
        if (!isDead)
        {
            Vector3 PlayerPos = player.position;
            PlayerPos.y = this.transform.position.y;
            this.transform.LookAt(PlayerPos);
        }
    }

    private void EnemyStateController()
    {
        if (!isDead)
        {
            playerDistance = Vector3.Distance(this.transform.position, this.player.position);
            switch (currentState)
            {
                case EnemyState.idle:
                    this.enemyAnimator.Play("MoveTree");
                    this.enemyAnimator.SetFloat("Speed", 0f);
                    this.enemyAnimator.SetBool("Attacking", false);
                    this.agent.isStopped = true;

                    if (playerDistance <= searchRange && playerDistance > attackRange)
                    {
                        currentState = EnemyState.move;
                    }
                    break;

                case EnemyState.move:
                    FaceToPlayer();
                    this.agent.isStopped = false;
                    this.agent.SetDestination(this.player.position);
                    this.enemyAnimator.SetFloat("Speed", 1f);
                    this.enemyAnimator.SetBool("Attacking", false);

                    if (playerDistance > searchRange)
                    {
                        currentState = EnemyState.idle;
                    }
                    else if (playerDistance <= attackRange)
                    {
                        currentState = EnemyState.attack;
                    }
                    break;

                case EnemyState.attack:
                    this.agent.isStopped = true;
                    EnemyAttack();

                    if (playerDistance > attackRange)
                    {
                        currentState = EnemyState.move;
                    }
                    else if (playerDistance > searchRange)
                    {
                        currentState = EnemyState.idle;
                    }
                    else
                    {
                        currentState = EnemyState.attack;
                    }
                    break;
            }
        }
    }

    private void EnemyAttack()
    {
        if (enemyAnimator.GetBool("Attacking"))
        {
            return;
        }
        else
        {
            enemyAnimator.SetBool("Attacking", true);
        }
    }

    private void PlayAttackSound(AnimationEvent attackSoundEvent)
    {
        if (Random.Range(1,5) == 1)
        {
            AudioSource.PlayOneShot(EnemyAttackSound, AttackVolume);
        }
    }

    private void DealDamage()
    {
        this.DealDamageArea.SetActive(true);
        this.enemyAnimator.SetBool("Attacking", true);
    }

    private void EndAttackingAction()
    {
        this.DealDamageArea.SetActive(false);
        this.enemyAnimator.SetBool("Attacking", false);
    }

    private void TakeDamage()
    {
        currentHP -= playerDamage;
        StartUnderAttackAnim();
        this.enemyAnimator.CrossFade("get_hit", 0.1f);
        Instantiate(TakeDamageEffect, TakeDamageEffectPosition.position, TakeDamageEffectPosition.rotation);
    }

    private void StartUnderAttackAnim()
    {
        enemyAnimator.SetBool("TakingDamage", true);
    }
    private void EndUnderAttackAnim()
    {
        enemyAnimator.SetBool("TakingDamage", false);
    }

    private void Dead()
    {
        if (currentHP <= 0 && !isDead)
        {
            isDead = true;
            enemyAnimator.CrossFade("dead_02", 0.1f);
            Destroy(this.gameObject, 3f);
        }
    }

    private void UIControl()
    {
        enemyHealthBar.value = currentHP / maxHP;
    }
}
