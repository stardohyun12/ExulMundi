using UnityEngine;

/// <summary>
/// 주인공의 기본 스탯 정의 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "HeroData", menuName = "Exul Mundi/Hero")]
public class HeroData : ScriptableObject
{
    [Header("기본 정보")]
    public string heroName = "주인공";
    public Sprite heroSprite;

    [Header("기본 스탯 (1계층 — 영구 성장 기준값)")]
    public int baseHP  = 100;
    public int baseATK = 10;
    public int baseDEF = 2;

    [Header("자동 공격")]
    public float attackInterval = 1.5f; // 공격 간격 (초)

    [Header("시작 카드 (핸드 초기 구성)")]
    public CardData[] startingDeck;
}
