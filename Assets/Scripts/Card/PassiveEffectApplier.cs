using System.Linq;
using UnityEngine;

/// <summary>
/// 손패(CardInventory)를 감시하다가 카드 변경 시마다 무기 장착과 패시브 스탯을 재계산합니다.
/// Player에 붙여 사용합니다.
/// </summary>
public class PassiveEffectApplier : MonoBehaviour
{
    private void Start()
    {
        var inv = CardInventory.Instance;
        if (inv == null)
        {
            Debug.LogWarning("[PassiveEffectApplier] CardInventory.Instance가 없습니다.");
            return;
        }
        inv.OnCardAdded   += OnHandChanged;
        inv.OnCardRemoved += OnHandChanged;
    }

    private void OnDestroy()
    {
        if (CardInventory.Instance == null) return;
        CardInventory.Instance.OnCardAdded   -= OnHandChanged;
        CardInventory.Instance.OnCardRemoved -= OnHandChanged;
    }

    private void OnHandChanged(CardData _) => ApplyAll();

    /// <summary>손패 전체를 다시 읽어 무기 장착과 패시브 효과를 재적용합니다.</summary>
    public void ApplyAll()
    {
        var inv = CardInventory.Instance;
        if (inv == null) return;

        var cards = inv.Cards;

        // ── 1. 무기 카드 → WeaponManager에 장착 요청 ──────────────────────
        var weaponCard = cards.FirstOrDefault(c => c.cardType == CardType.Weapon);
        if (weaponCard != null)
            WeaponManager.Instance?.EquipWeapon(weaponCard.weaponType);

        // ── 2. 현재 무기 스탯 초기화 후 패시브 카드 일괄 적용 ─────────────
        var weapon = WeaponManager.Instance?.CurrentWeapon;
        if (weapon == null) return;

        weapon.ResetStats();
        if (weaponCard != null) ApplyWeaponBaseStats(weaponCard, weapon);

        int passiveCount = 0;
        foreach (var card in cards)
        {
            if (card.cardType != CardType.Passive) continue;
            ApplyPassive(card, weapon);
            passiveCount++;
        }

        Debug.Log($"[PassiveEffectApplier] 적용 완료 — 무기: {weapon.WeaponType} ({weapon.Category}), 패시브: {passiveCount}장");
    }

    /// <summary>무기 카드에 정의된 기본 스탯을 무기에 적용합니다.</summary>
    private static void ApplyWeaponBaseStats(CardData wc, WeaponBase weapon)
    {
        if (wc.baseAttackInterval > 0f) weapon.AttackInterval = wc.baseAttackInterval;
        if (wc.baseAttackSize     > 0f) weapon.AttackSize     = wc.baseAttackSize;
        if (wc.baseAttackCount    > 0)  weapon.AttackCount    = wc.baseAttackCount;

        switch (weapon.Category)
        {
            case WeaponCategory.Projectile when weapon is GunWeapon gun:
                if (wc.baseDamage          > 0)  gun.BaseDamage  = wc.baseDamage;
                if (wc.baseProjectileSpeed > 0f) gun.BulletSpeed = wc.baseProjectileSpeed;
                break;

            case WeaponCategory.Melee when weapon is MeleeWeapon melee:
                if (wc.baseDamage > 0)  melee.BaseDamage  = wc.baseDamage;
                if (wc.baseRange  > 0f) melee.SlashRadius = wc.baseRange;
                break;

            case WeaponCategory.Summon when weapon is StaffWeapon staff:
                if (wc.baseDamage > 0)  staff.BaseDamage  = wc.baseDamage;
                if (wc.baseRange  > 0f) staff.OrbitRadius = wc.baseRange;
                break;
        }
    }

    /// <summary>패시브 카드 효과를 무기에 적용합니다.</summary>
    private static void ApplyPassive(CardData card, WeaponBase weapon)
    {
        switch (card.effectComponentType)
        {
            // ── 공통 ─────────────────────────────────────────────────────────
            case "AttackSpeedEffect":
                weapon.AttackInterval *= card.effectValue;
                break;

            case "AttackSizeEffect":
                weapon.AttackSize *= card.effectValue;
                break;

            case "AttackCountEffect":
                weapon.AttackCount += Mathf.RoundToInt(card.effectValue);
                break;

            case "LifeStealEffect":
                switch (weapon.Category)
                {
                    case WeaponCategory.Projectile when weapon is GunWeapon   gLS: gLS.LifeStealRate += card.effectValue; break;
                    case WeaponCategory.Melee      when weapon is MeleeWeapon mLS: mLS.LifeStealRate += card.effectValue; break;
                    case WeaponCategory.Summon     when weapon is StaffWeapon sLS: sLS.LifeStealRate += card.effectValue; break;
                }
                break;

            // ── Projectile 전용 ───────────────────────────────────────────────
            case "MultiShotEffect" when weapon.Category == WeaponCategory.Projectile && weapon is GunWeapon gun:
                gun.ExtraAngles.Add( card.effectValue);
                gun.ExtraAngles.Add(-card.effectValue);
                break;

            case "PiercingEffect" when weapon.Category == WeaponCategory.Projectile && weapon is GunWeapon gunP:
                gunP.IsPiercing = true;
                break;

            case "BulletSpeedEffect" when weapon.Category == WeaponCategory.Projectile && weapon is GunWeapon gunS:
                gunS.BulletSpeed *= card.effectValue;
                break;

            case "ExplosiveEffect" when weapon.Category == WeaponCategory.Projectile && weapon is GunWeapon gunE:
                gunE.IsExplosive     = true;
                gunE.ExplosionRadius = card.effectValue;
                break;

            // ── Melee 전용 ───────────────────────────────────────────────────
            case "BonusDamageEffect" when weapon.Category == WeaponCategory.Melee && weapon is MeleeWeapon melee:
                melee.BonusDamage += Mathf.RoundToInt(card.effectValue);
                break;

            case "SlashRadiusEffect" when weapon.Category == WeaponCategory.Melee && weapon is MeleeWeapon meleeR:
                meleeR.SlashRadius *= card.effectValue;
                break;

            // ── Summon 전용 ──────────────────────────────────────────────────
            case "OrbitRadiusEffect" when weapon.Category == WeaponCategory.Summon && weapon is StaffWeapon staff:
                staff.OrbitRadius *= card.effectValue;
                break;

            case "BonusDamageEffect" when weapon.Category == WeaponCategory.Summon && weapon is StaffWeapon staffD:
                staffD.BonusDamage += Mathf.RoundToInt(card.effectValue);
                break;
        }
    }
}
