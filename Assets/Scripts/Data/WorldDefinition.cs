using UnityEngine;

/// <summary>플레이어가 시작 시 선택하는 세계를 정의하는 ScriptableObject.</summary>
[CreateAssetMenu(fileName = "NewWorld", menuName = "Exul Mundi/World Definition")]
public class WorldDefinition : ScriptableObject
{
    [Header("세계 정보")]
    public string worldName;
    [TextArea]
    public string description;
    public Sprite icon;
    public Color  themeColor = Color.white;

    [Header("무기")]
    [Tooltip("런 시작 시 손패에 지급되는 무기 카드. 이 카드로 무기가 결정됩니다.")]
    public CardData weaponCard;

    [Header("카드 풀 (전투 후 보상)")]
    [Tooltip("이 세계에서 제공되는 카드 후보 목록.")]
    public CardData[] cardPool;

    [Header("난이도")]
    [Range(0.5f, 5f)]
    public float difficultyMult = 1f;
}
