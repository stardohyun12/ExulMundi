using UnityEngine;

/// <summary>
/// 기본 근접 적. EnemyBase를 상속하며 플레이어에게 접근 후 접촉 데미지를 입힙니다.
/// SpriteRenderer + Collider2D(Trigger) + 이 컴포넌트를 프리팹에 붙여 사용합니다.
/// </summary>
public class MeleeEnemy : EnemyBase
{
    // EnemyBase의 MoveTowardPlayer / OnTriggerStay2D / TakeDamage / Die를 그대로 사용합니다.
    // 추후 특수 행동(돌진, 점프 등)이 필요하면 여기서 오버라이드합니다.
}
