using UnityEngine;

/// <summary>
/// 런타임 적 인스턴스. 자동 공격 로직.
/// </summary>
public class EnemyUnit : MonoBehaviour
{
    public EnemyData Data { get; private set; }

    public int CurrentHP { get; private set; }
    public int MaxHP => Data != null ? Data.maxHP : 0;
    public bool IsAlive => CurrentHP > 0;

    private float attackTimer;
    private bool isAttacking;
    private CompanionUnit target;

    public event System.Action<EnemyUnit> OnHPChanged;

    public void Initialize(EnemyData data)
    {
        Data = data;
        CurrentHP = data.maxHP;
        isAttacking = true;
    }

    public void SetTarget(CompanionUnit companion)
    {
        target = companion;
    }

    void Update()
    {
        if (!isAttacking || Data == null) return;

        attackTimer += Time.deltaTime;
        float attackInterval = Data.atkSpeed > 0 ? 1f / Data.atkSpeed : 1f;
        if (attackTimer >= attackInterval)
        {
            attackTimer = 0f;
            PerformAttack();
        }
    }

    private void PerformAttack()
    {
        if (target == null || !target.IsAlive) return;
        target.TakeDamage(Data.atk);
        Debug.Log($"{Data.enemyName} 공격 → {target.Data.companionName} ({Data.atk} 데미지)");
    }

    public void TakeDamage(int damage)
    {
        if (!IsAlive) return;
        CurrentHP = Mathf.Max(0, CurrentHP - damage);
        OnHPChanged?.Invoke(this);

        if (!IsAlive)
        {
            StopAttacking();
            BattleManager.Instance.OnEnemyDied(this);
        }
    }

    public void StopAttacking()
    {
        isAttacking = false;
    }
}
