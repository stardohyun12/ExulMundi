using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 카드 시너지 효과 데이터
/// </summary>
[System.Serializable]
public class SynergyEffect
{
    public string synergyName;
    public CardData[] requiredCards;        // 필요한 카드 조합
    public SynergyEffectType effectType;    // 시너지 효과 타입
    public float bonusValue;                // 보너스 수치
    
    [TextArea]
    public string description;              // 효과 설명
}

/// <summary>
/// 시너지 효과 타입
/// </summary>
public enum SynergyEffectType
{
    // 스탯 보너스
    ATKBonus,           // 공격력 +X%
    DEFBonus,           // 방어력 +X%
    HPBonus,            // 최대 HP +X%
    AttackSpeedBonus,   // 공격 속도 +X%
    
    // 특수 효과
    CriticalChance,     // 치명타 확률 +X%
    LifeSteal,          // 생명력 흡수 X%
    DamageReflect,      // 데미지 반사 X%
    CooldownReduction,  // 스킬 쿨다운 감소 X%
    
    // 자원 관련
    EnergyRegenBonus,   // 에너지 회복 속도 +X%
    CardDrawBonus,      // 카드 드로우 +X장
    
    // 조건부 효과
    LowHPDamage,        // HP 50% 이하일 때 데미지 +X%
    FullHPDefense,      // HP 100%일 때 방어력 +X%
    
    // 특수 메카닉
    DoubleHit,          // X% 확률로 공격 2회
    PierceArmor,        // 방어력 무시 X%
}

/// <summary>
/// 카드 시너지 시스템.
/// 손에 든 카드 조합을 분석하고 시너지 효과를 계산.
/// </summary>
public class CardSynergySystem : MonoBehaviour
{
    [Header("시너지 정의")]
    [Tooltip("게임에 존재하는 모든 시너지 효과 목록")]
    public List<SynergyEffect> allSynergies = new List<SynergyEffect>();

    // 현재 활성화된 시너지 목록
    private List<SynergyEffect> _activeSynergies = new List<SynergyEffect>();

    // ═══════════════════════════════════════
    // 시너지 계산
    // ═══════════════════════════════════════

    /// <summary>
    /// 손에 든 카드들로 활성화되는 시너지를 계산
    /// </summary>
    public void CalculateSynergies(List<CardData> cardsInHand)
    {
        _activeSynergies.Clear();

        if (cardsInHand == null || cardsInHand.Count == 0)
            return;

        // 모든 시너지를 확인
        foreach (var synergy in allSynergies)
        {
            if (IsSynergyActive(synergy, cardsInHand))
            {
                _activeSynergies.Add(synergy);
                Debug.Log($"시너지 활성화: {synergy.synergyName} (+{synergy.bonusValue})");
            }
        }

        // 활성화된 시너지를 전투 시스템에 적용
        ApplySynergiesToBattle();
    }

    /// <summary>
    /// 특정 시너지가 활성화되는지 확인
    /// </summary>
    private bool IsSynergyActive(SynergyEffect synergy, List<CardData> cardsInHand)
    {
        if (synergy.requiredCards == null || synergy.requiredCards.Length == 0)
            return false;

        // 필요한 모든 카드가 손에 있는지 확인
        foreach (var requiredCard in synergy.requiredCards)
        {
            if (!cardsInHand.Contains(requiredCard))
                return false;
        }

        return true;
    }

    // ═══════════════════════════════════════
    // 시너지 효과 적용
    // ═══════════════════════════════════════

    /// <summary>
    /// 활성화된 시너지를 전투 시스템에 적용
    /// </summary>
    private void ApplySynergiesToBattle()
    {
        if (BattleManager.Instance == null || BattleManager.Instance.HeroUnit == null)
            return;

        // TODO: 실제 스탯 적용은 PassiveEffectApplier에서 처리
        // 여기서는 활성화된 시너지 목록만 업데이트
    }

    // ═══════════════════════════════════════
    // 공개 API
    // ═══════════════════════════════════════

    /// <summary>
    /// 현재 활성화된 시너지 목록 반환
    /// </summary>
    public List<SynergyEffect> GetActiveSynergies()
    {
        return new List<SynergyEffect>(_activeSynergies);
    }

    /// <summary>
    /// 특정 효과 타입의 총 보너스 계산
    /// </summary>
    public float GetTotalBonusForType(SynergyEffectType effectType)
    {
        float total = 0f;
        foreach (var synergy in _activeSynergies)
        {
            if (synergy.effectType == effectType)
                total += synergy.bonusValue;
        }
        return total;
    }

    /// <summary>
    /// 활성 시너지가 있는지 확인
    /// </summary>
    public bool HasActiveSynergies()
    {
        return _activeSynergies.Count > 0;
    }
}
