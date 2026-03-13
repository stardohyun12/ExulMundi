using System.Linq;
using UnityEngine;

/// <summary>
/// 손패(CardInventory)를 감시하다가 카드 변경 시마다 무기 장착과 패시브 스탯을 재계산합니다.
/// 패시브 카드는 무기 카드와 인접한 카드만 효과를 적용합니다.
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
        inv.OnCardMoved   += ApplyAll;
    }

    private void OnDestroy()
    {
        if (CardInventory.Instance == null) return;
        CardInventory.Instance.OnCardAdded   -= OnHandChanged;
        CardInventory.Instance.OnCardRemoved -= OnHandChanged;
        CardInventory.Instance.OnCardMoved   -= ApplyAll;
    }

    private void OnHandChanged(CardData _) => ApplyAll();

    /// <summary>손패 전체를 다시 읽어 무기 장착과 인접 패시브 효과를 재적용합니다.</summary>
    public void ApplyAll()
    {
        var inv = CardInventory.Instance;
        if (inv == null) return;

        var cards = inv.Cards;

        // ── 1. 무기 카드 → WeaponManager에 장착 요청 ──────────────────────
        int weaponIndex = -1;
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].cardType == CardType.Weapon)
            {
                weaponIndex = i;
                break;
            }
        }

        CardData weaponCard = weaponIndex >= 0 ? cards[weaponIndex] : null;
        if (weaponCard != null)
            WeaponManager.Instance?.EquipWeapon(weaponCard.weaponType);

        // ── 2. 현재 무기 스탯 초기화 ──────────────────────────────────────
        var weapon = WeaponManager.Instance?.CurrentWeapon;
        if (weapon == null) return;

        weapon.ResetStats();
        if (weaponCard != null) ApplyWeaponBaseStats(weaponCard, weapon);

        // ── 3. 무기 카드 인접 카드만 패시브 적용 ─────────────────────────
        if (weaponIndex < 0) return;

        int passiveCount = 0;
        int leftIndex    = weaponIndex - 1;
        int rightIndex   = weaponIndex + 1;

        if (leftIndex >= 0 && cards[leftIndex].cardType == CardType.Accessory)
        {
            ApplyPassive(cards[leftIndex], weapon);
            passiveCount++;
        }

        if (rightIndex < cards.Count && cards[rightIndex].cardType == CardType.Accessory)
        {
            ApplyPassive(cards[rightIndex], weapon);
            passiveCount++;
        }

        Debug.Log($"[PassiveEffectApplier] 적용 완료 — 무기: {weapon.WeaponType} ({weapon.Category}), 인접 패시브: {passiveCount}장 (무기 인덱스: {weaponIndex})");
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
            // ── 범용 (무기 타입 무관) ────────────────────────────────────────
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
                if (weapon is GunWeapon   gLS) gLS.LifeStealRate += card.effectValue / 100f;
                if (weapon is MeleeWeapon mLS) mLS.LifeStealRate += card.effectValue / 100f;
                if (weapon is StaffWeapon sLS) sLS.LifeStealRate += card.effectValue / 100f;
                break;

            case "BonusDamageEffect":
            {
                float mult = 1f + card.effectValue / 100f;
                if (weapon is GunWeapon   gD) gD.BaseDamage   = Mathf.Max(1, Mathf.RoundToInt(gD.BaseDamage   * mult));
                if (weapon is MeleeWeapon mD) mD.BaseDamage   = Mathf.Max(1, Mathf.RoundToInt(mD.BaseDamage   * mult));
                if (weapon is StaffWeapon sD) sD.BaseDamage   = Mathf.Max(1, Mathf.RoundToInt(sD.BaseDamage   * mult));
                break;
            }

            case "ConditionalDamageEffect":
            {
                var comp = FindAnyObjectByType<ConditionalDamageEffect>();
                comp?.ApplyIfConditionMet();
                break;
            }

            // ── Gun 전용 ────────────────────────────────────────────────────
            case "MultiShotEffect" when weapon is GunWeapon gunM:
                gunM.ExtraAngles.Add( card.effectValue);
                gunM.ExtraAngles.Add(-card.effectValue);
                break;

            case "PiercingEffect" when weapon is GunWeapon gunP:
                gunP.IsPiercing = true;
                break;

            case "BulletSpeedEffect" when weapon is GunWeapon gunS:
                gunS.BulletSpeed *= card.effectValue;
                break;

            case "ExplosiveEffect" when weapon is GunWeapon gunE:
                gunE.IsExplosive     = true;
                gunE.ExplosionRadius = card.effectValue;
                break;

            // ── Melee 전용 ─────────────────────────────────────────────────
            case "SlashRadiusEffect" when weapon is MeleeWeapon meleeR:
                meleeR.SlashRadius *= card.effectValue;
                break;

            // ── Staff 전용 ─────────────────────────────────────────────────
            case "OrbitRadiusEffect" when weapon is StaffWeapon staffR:
                staffR.OrbitRadius *= card.effectValue;
                break;
        }
    }
}