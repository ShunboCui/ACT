using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int MaxHP = 100;
    public int CurrentHP;

    public event Action<int, int> OnHealthChanged;
    [SerializeField] private PlayerHealthBar healthBar;

    void Start()
    {
        CurrentHP = MaxHP;
        healthBar.Bind(this);
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);
    }

    public void TakeDamage(int damage)
    {
        CurrentHP -= damage;
        CurrentHP = Mathf.Max(CurrentHP, 0);

        OnHealthChanged?.Invoke(CurrentHP, MaxHP);
    }
}