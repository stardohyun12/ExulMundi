using UnityEngine;

/// <summary>
/// 카드 효과를 HeroUnit / EnemyUnit에 적용하는 정적 헬퍼.
/// CardClickHandler에서 호출.
/// </summary>
public static class CardEffectApplier
{
    public static void Apply(CardData card, HeroUnit hero, EnemyUnit enemy)
    {
        if (card == null || hero == null)
        {
            Debug.LogWarning($"[CardEffectApplier] card 또는 hero가 null입니다!");
            return;
        }

        Debug.Log($"[CardEffectApplier] 카드 효과 적용: {card.cardName}, 효과타입: {card.effectType}, effectValue: {card.effectValue}");

        // 약점 노출 적용 시 이번 카드 효과 2배
        float mult = hero.ConsumeWeaknessExposed() ? 2f : 1f;
        float val  = card.effectValue  * mult;
        float val2 = card.effectValue2;

        switch (card.effectType)
        {
            // ──────────────────────────────
            // 공격형
            // ──────────────────────────────

            case CardEffectType.InstantDamage:
                if (enemy != null)
                {
                    int damage = CalcDamage(hero.EffectiveATK, val);
                    Debug.Log($"[CardEffectApplier] InstantDamage - ATK: {hero.EffectiveATK}, val: {val}%, 최종 데미지: {damage}");
                    enemy.TakeDamage(damage);
                }
                else
                {
                    Debug.LogWarning($"[CardEffectApplier] enemy가 null입니다!");
                }
                break;

            case CardEffectType.MultihitDamage:
                if (enemy != null)
                    for (int i = 0; i < card.hitCount; i++)
                        enemy.TakeDamage(CalcDamage(hero.EffectiveATK, val));
                break;

            case CardEffectType.PierceDamage:
                if (enemy != null)
                    enemy.TakeDamageIgnoreDef(CalcDamage(hero.EffectiveATK, val));
                break;

            case CardEffectType.ExecuteDamage:
                // 적 현재 HP의 val% 즉시 제거
                if (enemy != null)
                    enemy.TakeDamageIgnoreDef(
                        Mathf.RoundToInt(enemy.CurrentHP * val / 100f));
                break;

            case CardEffectType.ConditionDamage:
            {
                // HP 50% 이하일 때 val2% 추가
                float bonus = hero.CurrentHP <= hero.MaxHP * 0.5f ? val2 : 0f;
                if (enemy != null)
                    enemy.TakeDamage(CalcDamage(hero.EffectiveATK, val + bonus));
                break;
            }

            // ──────────────────────────────
            // 방어형
            // ──────────────────────────────

            case CardEffectType.InstantHeal:
                hero.Heal(Mathf.RoundToInt(val));
                break;

            case CardEffectType.DEFBuff:
                hero.ApplyDEFBuff(Mathf.RoundToInt(val), val2);
                break;

            case CardEffectType.BlockNextHit:
                hero.ApplyBlockNextHit();
                break;

            case CardEffectType.HealToThreshold:
            {
                // HP가 최대치의 val% 미만이면 그 수치까지 회복
                int target = Mathf.RoundToInt(hero.MaxHP * val / 100f);
                if (hero.CurrentHP < target)
                    hero.Heal(target - hero.CurrentHP);
                break;
            }

            case CardEffectType.ThornsBuff:
                hero.ApplyThorns(val2);
                break;

            case CardEffectType.InvincibleBurst:
                hero.ApplyInvincible(val);
                break;

            // ──────────────────────────────
            // 유틸형
            // ──────────────────────────────

            case CardEffectType.DrawCard:
                HandManager.Instance?.DrawCards(Mathf.RoundToInt(val));
                break;

            case CardEffectType.EnergyRegenBoost:
                EnergySystem.Instance?.ApplyRegenBoost(val, val2);
                break;

            case CardEffectType.AtkSpeedBoost:
                hero.ApplyAtkSpeedBuff(val, val2);
                break;

            case CardEffectType.WeaknessExpose:
                hero.SetWeaknessExposed();
                break;

            case CardEffectType.RefreshHand:
                HandManager.Instance?.RefreshHand();
                break;

            case CardEffectType.EnergyBurst:
            {
                int drained = EnergySystem.Instance?.DrainAll() ?? 0;
                if (enemy != null)
                    enemy.TakeDamageIgnoreDef(drained * Mathf.RoundToInt(val));
                break;
            }

            // ──────────────────────────────
            // 특수형
            // ──────────────────────────────

            case CardEffectType.BloodPact:
                hero.ApplyPermanentATKBuff(Mathf.RoundToInt(val));
                hero.ApplyPermanentHPPenalty(Mathf.RoundToInt(val2));
                break;

            case CardEffectType.Gamble:
                if (Random.value >= 0.5f)
                    hero.Heal(Mathf.RoundToInt(hero.MaxHP * val / 100f));
                else
                    hero.TakeDamage(Mathf.RoundToInt(hero.MaxHP * 0.2f));
                break;

            case CardEffectType.BurnHand:
            {
                var burned = HandManager.Instance?.BurnAllHand();
                if (enemy != null && burned != null)
                    enemy.TakeDamageIgnoreDef(
                        hero.EffectiveATK * burned.Count * Mathf.RoundToInt(val) / 100);
                break;
            }

            case CardEffectType.UltraFocus:
                // effectValue초간 에너지 회복 속도 10배 → 카드 연속 사용 가능
                EnergySystem.Instance?.ApplyRegenBoost(10f, val);
                break;
        }
    }

    private static int CalcDamage(int atk, float pct)
        => Mathf.RoundToInt(atk * pct / 100f);
}
