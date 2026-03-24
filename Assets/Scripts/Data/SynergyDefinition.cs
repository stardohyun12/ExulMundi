using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 시너지 조건과 보너스 효과를 정의하는 ScriptableObject.
/// CardSynergySystem이 손패와 대조해 활성화 여부를 판정합니다.
/// </summary>
[CreateAssetMenu(fileName = "NewSynergy", menuName = "Exul Mundi/Synergy Definition")]
public class SynergyDefinition : ScriptableObject
{
    [Header("기본 정보")]
    public string synergyName;
    [TextArea]
    public string effectDescription;

    [Header("활성화 조건 — 카드 직접 지정")]
    [Tooltip("손패에 이 카드들이 모두 있을 때 시너지가 활성화됩니다.")]
    public CardData[] requiredCards;

    [Header("활성화 조건 — 태그 기반")]
    [Tooltip("손패에 이 태그를 가진 카드가 각각 1장 이상 있을 때 시너지가 활성화됩니다.")]
    public string[] requiredSynergyTags;

    [Header("보너스 효과")]
    [Tooltip("PassiveEffectApplier의 effectComponentType과 동일한 식별자.")]
    public string bonusEffectType;
    public float  bonusEffectValue;
    public float  bonusEffectValue2;

    /// <summary>주어진 카드 목록으로 활성화 여부를 반환합니다.</summary>
    public bool IsActive(IReadOnlyList<CardData> cards)
    {
        // 카드 직접 비교 우선
        if (requiredCards != null && requiredCards.Length > 0)
        {
            foreach (var required in requiredCards)
            {
                if (required == null) continue;
                bool found = false;
                foreach (var card in cards)
                    if (card == required) { found = true; break; }
                if (!found) return false;
            }
            return true;
        }

        // 태그 기반
        if (requiredSynergyTags != null && requiredSynergyTags.Length > 0)
        {
            foreach (var tag in requiredSynergyTags)
            {
                if (string.IsNullOrEmpty(tag)) continue;
                bool found = false;
                foreach (var card in cards)
                {
                    if (card.synergyTags == null) continue;
                    foreach (var cardTag in card.synergyTags)
                        if (cardTag == tag) { found = true; break; }
                    if (found) break;
                }
                if (!found) return false;
            }
            return true;
        }

        return false;
    }
}
