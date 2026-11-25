using UnityEngine;

public class PlayerAttackHitbox : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        EnemyControl enemy = other.GetComponent<EnemyControl>();

        if (enemy == null) return;

        int damage = WeaponController.Instance.CalculateDamage();

        if (other.gameObject.tag == "Enemy")
        {
            enemy.TakeDamage(damage);
        }
    }
}
