using UnityEngine;

public class EnemyAttackHitbox : MonoBehaviour
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
        PlayerControl player = other.GetComponent<PlayerControl>();

        if (player == null) return;

        int damage = 10;

        if (other.gameObject.tag == "Player")
        {
            player.TakeDamage(damage);
        }
    }
}
