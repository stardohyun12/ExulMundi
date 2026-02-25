using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 패시브 카드 효과와 시너지를 전투에 적용하는 시스템.
/// 손에 든 카드들이 자동으로 주는 효과를 계산하고 HeroUnit에 반영.
/// </summary>
public class PassiveEffectApplier : MonoBehaviour
{
    [Header("업데이트 주기")]
    [Tooltip("패시브 효과 재계산 주기 (초)")]
    public float updateInterval = 0.5f;

    private float _updateTimer;
    private HeroUnit _heroUnit;

    // 현재 적용 중인 보너스 (디버깅용)
    private float _currentATKBonus;
    private float _currentDEFBonus;
    private float _currentHPBonus;
    private float _currentAttackSpeedBonus;

    void Start()
    {
        if (BattleManager.Instance != null)
            _heroUnit = BattleManager.Instance.HeroUnit;
    }

    void Update()
    {
        if (BattleManager.Instance == null || !BattleManager.Instance.IsBattleActive)
            return;

        _updateTimer += Time.deltaTime;
        if (_updateTimer >= updateInterval)
        {
            _updateTimer = 0f;
            ApplyPassiveEffects();
        }
    }

    // ═══════════════════════════════════════
    // 패시브 효과 적용
    // ═══════════════════════════════════════

    /// <summary>
    /// 현재 손에 든 카드의 패시브 효과 + 시너지를 계산하고 적용
    /// </summary>
    private void ApplyPassiveEffects()
    {
        if (_heroUnit == null || PassiveCardManager.Instance == null)
        {
            Debug.LogWarning("[패시브] HeroUnit 또는 PassiveCardManager가 null입니다!");
            return;
        }

        Debug.Log("[패시브] 효과 계산 시작");

        // 보너스 초기화
        _currentATKBonus = 0f;
        _currentDEFBonus = 0f;
        _currentHPBonus = 0f;
        _currentAttackSpeedBonus = 0f;

        // 1. 손에 든 카드의 개별 패시브 효과 계산
        ApplyCardPassives();

        // 2. 시너지 보너스 추가
        ApplySynergyBonuses();

        // 3. 실제 스탯에 반영
        ApplyBonusesToHero();
    }

    /// <summary>
    /// 각 카드의 개별 패시브 효과 계산
    /// </summary>
    private void ApplyCardPassives()
    {
        List<CardData> cardsInHand = PassiveCardManager.Instance.GetCurrentHand();

        Debug.Log($"[패시브] 손에 든 카드 수: {cardsInHand.Count}");

        foreach (var card in cardsInHand)
        {
            Debug.Log($"[패시브] 카드: {card.cardName}, 효과타입: {card.passiveEffectType}, 수치: {card.passiveValue}");

            if (card.passiveEffectType == PassiveEffectType.None)
                continue;

            switch (card.passiveEffectType)
            {
                case PassiveEffectType.ATKBonus:
                    _currentATKBonus += card.passiveValue;
                    Debug.Log($"[패시브] ATK 보너스 추가: +{card.passiveValue}% (총: {_currentATKBonus}%)");
                    break;

                case PassiveEffectType.DEFBonus:
                    _currentDEFBonus += card.passiveValue;
                    break;

                case PassiveEffectType.MaxHPBonus:
                    _currentHPBonus += card.passiveValue;
                    break;

                case PassiveEffectType.AttackSpeedBonus:
                    _currentAttackSpeedBonus += card.passiveValue;
                    break;

                case PassiveEffectType.LifeSteal:
                    // TODO: 생명력 흡수 구현
                    break;

                case PassiveEffectType.LowHPATKBonus:
                    // HP 50% 이하일 때만 적용
                    if (_heroUnit != null && _heroUnit.CurrentHP <= _heroUnit.MaxHP * 0.5f)
                    {
                        _currentATKBonus += card.passiveValue;
                        _currentAttackSpeedBonus += card.passiveValue2;
                    }
                    break;

                case PassiveEffectType.LowHPAtkSpeedBonus:
                    if (_heroUnit != null && _heroUnit.CurrentHP <= _heroUnit.MaxHP * 0.5f)
                    {
                        _currentAttackSpeedBonus += card.passiveValue;
                    }
                    break;

                // 추가 패시브 효과들...
            }
        }
    }

    /// <summary>
    /// 시너지 시스템에서 보너스 가져오기
    /// </summary>
    private void ApplySynergyBonuses()
    {
        if (PassiveCardManager.Instance.synergySystem == null)
            return;

        var synergies = PassiveCardManager.Instance.GetActiveSynergies();

        foreach (var synergy in synergies)
        {
            switch (synergy.effectType)
            {
                case SynergyEffectType.ATKBonus:
                    _currentATKBonus += synergy.bonusValue;
                    break;

                case SynergyEffectType.DEFBonus:
                    _currentDEFBonus += synergy.bonusValue;
                    break;

                case SynergyEffectType.HPBonus:
                    _currentHPBonus += synergy.bonusValue;
                    break;

                case SynergyEffectType.AttackSpeedBonus:
                    _currentAttackSpeedBonus += synergy.bonusValue;
                    break;

                // 추가 시너지 효과들...
            }
        }
    }

    /// <summary>
    /// 계산된 보너스를 HeroUnit에 실제 적용
    /// </summary>
    private void ApplyBonusesToHero()
    {
        if (_heroUnit == null) return;

        // HeroUnit의 private 필드에 리플렉션으로 직접 설정
        // 패시브 효과는 지속적으로 적용되므로 버프가 아닌 직접 설정
        
        // ATK 보너스 (% 형태)
        int atkBonus = Mathf.RoundToInt(_heroUnit.EffectiveATK * (_currentATKBonus / 100f));
        SetPrivateField(_heroUnit, "_atkBuff", atkBonus);
        
        // DEF 보너스 (고정 수치)
        SetPrivateField(_heroUnit, "_defBuff", Mathf.RoundToInt(_currentDEFBonus));
        
        // MaxHP 보너스 (% 형태)
        int hpBonus = Mathf.RoundToInt(_heroUnit.MaxHP * (_currentHPBonus / 100f));
        SetPrivateField(_heroUnit, "_maxHPBuff", hpBonus);
        
        // Attack Speed 보너스 (배율)
        float speedMult = 1f + (_currentAttackSpeedBonus / 100f);
        SetPrivateField(_heroUnit, "_atkSpeedMult", speedMult);

        // 디버깅 로그 (첫 프레임에만)
        if (Time.frameCount % 120 == 0)
        {
            Debug.Log($"[패시브 효과] ATK: +{atkBonus} ({_currentATKBonus}%), DEF: +{_currentDEFBonus}, HP: +{hpBonus} ({_currentHPBonus}%), Speed: x{speedMult:F2}");
        }
    }

    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(obj, value);
        }
    }

    // ═══════════════════════════════════════
    // 공개 API
    // ═══════════════════════════════════════

    /// <summary>
    /// 현재 적용 중인 ATK 보너스
    /// </summary>
    public float GetCurrentATKBonus() => _currentATKBonus;

    /// <summary>
    /// 현재 적용 중인 DEF 보너스
    /// </summary>
    public float GetCurrentDEFBonus() => _currentDEFBonus;

    /// <summary>
    /// 현재 적용 중인 공격 속도 보너스
    /// </summary>
    public float GetCurrentAttackSpeedBonus() => _currentAttackSpeedBonus;

    /// <summary>
    /// 패시브 효과 강제 재계산
    /// </summary>
    public void ForceUpdate()
    {
        ApplyPassiveEffects();
    }
}
