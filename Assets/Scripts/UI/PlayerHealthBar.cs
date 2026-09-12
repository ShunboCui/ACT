using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBar : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private Text currentHP;
    [SerializeField] private Text maxHP;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Bind(PlayerHealth health)
    {
        health.OnHealthChanged += UpdateHealth;
    }

    public void UpdateHealth(int current, int max)
    {
        slider.value = (float)current / max;
        currentHP.text = current.ToString();
        maxHP.text = "/" + max.ToString();
    }
}
