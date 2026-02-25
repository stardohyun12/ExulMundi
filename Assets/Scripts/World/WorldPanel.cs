using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 개별 세계 패널 — 배경색과 스테이지 정보를 표시.
/// WorldScrollManager가 Setup()을 호출하여 내용을 갱신한다.
/// </summary>
[RequireComponent(typeof(Image))]
public class WorldPanel : MonoBehaviour
{
    [Header("UI 참조")]
    public TextMeshProUGUI stageText;
    public TextMeshProUGUI typeText;

    private Image _bg;

    void Awake() => _bg = GetComponent<Image>();

    // ══════════════════════════════════════════════════════════

    /// <summary>스테이지 인덱스 기반으로 패널 내용 설정</summary>
    public void Setup(int stageIndex)
    {
        if (stageIndex <= 0)
        {
            // 시작 이전 — 진입 불가 구간
            _bg.color = new Color(0.06f, 0.06f, 0.06f);
            if (stageText != null) stageText.text = "";
            if (typeText  != null) typeText.text  = "";
            return;
        }

        WorldType type = GetWorldType(stageIndex);
        _bg.color = GetColor(stageIndex, type);

        if (stageText != null) stageText.text = $"Stage {stageIndex}";
        if (typeText  != null) typeText.text  = GetTypeName(type);
    }

    // ─── 스테이지 → WorldType (재현 가능한 결정론적 무작위) ────

    static WorldType GetWorldType(int index)
    {
        // 11스테이지마다 보스
        if (index % 11 == 0) return WorldType.Boss;

        // 시드 기반 무작위 (같은 인덱스는 항상 같은 타입)
        var rng  = new System.Random(index * 7919);
        int roll = rng.Next(100);

        return roll switch
        {
            < 55 => WorldType.Normal,
            < 70 => WorldType.Elite,
            < 80 => WorldType.Shop,
            < 88 => WorldType.Healing,
            < 95 => WorldType.Mystery,
            _    => WorldType.Boss,
        };
    }

    // ─── 배경색 ───────────────────────────────────────────────

    // WorldType 순서: Normal / Elite / Shop / Healing / Mystery / Boss
    static readonly Color[] BaseColors =
    {
        new Color(0.12f, 0.18f, 0.35f), // Normal   — 딥 블루
        new Color(0.40f, 0.10f, 0.10f), // Elite    — 다크 레드
        new Color(0.10f, 0.30f, 0.10f), // Shop     — 다크 그린
        new Color(0.08f, 0.28f, 0.25f), // Healing  — 틸
        new Color(0.25f, 0.08f, 0.35f), // Mystery  — 다크 퍼플
        new Color(0.45f, 0.05f, 0.05f), // Boss     — 크림슨
    };

    static Color GetColor(int index, WorldType type)
    {
        Color  b   = BaseColors[(int)type];
        var    rng = new System.Random(index * 1301 + 42);
        // 인덱스마다 ±4% 미세 변화로 같은 타입이어도 조금씩 다르게
        float  v   = (float)(rng.NextDouble() - 0.5) * 0.08f;
        return new Color(
            Mathf.Clamp01(b.r + v),
            Mathf.Clamp01(b.g + v),
            Mathf.Clamp01(b.b + v)
        );
    }

    // ─── 타입 이름 ────────────────────────────────────────────

    static string GetTypeName(WorldType type) => type switch
    {
        WorldType.Normal  => "일반 전투",
        WorldType.Elite   => "정예 전투",
        WorldType.Shop    => "상점",
        WorldType.Healing => "회복 지대",
        WorldType.Mystery => "미스터리",
        WorldType.Boss    => "⚔ 보스",
        _                 => "",
    };
}
