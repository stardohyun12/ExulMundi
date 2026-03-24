using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 손패(CardInventory) 변경을 감시해 활성 시너지 목록을 갱신합니다.
/// 활성 시너지의 보너스 효과를 현재 장착 무기에 추가 적용합니다.
/// </summary>
public class CardSynergySystem : MonoBehaviour
{
    public static CardSynergySystem Instance { get; private set; }

    [Header("시너지 정의 목록")]
    [SerializeField] private SynergyDefinition[] synergyDefinitions;

    private readonly List<SynergyDefinition> _activeSynergies = new();

    /// <summary>활성 시너지 목록이 갱신될 때 발생합니다.</summary>
    public event Action<IReadOnlyList<SynergyDefinition>> OnSynergiesChanged;

    public IReadOnlyList<SynergyDefinition> ActiveSynergies => _activeSynergies;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        var inv = CardInventory.Instance;
        if (inv == null)
        {
            Debug.LogWarning("[CardSynergySystem] CardInventory.Instance가 없습니다.");
            return;
        }
        inv.OnCardAdded   += OnHandChanged;
        inv.OnCardRemoved += OnHandChanged;
        inv.OnCardMoved   += EvaluateSynergies;
        EvaluateSynergies();
    }

    private void OnDestroy()
    {
        if (CardInventory.Instance == null) return;
        CardInventory.Instance.OnCardAdded   -= OnHandChanged;
        CardInventory.Instance.OnCardRemoved -= OnHandChanged;
        CardInventory.Instance.OnCardMoved   -= EvaluateSynergies;
    }

    private void OnHandChanged(CardData _) => EvaluateSynergies();

    /// <summary>손패와 모든 SynergyDefinition을 대조해 활성 목록을 갱신합니다.</summary>
    public void EvaluateSynergies()
    {
        var inv = CardInventory.Instance;
        if (inv == null) return;

        _activeSynergies.Clear();

        if (synergyDefinitions != null)
            foreach (var def in synergyDefinitions)
                if (def != null && def.IsActive(inv.Cards))
                    _activeSynergies.Add(def);

        OnSynergiesChanged?.Invoke(_activeSynergies);

        if (_activeSynergies.Count > 0)
            ApplySynergyBonuses();

        Debug.Log($"[CardSynergySystem] 활성 시너지: {_activeSynergies.Count}개");
    }

    private void ApplySynergyBonuses()
    {
        var weapon = WeaponManager.Instance?.CurrentWeapon;
        if (weapon == null) return;

        foreach (var synergy in _activeSynergies)
        {
            if (string.IsNullOrEmpty(synergy.bonusEffectType)) continue;
            ApplyBonus(synergy, weapon);
        }
    }

    private static void ApplyBonus(SynergyDefinition synergy, WeaponBase weapon)
    {
        switch (synergy.bonusEffectType)
        {
            case "AttackSpeedEffect":
                weapon.AttackInterval *= synergy.bonusEffectValue;
                break;

            case "AttackSizeEffect":
                weapon.AttackSize *= synergy.bonusEffectValue;
                break;

            case "AttackCountEffect":
                weapon.AttackCount += Mathf.RoundToInt(synergy.bonusEffectValue);
                break;

            case "BonusDamageEffect":
            {
                float mult = 1f + synergy.bonusEffectValue / 100f;
                if (weapon is GunWeapon   g) g.BaseDamage = Mathf.Max(1, Mathf.RoundToInt(g.BaseDamage * mult));
                if (weapon is MeleeWeapon m) m.BaseDamage = Mathf.Max(1, Mathf.RoundToInt(m.BaseDamage * mult));
                if (weapon is StaffWeapon s) s.BaseDamage = Mathf.Max(1, Mathf.RoundToInt(s.BaseDamage * mult));
                break;
            }

            case "LifeStealEffect":
                if (weapon is GunWeapon   gLS) gLS.LifeStealRate += synergy.bonusEffectValue / 100f;
                if (weapon is MeleeWeapon mLS) mLS.LifeStealRate += synergy.bonusEffectValue / 100f;
                if (weapon is StaffWeapon sLS) sLS.LifeStealRate += synergy.bonusEffectValue / 100f;
                break;

            default:
                Debug.LogWarning($"[CardSynergySystem] 알 수 없는 시너지 효과 타입: {synergy.bonusEffectType}");
                break;
        }
    }
}
