using UnityEngine;
using System;

/// <summary>
/// 모든 적의 추상 기반 클래스.
/// 플레이어를 향해 이동하고 접촉 피해를 줍니다.
/// </summary>
public class EnemyBase : MonoBehaviour
{
    [Header("스탯")]
    [SerializeField] protected int   maxHP          = 3;
    [SerializeField] protected float moveSpeed      = 2f;
    [SerializeField] protected int   contactDamage  = 1;
    [SerializeField] protected float attackCooldown = 1f;

    protected int       _currentHP;
    protected Transform _player;
    protected float     _attackTimer;

    public event Action<EnemyBase> OnDied;

    protected virtual void Awake() => _currentHP = maxHP;

    protected virtual void Start()
    {
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;
    }

    protected virtual void Update()
    {
        if (_player == null) return;

        MoveTowardPlayer();

        _attackTimer += Time.deltaTime;
        if (_attackTimer >= attackCooldown)
        {
            _attackTimer = 0f;
            TryAttack();
        }
    }

    protected virtual void MoveTowardPlayer()
    {
        Vector2 dir = (_player.position - transform.position).normalized;
        transform.Translate(dir * moveSpeed * Time.deltaTime);
    }

    protected virtual void TryAttack() { }

    /// <summary>이 적에게 피해를 입힙니다.</summary>
    public virtual void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        _currentHP -= amount;
        if (_currentHP <= 0) Die();
    }

    /// <summary>EncounterManager가 난이도를 적용할 때 호출합니다.</summary>
    public void ApplyDifficultyScale(float mult)
    {
        maxHP      = Mathf.RoundToInt(maxHP * mult);
        _currentHP = maxHP;
        moveSpeed  *= Mathf.Sqrt(mult);
    }

    protected virtual void Die()
    {
        OnDied?.Invoke(this);
        Destroy(gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        other.GetComponent<PlayerHealth>()?.TakeDamage(contactDamage);
    }
}
