using UnityEngine;
using System.Collections;

/// <summary>
/// 런타임 동료 인스턴스. 자동 기본 공격 + 플레이어 탭 스킬 발동.
/// </summary>
public class CompanionUnit : MonoBehaviour
{
    public CompanionData Data { get; private set; }

    public int CurrentHP { get; private set; }
    public int MaxHP => Data != null ? Data.maxHP : 0;
    public bool IsAlive => CurrentHP > 0;

    private float attackTimer;
    private float skillCooldownTimer;
    private bool isAttacking;
    private EnemyUnit target;

    public event System.Action<CompanionUnit> OnHPChanged;

    public void Initialize(CompanionData data)
    {
        Data = data;
        CurrentHP = data.maxHP;
        skillCooldownTimer = 0f;
    }

    public void ResetForBattle()
    {
        if (Data != null)
            CurrentHP = Data.maxHP;
        attackTimer = 0f;
        skillCooldownTimer = 0f;
        isAttacking = true;
    }

    public void SetTarget(EnemyUnit enemy)
    {
        target = enemy;
    }

    void Update()
    {
        if (!isAttacking || Data == null) return;

        // 자동 기본 공격
        attackTimer += Time.deltaTime;
        float attackInterval = Data.atkSpeed > 0 ? 1f / Data.atkSpeed : 1f;
        if (attackTimer >= attackInterval)
        {
            attackTimer = 0f;
            PerformBasicAttack();
        }

        // 스킬 쿨다운 감소
        if (skillCooldownTimer > 0)
            skillCooldownTimer -= Time.deltaTime;
    }

    private void PerformBasicAttack()
    {
        if (target == null || !target.IsAlive) return;
        target.TakeDamage(Data.atk);
        Debug.Log($"{Data.companionName} 기본 공격 → {target.Data.enemyName} ({Data.atk} 데미지)");
    }

    /// <summary>
    /// 플레이어가 카드 탭 시 호출
    /// </summary>
    public bool TryUseSkill()
    {
        if (skillCooldownTimer > 0 || target == null || !target.IsAlive) return false;

        target.TakeDamage(Data.skillDamage);
        skillCooldownTimer = Data.skillCooldown;
        Debug.Log($"{Data.companionName} 스킬 [{Data.skillName}] → {target.Data.enemyName} ({Data.skillDamage} 데미지)");
        return true;
    }

    public float GetSkillCooldownRatio()
    {
        if (Data == null || Data.skillCooldown <= 0) return 0f;
        return Mathf.Clamp01(skillCooldownTimer / Data.skillCooldown);
    }

    public void TakeDamage(int damage)
    {
        if (!IsAlive) return;
        CurrentHP = Mathf.Max(0, CurrentHP - damage);
        OnHPChanged?.Invoke(this);

        if (!IsAlive)
        {
            StopAttacking();
            BattleManager.Instance.OnCompanionDied(this);
        }
    }

    public void StopAttacking()
    {
        isAttacking = false;
    }

    /// <summary>
    /// HP 감소 (패널티 등)
    /// </summary>
    public void ReduceHP(int amount)
    {
        CurrentHP = Mathf.Max(1, CurrentHP - amount);
        OnHPChanged?.Invoke(this);
    }
}
