using UnityEngine;

public class CharacterHitDetect : MonoBehaviour
{
    public float destroyTime = 0.3f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("Damage detection box created");
        Destroy(this.gameObject, destroyTime);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
