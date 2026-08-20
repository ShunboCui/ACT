using UnityEngine;

public class EnemyHitDetect : MonoBehaviour
{
    public float destroyTime = 0.3f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("Enemy damage detection box created");
        Destroy(this.gameObject, destroyTime);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
