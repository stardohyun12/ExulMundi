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

    /// <summary>XZ 평면에서 플레이어 방향으로 이동합니다.</summary>
    protected virtual void MoveTowardPlayer()
    {
        Vector3 dir = (_player.position - transform.position);
        dir.y = 0f; // Y축 이동 제거 (XZ 평면 고정)
        transform.Translate(dir.normalized * moveSpeed * Time.deltaTime, Space.World);
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

    // 3D 물리 충돌 — 플레이어 접촉 시 피해 (비트리거 콜라이더)
    private void OnCollisionStay(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        collision.gameObject.GetComponent<PlayerHealth>()?.TakeDamage(contactDamage);
    }
}
