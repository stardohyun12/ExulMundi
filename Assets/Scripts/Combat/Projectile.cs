using UnityEngine;

/// <summary>플레이어가 발사하는 투사체. GunWeapon이 생성합니다.</summary>
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private int   damage   = 1;
    [SerializeField] private float lifetime = 5f;

    /// <summary>GunWeapon이 카드 기본 공격력을 주입할 때 사용합니다.</summary>
    public int Damage { get => damage; set => damage = value; }

    public bool         IsPiercing      { get; set; }
    public bool         IsExplosive     { get; set; }
    public float        ExplosionRadius { get; set; }
    public float        LifeStealRate   { get; set; }
    public PlayerHealth ShooterHealth   { get; set; }

    private void Start() => Destroy(gameObject, lifetime);

    // 3D Trigger 충돌 — 적에게 피해
    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<EnemyBase>(out var enemy)) return;

        DealDamage(enemy);

        if (IsExplosive)
        {
            // 3D 구형 범위 내 추가 적 피해
            var hits = Physics.OverlapSphere(transform.position, ExplosionRadius);
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<EnemyBase>(out var e) && e != enemy)
                    DealDamage(e);
            }
        }

        if (!IsPiercing) Destroy(gameObject);
    }

    private void DealDamage(EnemyBase enemy)
    {
        enemy.TakeDamage(damage);
        if (LifeStealRate > 0f && ShooterHealth != null)
            ShooterHealth.Heal(Mathf.Max(1, Mathf.RoundToInt(damage * LifeStealRate)));
    }
}
