using UnityEngine;
using System;
using System.Collections;

/// <summary>플레이어 HP 관리. 피해·회복·무적 처리.</summary>
public class PlayerHealth : MonoBehaviour
{
    public static event Action OnPlayerDied;

    [Header("설정")]
    [SerializeField] private int   maxHP             = 5;
    [SerializeField] private float invincibleDuration = 1f;

    public int  MaxHP      => maxHP;
    public int  CurrentHP  { get; private set; }
    public bool IsInvincible { get; private set; }

    /// <summary>(currentHP, maxHP)</summary>
    public event Action<int, int> OnHealthChanged;

    private void Awake() => CurrentHP = maxHP;

    /// <summary>플레이어에게 피해를 줍니다.</summary>
    public void TakeDamage(int amount)
    {
        if (IsInvincible || amount <= 0) return;

        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        OnHealthChanged?.Invoke(CurrentHP, maxHP);

        if (CurrentHP <= 0)
            OnPlayerDied?.Invoke();
        else
            StartCoroutine(InvincibleRoutine());
    }

    /// <summary>플레이어를 회복시킵니다.</summary>
    public void Heal(int amount)
    {
        if (amount <= 0) return;
        CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
    }

    private IEnumerator InvincibleRoutine()
    {
        IsInvincible = true;
        yield return new WaitForSeconds(invincibleDuration);
        IsInvincible = false;
    }
}
