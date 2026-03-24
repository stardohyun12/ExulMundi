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

    // ── PixelArtLit 팔레트 ────────────────────────────────────────────────────
    // WorldPaletteController가 씬 내 모든 PixelArtLit 머티리얼에 런타임으로 적용합니다.
    // 아트 문서 기준 세계별 기본값:
    //   중세      : 따뜻한 황금빛  (금색·갈색·세피아)
    //   스팀펑크  : 주황빛 증기    (구리·황동·어두운 회색)
    //   사이버펑크: 네온(청록·보라) (고대비 어두운 배경)
    //   동양      : 차가운 달빛    (먹색·남색·절제된 보라)
    //   고대      : 자연광         (황토·흙갈색·이끼 녹색)

    [Header("PixelArtLit 팔레트")]
    [Tooltip("밝은 면 색상. 주광원이 닿는 영역.")]
    public Color lightColor   = new Color(0.85f, 0.68f, 0.45f, 1f);

    [Tooltip("어두운 면 색상. 그림자 영역.")]
    public Color shadowColor  = new Color(0.14f, 0.10f, 0.22f, 1f);

    [Tooltip("전체 씬 앰비언트 (상수 항).")]
    public Color ambientColor = new Color(0.05f, 0.04f, 0.08f, 1f);

    [Tooltip("실루엣 림 하이라이트 색상.")]
    public Color rimColor     = new Color(1.00f, 0.90f, 0.65f, 1f);

    [Tooltip("외곽선 색상.")]
    public Color outlineColor = new Color(0.02f, 0.01f, 0.05f, 1f);

    [Tooltip("팔레트 전환 시간 (초). 세계가 바뀔 때 이 시간 동안 부드럽게 전환됩니다.")]
    [Range(0f, 2f)]
    public float paletteFadeDuration = 0.6f;
}
